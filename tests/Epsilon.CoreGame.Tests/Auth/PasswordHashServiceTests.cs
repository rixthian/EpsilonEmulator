using Epsilon.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class PasswordHashServiceTests
{
    private static Pbkdf2PasswordHashService CreateService(
        int iterations = 600_000,
        int saltSizeBytes = 32,
        int subkeySizeBytes = 32)
    {
        return new Pbkdf2PasswordHashService(
            Options.Create(new CryptographyOptions
            {
                Pbkdf2IterationCount = iterations,
                SaltSizeBytes = saltSizeBytes,
                SubkeySizeBytes = subkeySizeBytes
            }));
    }

    [Fact]
    public void HashPassword_ProducesParseableVersionedRecord()
    {
        Pbkdf2PasswordHashService service = CreateService();

        PasswordHashRecord hash = service.HashPassword("correct horse battery staple");

        Assert.True(PasswordHashRecord.TryParse(hash.ToString(), out PasswordHashRecord? parsed));
        Assert.NotNull(parsed);
        Assert.Equal(PasswordHashAlgorithm.Pbkdf2Sha256V1, parsed!.Algorithm);
        Assert.Equal(600_000, parsed.IterationCount);
    }

    [Fact]
    public void VerifyPassword_SucceedsForMatchingPassword()
    {
        Pbkdf2PasswordHashService service = CreateService();
        PasswordHashRecord hash = service.HashPassword("epsilon-secret");

        PasswordVerificationResult result = service.VerifyPassword("epsilon-secret", hash);

        Assert.True(result.Succeeded);
        Assert.False(result.RequiresRehash);
        Assert.Null(result.UpgradedHash);
    }

    [Fact]
    public void VerifyPassword_FailsForWrongPassword()
    {
        Pbkdf2PasswordHashService service = CreateService();
        PasswordHashRecord hash = service.HashPassword("epsilon-secret");

        PasswordVerificationResult result = service.VerifyPassword("wrong-secret", hash);

        Assert.False(result.Succeeded);
        Assert.False(result.RequiresRehash);
        Assert.Null(result.UpgradedHash);
    }

    [Fact]
    public void VerifyPassword_ReturnsUpgradeWhenPolicyChanges()
    {
        Pbkdf2PasswordHashService oldService = CreateService(iterations: 600_000);
        PasswordHashRecord oldHash = oldService.HashPassword("epsilon-secret");

        Pbkdf2PasswordHashService newService = CreateService(iterations: 700_000);
        PasswordVerificationResult result = newService.VerifyPassword("epsilon-secret", oldHash);

        Assert.True(result.Succeeded);
        Assert.True(result.RequiresRehash);
        Assert.NotNull(result.UpgradedHash);
        Assert.Equal(700_000, result.UpgradedHash!.IterationCount);
    }
}
