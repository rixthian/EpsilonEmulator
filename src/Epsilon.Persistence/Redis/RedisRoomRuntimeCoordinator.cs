using System.Collections.Concurrent;
using System.Text.Json;
using Epsilon.CoreGame;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Epsilon.Persistence;

internal sealed class RedisRoomRuntimeCoordinator : IRoomRuntimeCoordinator, IAsyncDisposable
{
    private const string ChannelName = "epsilon:room-runtime:mutations";
    private const string SnapshotKeyPrefix = "epsilon:room-runtime:snapshot:";
    private const string RoomIndexKey = "epsilon:room-runtime:rooms";

    private readonly LocalRoomRuntimeCoordinator _localFallback;
    private readonly string _connectionString;
    private readonly ConcurrentDictionary<RoomId, long> _roomVersions = new();
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private ConnectionMultiplexer? _multiplexer;
    private DateTime _nextConnectAttemptUtc = DateTime.MinValue;

    public RedisRoomRuntimeCoordinator(
        IOptions<PersistenceOptions> options,
        LocalRoomRuntimeCoordinator localFallback)
    {
        _connectionString = options.Value.RedisConnectionString;
        _localFallback = localFallback;
    }

    public async ValueTask SignalMutationAsync(
        RoomId roomId,
        RoomRuntimeMutationKind mutationKind,
        CancellationToken cancellationToken = default)
    {
        await _localFallback.SignalMutationAsync(roomId, mutationKind, cancellationToken);
        RoomRuntimeCoordinationSnapshot? snapshot =
            await _localFallback.GetSnapshotAsync(roomId, cancellationToken);

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        ConnectionMultiplexer? multiplexer = await TryGetMultiplexerAsync(cancellationToken);
        if (multiplexer is null || !multiplexer.IsConnected)
        {
            return;
        }

        long version = _roomVersions.AddOrUpdate(roomId, 1, static (_, current) => current + 1);
        var payload = new RoomRuntimeMutationEnvelope(
            RoomId: roomId.Value,
            MutationKind: mutationKind.ToString(),
            Version: version,
            OccurredAtUtc: DateTime.UtcNow);

        try
        {
            if (snapshot is not null)
            {
                await multiplexer.GetDatabase().StringSetAsync(
                    BuildSnapshotKey(roomId),
                    JsonSerializer.Serialize(snapshot));
                await multiplexer.GetDatabase().SetAddAsync(RoomIndexKey, roomId.Value);
            }

            await multiplexer.GetSubscriber().PublishAsync(
                RedisChannel.Literal(ChannelName),
                JsonSerializer.Serialize(payload));
        }
        catch (RedisConnectionException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
        }
        catch (RedisTimeoutException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
        }
    }

    public ValueTask<RoomRuntimeCoordinationSnapshot?> GetSnapshotAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        return GetSnapshotCoreAsync(roomId, cancellationToken);
    }

    public async ValueTask<IReadOnlyList<RoomRuntimeCoordinationSnapshot>> GetSnapshotsAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return await _localFallback.GetSnapshotsAsync(cancellationToken);
        }

        ConnectionMultiplexer? multiplexer = await TryGetMultiplexerAsync(cancellationToken);
        if (multiplexer is null || !multiplexer.IsConnected)
        {
            return await _localFallback.GetSnapshotsAsync(cancellationToken);
        }

        try
        {
            IDatabase database = multiplexer.GetDatabase();
            RedisValue[] roomIds = await database.SetMembersAsync(RoomIndexKey);
            if (roomIds.Length == 0)
            {
                return await _localFallback.GetSnapshotsAsync(cancellationToken);
            }

            RedisKey[] keys = roomIds
                .Select(static roomId => (RedisKey)BuildSnapshotKey(new RoomId((long)roomId)))
                .ToArray();
            RedisValue[] payloads = await database.StringGetAsync(keys);

            IReadOnlyList<RoomRuntimeCoordinationSnapshot> snapshots = payloads
                .Where(static payload => payload.HasValue)
                .Select(static payload => JsonSerializer.Deserialize<RoomRuntimeCoordinationSnapshot>(payload.ToString()))
                .OfType<RoomRuntimeCoordinationSnapshot>()
                .OrderByDescending(static snapshot => snapshot.LastMutationAtUtc)
                .ToArray();

            return snapshots.Count > 0
                ? snapshots
                : await _localFallback.GetSnapshotsAsync(cancellationToken);
        }
        catch (RedisConnectionException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return await _localFallback.GetSnapshotsAsync(cancellationToken);
        }
        catch (RedisTimeoutException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return await _localFallback.GetSnapshotsAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_multiplexer is not null)
        {
            await _multiplexer.CloseAsync();
            _multiplexer.Dispose();
        }

        _connectLock.Dispose();
    }

    private async Task<ConnectionMultiplexer?> TryGetMultiplexerAsync(CancellationToken cancellationToken)
    {
        if (_multiplexer is { IsConnected: true })
        {
            return _multiplexer;
        }

        if (DateTime.UtcNow < _nextConnectAttemptUtc)
        {
            return _multiplexer;
        }

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_multiplexer is { IsConnected: true })
            {
                return _multiplexer;
            }

            if (DateTime.UtcNow < _nextConnectAttemptUtc)
            {
                return _multiplexer;
            }

            ConfigurationOptions configuration = ConfigurationOptions.Parse(_connectionString);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectRetry = 1;
            configuration.ConnectTimeout = 1000;
            configuration.SyncTimeout = 1000;
            configuration.AsyncTimeout = 1000;

            _multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration);
            _nextConnectAttemptUtc = DateTime.MinValue;
            return _multiplexer;
        }
        catch (RedisConnectionException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return _multiplexer;
        }
        catch (RedisTimeoutException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return _multiplexer;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async ValueTask<RoomRuntimeCoordinationSnapshot?> GetSnapshotCoreAsync(
        RoomId roomId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return await _localFallback.GetSnapshotAsync(roomId, cancellationToken);
        }

        ConnectionMultiplexer? multiplexer = await TryGetMultiplexerAsync(cancellationToken);
        if (multiplexer is null || !multiplexer.IsConnected)
        {
            return await _localFallback.GetSnapshotAsync(roomId, cancellationToken);
        }

        try
        {
            RedisValue payload = await multiplexer.GetDatabase().StringGetAsync(BuildSnapshotKey(roomId));
            if (!payload.HasValue)
            {
                return await _localFallback.GetSnapshotAsync(roomId, cancellationToken);
            }

            return JsonSerializer.Deserialize<RoomRuntimeCoordinationSnapshot>(payload.ToString());
        }
        catch (RedisConnectionException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return await _localFallback.GetSnapshotAsync(roomId, cancellationToken);
        }
        catch (RedisTimeoutException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return await _localFallback.GetSnapshotAsync(roomId, cancellationToken);
        }
    }

    private static string BuildSnapshotKey(RoomId roomId)
    {
        return SnapshotKeyPrefix + roomId.Value;
    }

    private sealed record RoomRuntimeMutationEnvelope(
        long RoomId,
        string MutationKind,
        long Version,
        DateTime OccurredAtUtc);
}
