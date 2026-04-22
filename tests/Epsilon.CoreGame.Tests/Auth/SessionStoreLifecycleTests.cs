using Epsilon.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class SessionStoreLifecycleTests
{
    [Fact]
    public async Task InMemorySessionStore_TouchAndRevoke_WorkAsExpected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemorySessionStore store = new();
        DateTime createdAtUtc = DateTime.UtcNow;
        SessionTicket original = new(
            Guid.NewGuid(),
            AccountId: 1,
            CharacterId: 7,
            Ticket: "ticket-alpha",
            RemoteAddress: "127.0.0.1",
            CreatedAtUtc: createdAtUtc,
            LastSeenAtUtc: createdAtUtc,
            ExpiresAtUtc: createdAtUtc.AddMinutes(120));

        await store.StoreAsync(original, cancellationToken);

        SessionTicket? touched = await store.TouchAsync("ticket-alpha", "127.0.0.2", cancellationToken);
        bool revoked = await store.RevokeAsync("ticket-alpha", cancellationToken);
        SessionTicket? missing = await store.FindByTicketAsync("ticket-alpha", cancellationToken);

        Assert.NotNull(touched);
        Assert.Equal("127.0.0.2", touched!.RemoteAddress);
        Assert.True(touched.LastSeenAtUtc >= original.LastSeenAtUtc);
        Assert.True(revoked);
        Assert.Null(missing);
    }

    [Fact]
    public async Task RedisSessionStore_WithoutConnection_FailsClosed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RedisSessionStore store = new(Options.Create(new AuthOptions
        {
            RedisConnectionString = string.Empty
        }));

        SessionTicket? touched = await store.TouchAsync("missing-ticket", cancellationToken: cancellationToken);
        bool revoked = await store.RevokeAsync("missing-ticket", cancellationToken);

        Assert.Null(touched);
        Assert.False(revoked);
        await store.DisposeAsync();
    }
}
