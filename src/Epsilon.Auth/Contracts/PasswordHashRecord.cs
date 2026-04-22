namespace Epsilon.Auth;

public sealed record PasswordHashRecord(
    PasswordHashAlgorithm Algorithm,
    int IterationCount,
    string SaltBase64,
    string SubkeyBase64)
{
    public override string ToString()
    {
        return $"epsilon-pwd$1${Algorithm}$i={IterationCount}${SaltBase64}${SubkeyBase64}";
    }

    public static bool TryParse(string encoded, out PasswordHashRecord? record)
    {
        record = null;

        if (string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        string[] parts = encoded.Split('$', StringSplitOptions.None);
        if (parts.Length != 6)
        {
            return false;
        }

        if (!string.Equals(parts[0], "epsilon-pwd", StringComparison.Ordinal) ||
            !string.Equals(parts[1], "1", StringComparison.Ordinal))
        {
            return false;
        }

        if (!Enum.TryParse(parts[2], ignoreCase: false, out PasswordHashAlgorithm algorithm))
        {
            return false;
        }

        if (!parts[3].StartsWith("i=", StringComparison.Ordinal) ||
            !int.TryParse(parts[3].AsSpan(2), out int iterations) ||
            iterations <= 0)
        {
            return false;
        }

        record = new PasswordHashRecord(
            algorithm,
            iterations,
            parts[4],
            parts[5]);
        return true;
    }
}
