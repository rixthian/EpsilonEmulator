namespace Epsilon.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int SessionTtlMinutes { get; set; } = 120;
    public int TicketLength { get; set; } = 48;
    public bool AllowInMemorySessions { get; set; } = true;
    public string RedisConnectionString { get; set; } = string.Empty;
    public bool AllowRemoteDevelopmentAuth { get; set; }
}
