using Epsilon.Gateway;
using Epsilon.Persistence;
using Epsilon.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class ProtocolHealthMonitorTests
{
    [Fact]
    public void Refresh_ReportsHealthy_WhenSelfCheckPassesAndTrafficIsRecent()
    {
        var packetLogger = new InMemoryPacketLogger();
        packetLogger.Log(new PacketLogEntry(
            DateTime.UtcNow,
            "inbound",
            "rooms.chat",
            1,
            "127.0.0.1",
            StatusCodes.Status200OK,
            12));

        ProtocolHealthMonitor monitor = CreateMonitor(packetLogger, null);

        monitor.Refresh();
        ProtocolHealthSnapshot snapshot = monitor.GetSnapshot();

        Assert.Equal(ProtocolHealthState.Healthy, snapshot.State);
        Assert.True(snapshot.SelfCheck.IsHealthy);
        Assert.Equal(1, snapshot.RecentPacketCount);
        Assert.Empty(snapshot.Alerts);
    }

    [Fact]
    public void Refresh_ReportsWarning_WhenRecentServerErrorsExist()
    {
        var packetLogger = new InMemoryPacketLogger();
        packetLogger.Log(new PacketLogEntry(
            DateTime.UtcNow,
            "inbound",
            "rooms.chat",
            1,
            "127.0.0.1",
            StatusCodes.Status500InternalServerError,
            48));

        ProtocolHealthMonitor monitor = CreateMonitor(packetLogger, null);

        monitor.Refresh();
        ProtocolHealthSnapshot snapshot = monitor.GetSnapshot();

        Assert.Equal(ProtocolHealthState.Warning, snapshot.State);
        Assert.Contains(snapshot.Alerts, alert => alert.Contains("server error responses", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Refresh_ReportsCritical_WhenManifestSelfCheckFails()
    {
        string tempCommandManifest = Path.Combine(
            Path.GetTempPath(),
            $"epsilon-bad-protocol-{Guid.NewGuid():N}.json");

        try
        {
            string sourceManifest = Path.Combine(
                GetRepositoryRoot(),
                "src",
                "Epsilon.Protocol",
                "command-manifests",
                "release63.commands.json");

            string json = File.ReadAllText(sourceManifest)
                .Replace("\"packetName\": \"init_crypto\"", "\"packetName\": \"missing_packet_name\"", StringComparison.Ordinal);
            File.WriteAllText(tempCommandManifest, json);

            var packetLogger = new InMemoryPacketLogger();
            packetLogger.Log(new PacketLogEntry(
                DateTime.UtcNow,
                "inbound",
                "session.initialize_crypto",
                null,
                "127.0.0.1",
                StatusCodes.Status200OK,
                3));

            ProtocolHealthMonitor monitor = CreateMonitor(packetLogger, tempCommandManifest);

            monitor.Refresh();
            ProtocolHealthSnapshot snapshot = monitor.GetSnapshot();

            Assert.Equal(ProtocolHealthState.Critical, snapshot.State);
            Assert.False(snapshot.SelfCheck.IsHealthy);
            Assert.Contains(snapshot.Alerts, alert => alert.Contains("missing incoming packet", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(tempCommandManifest))
            {
                File.Delete(tempCommandManifest);
            }
        }
    }

    [Fact]
    public void Refresh_ReportsWarning_WhenRealtimeTransportHasBeenIdleTooLong()
    {
        var packetLogger = new InMemoryPacketLogger();
        var realtimeConnectionMonitor = new RealtimeConnectionMonitor();
        realtimeConnectionMonitor.RecordAcceptedConnection();
        realtimeConnectionMonitor.RecordClosedConnection();

        ProtocolHealthMonitor monitor = CreateMonitor(
            packetLogger,
            realtimeConnectionMonitor,
            null,
            new ProtocolHealthMonitorOptions
            {
                CheckIntervalSeconds = 30,
                RecentPacketWindowSeconds = 300,
                IdleWarningSeconds = 3_600,
                IdleCriticalSeconds = 7_200,
                RealtimeIdleWarningSeconds = 1,
                RealtimeIdleCriticalSeconds = 60
            });

        Thread.Sleep(1200);

        monitor.Refresh();
        ProtocolHealthSnapshot snapshot = monitor.GetSnapshot();

        Assert.Equal(ProtocolHealthState.Warning, snapshot.State);
        Assert.Contains(snapshot.Alerts, alert => alert.Contains("Realtime transport has been idle", StringComparison.OrdinalIgnoreCase));
    }

    private static ProtocolHealthMonitor CreateMonitor(
        InMemoryPacketLogger packetLogger,
        string? commandManifestOverride)
    {
        return CreateMonitor(
            packetLogger,
            new RealtimeConnectionMonitor(),
            commandManifestOverride,
            new ProtocolHealthMonitorOptions
            {
                CheckIntervalSeconds = 30,
                RecentPacketWindowSeconds = 300,
                IdleWarningSeconds = 180,
                IdleCriticalSeconds = 600,
                RealtimeIdleWarningSeconds = 300,
                RealtimeIdleCriticalSeconds = 900
            });
    }

    private static ProtocolHealthMonitor CreateMonitor(
        InMemoryPacketLogger packetLogger,
        IRealtimeConnectionMonitor realtimeConnectionMonitor,
        string? commandManifestOverride,
        ProtocolHealthMonitorOptions options)
    {
        string repositoryRoot = GetRepositoryRoot();
        string packetManifestPath = Path.Combine(repositoryRoot, "src", "Epsilon.Protocol", "packet-manifests", "release63.json");
        string commandManifestPath = commandManifestOverride ?? Path.Combine(
            repositoryRoot,
            "src",
            "Epsilon.Protocol",
            "command-manifests",
            "release63.commands.json");

        PacketManifestOptions packetManifestOptions = new()
        {
            Family = "RELEASE63",
            ManifestPath = packetManifestPath,
            CommandManifestPath = commandManifestPath
        };

        ProtocolSelfCheckService selfCheckService = new(
            new PacketRegistry(new PacketManifestLoader(Options.Create(packetManifestOptions))),
            new ProtocolCommandRegistry(new ProtocolCommandManifestLoader(Options.Create(packetManifestOptions))));

        return new ProtocolHealthMonitor(
            selfCheckService,
            packetLogger,
            realtimeConnectionMonitor,
            Options.Create(options),
            NullLogger<ProtocolHealthMonitor>.Instance);
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

        throw new InvalidOperationException("Repository root could not be resolved for protocol monitor tests.");
    }
}
