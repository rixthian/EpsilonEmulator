using Epsilon.Games;
using Epsilon.Gateway;
using Epsilon.Persistence;
using Epsilon.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class HotelIntelligenceServiceTests
{
    [Fact]
    public async Task BuildAsync_DefaultRuntimeProducesRealtimeRecommendation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelIntelligenceService intelligenceService = services.GetRequiredService<IHotelIntelligenceService>();

        HotelIntelligenceSummary summary = await intelligenceService.BuildAsync(cancellationToken);

        Assert.True(summary.Scorecard.Total > 0);
        Assert.Contains(
            summary.Recommendations,
            recommendation => string.Equals(recommendation.ActionKey, "realtime.exercise", StringComparison.Ordinal));
        Assert.True(summary.Signals.RegisteredCommandCount > 0);
    }

    [Fact]
    public async Task BuildAsync_IncludesRecentHotelEventsAfterRuntimeActivity()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IHotelIntelligenceService intelligenceService = services.GetRequiredService<IHotelIntelligenceService>();

        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), "intelligence signal"),
            cancellationToken);

        HotelIntelligenceSummary summary = await intelligenceService.BuildAsync(cancellationToken);

        Assert.True(summary.Signals.RecentEventCount > 0);
        Assert.True(summary.Signals.RecentEventKinds.TryGetValue(nameof(HotelEventKind.ChatMessagePublished), out int count));
        Assert.True(count >= 1);
    }

    private static ServiceProvider BuildServices()
    {
        string repositoryRoot = GetRepositoryRoot();

        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";
        configuration["Infrastructure:RedisConnectionString"] = "localhost:6379";
        configuration["Gateway:HotelName"] = "Epsilon Test Hotel";
        configuration["Gateway:PublicHost"] = "localhost";
        configuration["Gateway:TcpPort"] = "30000";
        configuration["Gateway:RealtimePath"] = "/realtime";
        configuration["Gateway:RequireTlsForRealtime"] = "true";
        configuration["Gateway:AllowInsecureLoopbackRealtime"] = "true";
        configuration["Gateway:RealtimeKeepAliveSeconds"] = "30";
        configuration["ProtocolHealthMonitor:CheckIntervalSeconds"] = "30";
        configuration["ProtocolHealthMonitor:RecentPacketWindowSeconds"] = "300";
        configuration["ProtocolHealthMonitor:IdleWarningSeconds"] = "180";
        configuration["ProtocolHealthMonitor:IdleCriticalSeconds"] = "600";
        configuration["ProtocolHealthMonitor:RealtimeIdleWarningSeconds"] = "300";
        configuration["ProtocolHealthMonitor:RealtimeIdleCriticalSeconds"] = "900";
        configuration["Protocol:Family"] = "RELEASE63";
        configuration["Protocol:ManifestPath"] = Path.Combine(repositoryRoot, "src", "Epsilon.Protocol", "packet-manifests", "release63.json");
        configuration["Protocol:CommandManifestPath"] = Path.Combine(repositoryRoot, "src", "Epsilon.Protocol", "command-manifests", "release63.commands.json");

        ServiceCollection services = new();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddPersistenceRuntime(configuration);
        services.AddGameRuntime();
        services.AddCoreGameRuntime();
        services.AddProtocolServices(configuration);
        services.AddGatewayRuntime(configuration);
        return services.BuildServiceProvider();
    }

    private static string GetRepositoryRoot()
    {
        string current = AppContext.BaseDirectory;
        DirectoryInfo? directory = new(current);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EpsilonEmulator.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be resolved for hotel intelligence tests.");
    }
}
