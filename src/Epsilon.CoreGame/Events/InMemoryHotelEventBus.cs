using System.Collections.Concurrent;

namespace Epsilon.CoreGame;

public sealed class InMemoryHotelEventBus : IHotelEventBus
{
    private const int MaximumHistorySize = 512;

    private readonly ConcurrentQueue<HotelEventEnvelope> _history = new();
    private readonly object _subscriptionGate = new();
    private List<HotelEventHandler> _subscribers = [];

    public ValueTask PublishAsync<TPayload>(
        HotelEventKind kind,
        TPayload payload,
        CharacterId? actorCharacterId = null,
        RoomId? roomId = null,
        CancellationToken cancellationToken = default)
        where TPayload : class
    {
        return PublishAsync(
            new HotelEventEnvelope(
                Guid.NewGuid(),
                kind,
                DateTime.UtcNow,
                actorCharacterId,
                roomId,
                payload),
            cancellationToken);
    }

    public async ValueTask PublishAsync(
        HotelEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        _history.Enqueue(envelope);
        while (_history.Count > MaximumHistorySize && _history.TryDequeue(out _))
        {
        }

        HotelEventHandler[] subscribers;
        lock (_subscriptionGate)
        {
            subscribers = _subscribers.ToArray();
        }

        foreach (HotelEventHandler subscriber in subscribers)
        {
            try
            {
                await subscriber(envelope, cancellationToken);
            }
            catch
            {
                // Event subscribers are sidecar observers. They must not be allowed
                // to interrupt the authoritative hotel action that already succeeded.
            }
        }
    }

    public IDisposable Subscribe(HotelEventHandler handler)
    {
        lock (_subscriptionGate)
        {
            _subscribers.Add(handler);
        }

        return new Subscription(this, handler);
    }

    public ValueTask<IReadOnlyList<HotelEventEnvelope>> GetRecentAsync(
        int maxCount = 128,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxCount <= 0)
        {
            return ValueTask.FromResult<IReadOnlyList<HotelEventEnvelope>>([]);
        }

        HotelEventEnvelope[] snapshot = _history.ToArray();
        HotelEventEnvelope[] recent = snapshot
            .TakeLast(Math.Min(maxCount, snapshot.Length))
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<HotelEventEnvelope>>(recent);
    }

    private void Unsubscribe(HotelEventHandler handler)
    {
        lock (_subscriptionGate)
        {
            _subscribers = _subscribers
                .Where(candidate => candidate != handler)
                .ToList();
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly InMemoryHotelEventBus _owner;
        private readonly HotelEventHandler _handler;
        private int _disposed;

        public Subscription(InMemoryHotelEventBus owner, HotelEventHandler handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _owner.Unsubscribe(_handler);
        }
    }
}
