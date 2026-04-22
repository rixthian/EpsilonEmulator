using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Epsilon.Auth;

public sealed class RedisSessionStore : ISessionStore, IAsyncDisposable
{
    private const string SessionKeyPrefix = "epsilon:auth:session:";

    private readonly string _connectionString;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private ConnectionMultiplexer? _multiplexer;
    private DateTime _nextConnectAttemptUtc = DateTime.MinValue;

    public RedisSessionStore(IOptions<AuthOptions> options)
    {
        _connectionString = options.Value.RedisConnectionString;
    }

    public async ValueTask StoreAsync(SessionTicket ticket, CancellationToken cancellationToken = default)
    {
        IDatabase? database = await TryGetDatabaseAsync(cancellationToken);
        if (database is null)
        {
            return;
        }

        try
        {
            await database.StringSetAsync(
                BuildSessionKey(ticket.Ticket),
                JsonSerializer.Serialize(ticket),
                GetTtl(ticket));
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

    public async ValueTask<SessionTicket?> FindByTicketAsync(string ticket, CancellationToken cancellationToken = default)
    {
        IDatabase? database = await TryGetDatabaseAsync(cancellationToken);
        if (database is null)
        {
            return null;
        }

        try
        {
            return await LoadActiveSessionAsync(database, ticket);
        }
        catch (RedisConnectionException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return null;
        }
        catch (RedisTimeoutException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return null;
        }
    }

    public async ValueTask<SessionTicket?> TouchAsync(
        string ticket,
        string? remoteAddress = null,
        CancellationToken cancellationToken = default)
    {
        IDatabase? database = await TryGetDatabaseAsync(cancellationToken);
        if (database is null)
        {
            return null;
        }

        try
        {
            SessionTicket? current = await LoadActiveSessionAsync(database, ticket);
            if (current is null)
            {
                return null;
            }

            SessionTicket updated = current with
            {
                LastSeenAtUtc = DateTime.UtcNow,
                RemoteAddress = string.IsNullOrWhiteSpace(remoteAddress) ? current.RemoteAddress : remoteAddress
            };

            await database.StringSetAsync(
                BuildSessionKey(ticket),
                JsonSerializer.Serialize(updated),
                GetTtl(updated));

            return updated;
        }
        catch (RedisConnectionException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return null;
        }
        catch (RedisTimeoutException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return null;
        }
    }

    public async ValueTask<bool> RevokeAsync(string ticket, CancellationToken cancellationToken = default)
    {
        IDatabase? database = await TryGetDatabaseAsync(cancellationToken);
        if (database is null)
        {
            return false;
        }

        try
        {
            return await database.KeyDeleteAsync(BuildSessionKey(ticket));
        }
        catch (RedisConnectionException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return false;
        }
        catch (RedisTimeoutException)
        {
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
            return false;
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

    private async ValueTask<IDatabase?> TryGetDatabaseAsync(CancellationToken cancellationToken)
    {
        ConnectionMultiplexer? multiplexer = await TryGetMultiplexerAsync(cancellationToken);
        return multiplexer is { IsConnected: true } ? multiplexer.GetDatabase() : null;
    }

    private static TimeSpan GetTtl(SessionTicket ticket)
    {
        return ticket.ExpiresAtUtc > DateTime.UtcNow
            ? ticket.ExpiresAtUtc - DateTime.UtcNow
            : TimeSpan.FromSeconds(1);
    }

    private static async ValueTask<SessionTicket?> LoadActiveSessionAsync(IDatabase database, string ticket)
    {
        RedisValue payload = await database.StringGetAsync(BuildSessionKey(ticket));
        if (!payload.HasValue)
        {
            return null;
        }

        SessionTicket? session = JsonSerializer.Deserialize<SessionTicket>(payload.ToString());
        if (session is null || session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        return session;
    }

    private async ValueTask<ConnectionMultiplexer?> TryGetMultiplexerAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return null;
        }

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

    private static string BuildSessionKey(string ticket)
    {
        return SessionKeyPrefix + ticket;
    }
}
