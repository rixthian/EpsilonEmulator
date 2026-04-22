using Epsilon.Auth;
using Epsilon.Persistence;
using Epsilon.Protocol;

namespace Epsilon.Gateway;

internal static class StartupBanner
{
    private static readonly string[] LogoLines =
    [
        "==============================================================",
        "",
        "███████╗██████╗ ███████╗██╗██╗      ██████╗ ███╗   ██╗",
        "██╔════╝██╔══██╗██╔════╝██║██║     ██╔═══██╗████╗  ██║",
        "█████╗  ██████╔╝███████╗██║██║     ██║   ██║██╔██╗ ██║",
        "██╔══╝  ██╔═══╝ ╚════██║██║██║     ██║   ██║██║╚██╗██║",
        "███████╗██║     ███████║██║███████╗╚██████╔╝██║ ╚████║",
        "╚══════╝╚═╝     ╚══════╝╚═╝╚══════╝ ╚═════╝ ╚═╝  ╚═══╝",
        "",
        "=============================================================="
    ];

    public static void Print(
        GatewayRuntimeOptions gatewayOptions,
        PersistenceOptions persistenceOptions,
        AuthOptions authOptions,
        PacketRegistry packetRegistry,
        string version,
        string environmentName)
    {
        ConsoleColor originalForeground = Console.ForegroundColor;
        ConsoleColor originalBackground = Console.BackgroundColor;

        try
        {
            WriteLogo();
            WriteSection(
                "BOOT",
                [
                    $"Version      : {version}",
                    $"Hotel        : {gatewayOptions.HotelName}",
                    $"Environment  : {environmentName}",
                    $"Endpoint     : {gatewayOptions.PublicHost}:{gatewayOptions.TcpPort}",
                    $"Protocol     : {packetRegistry.Family}",
                    $"Packet In    : {packetRegistry.Incoming.Count}",
                    $"Packet Out   : {packetRegistry.Outgoing.Count}",
                    $"Persistence  : {persistenceOptions.Provider}",
                    $"Redis        : {DescribeRedis(persistenceOptions)}",
                    $"Sessions     : {DescribeSessions(authOptions)}"
                ]);

            WriteSection(
                "TIME",
                [
                    $"Utc          : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    $"Local        : {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}"
                ]);
        }
        finally
        {
            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
        }
    }

    private static void WriteLogo()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        foreach (string line in LogoLines)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine();
    }

    private static void WriteSection(string title, IReadOnlyList<string> lines)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"+-------------------- {title} --------------------+");

        Console.ForegroundColor = ConsoleColor.Gray;
        foreach (string line in lines)
        {
            Console.WriteLine($"| {line.PadRight(47)} |");
        }

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("+------------------------------------------------+");
        Console.WriteLine();
    }

    private static string DescribeRedis(PersistenceOptions persistenceOptions)
    {
        return string.IsNullOrWhiteSpace(persistenceOptions.RedisConnectionString)
            ? "disabled"
            : "configured";
    }

    private static string DescribeSessions(AuthOptions authOptions)
    {
        if (!string.IsNullOrWhiteSpace(authOptions.RedisConnectionString) && !authOptions.AllowInMemorySessions)
        {
            return "redis_shared";
        }

        return authOptions.AllowInMemorySessions ? "in_memory" : "custom";
    }
}
