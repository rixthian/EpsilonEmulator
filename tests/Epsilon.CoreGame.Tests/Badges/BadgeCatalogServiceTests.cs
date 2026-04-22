using Epsilon.Content;
using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class BadgeCatalogServiceTests
{
    [Fact]
    public async Task GetBadge_ReturnsKnownBadgeDefinition()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ServiceProvider services = BuildServices();
        IBadgeCatalogService badgeCatalogService = services.GetRequiredService<IBadgeCatalogService>();

        BadgeDefinition? badge = await badgeCatalogService.GetBadgeAsync("ACH_Tutorial5", cancellationToken);

        Assert.NotNull(badge);
        Assert.Equal("ACH_Tutorial5", badge!.BadgeCode);
        Assert.Equal("achievement", badge.BadgeGroup);
        Assert.Equal("badges/library/ACH_Tutorial5.gif", badge.AssetPath);
        Assert.Equal("gif", badge.AssetKind);
    }

    [Fact]
    public async Task Search_ReturnsBoundedBadgeResults()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ServiceProvider services = BuildServices();
        IBadgeCatalogService badgeCatalogService = services.GetRequiredService<IBadgeCatalogService>();

        IReadOnlyList<BadgeDefinition> badges = await badgeCatalogService.SearchAsync("ACH_", 5, cancellationToken);

        Assert.Equal(5, badges.Count);
        Assert.All(badges, badge => Assert.Contains("ACH_", badge.BadgeCode, StringComparison.OrdinalIgnoreCase));
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddGameRuntime();
        services.AddCoreGameRuntime();
        return services.BuildServiceProvider();
    }
}
