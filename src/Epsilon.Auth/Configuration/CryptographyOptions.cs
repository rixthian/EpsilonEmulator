namespace Epsilon.Auth;

public sealed class CryptographyOptions
{
    public const string SectionName = "Cryptography";

    public int Pbkdf2IterationCount { get; set; } = 600_000;
    public int SaltSizeBytes { get; set; } = 32;
    public int SubkeySizeBytes { get; set; } = 32;
}
