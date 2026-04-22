using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Epsilon.Auth;

public sealed class Pbkdf2PasswordHashService : IPasswordHashService
{
    private readonly CryptographyOptions _options;

    public Pbkdf2PasswordHashService(IOptions<CryptographyOptions> options)
    {
        _options = options.Value;
    }

    public PasswordHashRecord HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        byte[] subkey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            _options.Pbkdf2IterationCount,
            HashAlgorithmName.SHA256,
            _options.SubkeySizeBytes);

        return new PasswordHashRecord(
            PasswordHashAlgorithm.Pbkdf2Sha256V1,
            _options.Pbkdf2IterationCount,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(subkey));
    }

    public PasswordVerificationResult VerifyPassword(string password, PasswordHashRecord hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(hash);

        if (hash.Algorithm is not PasswordHashAlgorithm.Pbkdf2Sha256V1)
        {
            return new PasswordVerificationResult(false, false, null);
        }

        byte[] salt = Convert.FromBase64String(hash.SaltBase64);
        byte[] expectedSubkey = Convert.FromBase64String(hash.SubkeyBase64);
        byte[] actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            hash.IterationCount,
            HashAlgorithmName.SHA256,
            expectedSubkey.Length);

        bool succeeded = CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        bool requiresRehash = succeeded &&
            (hash.IterationCount != _options.Pbkdf2IterationCount ||
             salt.Length != _options.SaltSizeBytes ||
             expectedSubkey.Length != _options.SubkeySizeBytes);

        PasswordHashRecord? upgradedHash = requiresRehash ? HashPassword(password) : null;
        return new PasswordVerificationResult(succeeded, requiresRehash, upgradedHash);
    }
}
