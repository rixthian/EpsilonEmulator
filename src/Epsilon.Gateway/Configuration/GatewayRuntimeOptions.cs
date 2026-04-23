namespace Epsilon.Gateway;

public sealed class GatewayRuntimeOptions
{
    public const string SectionName = "Gateway";

    public string HotelName { get; set; } = "Epsilon Emulator";
    public string PublicHost { get; set; } = "localhost";
    public int TcpPort { get; set; } = 30000;
    public string RealtimePath { get; set; } = "/realtime";
    public bool RequireTlsForRealtime { get; set; } = true;
    public bool AllowInsecureLoopbackRealtime { get; set; } = true;
    public int RealtimeKeepAliveSeconds { get; set; } = 30;
}
