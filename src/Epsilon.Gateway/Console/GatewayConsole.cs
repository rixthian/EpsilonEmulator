namespace Epsilon.Gateway;

internal enum GatewayConsoleEventKind
{
    Info,
    Ok,
    Time,
    Network,
    Packet,
    Warning,
    Security,
    Error,
    Bug,
    Alert
}

internal static class GatewayConsole
{
    public static void WriteEvent(GatewayConsoleEventKind kind, string message)
    {
        ConsoleColor originalForeground = Console.ForegroundColor;

        try
        {
            Console.ForegroundColor = GetColor(kind);
            Console.Write($"[{DateTimeOffset.Now:HH:mm:ss}] [{GetLabel(kind)}] ");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalForeground;
        }
    }

    private static string GetLabel(GatewayConsoleEventKind kind)
    {
        return kind switch
        {
            GatewayConsoleEventKind.Ok => "OK  ",
            GatewayConsoleEventKind.Time => "TIME",
            GatewayConsoleEventKind.Network => "NET ",
            GatewayConsoleEventKind.Packet => "PACK",
            GatewayConsoleEventKind.Warning => "WARN",
            GatewayConsoleEventKind.Security => "SECU",
            GatewayConsoleEventKind.Error => "ERR ",
            GatewayConsoleEventKind.Bug => "BUG ",
            GatewayConsoleEventKind.Alert => "ALRT",
            _ => "INFO"
        };
    }

    private static ConsoleColor GetColor(GatewayConsoleEventKind kind)
    {
        return kind switch
        {
            GatewayConsoleEventKind.Ok => ConsoleColor.Green,
            GatewayConsoleEventKind.Time => ConsoleColor.Cyan,
            GatewayConsoleEventKind.Network => ConsoleColor.Blue,
            GatewayConsoleEventKind.Packet => ConsoleColor.Magenta,
            GatewayConsoleEventKind.Warning => ConsoleColor.Yellow,
            GatewayConsoleEventKind.Security => ConsoleColor.DarkYellow,
            GatewayConsoleEventKind.Error => ConsoleColor.Red,
            GatewayConsoleEventKind.Bug => ConsoleColor.DarkRed,
            GatewayConsoleEventKind.Alert => ConsoleColor.DarkMagenta,
            _ => ConsoleColor.White
        };
    }
}
