namespace Epsilon.Auth;

public interface IPasswordHashService
{
    PasswordHashRecord HashPassword(string password);
    PasswordVerificationResult VerifyPassword(string password, PasswordHashRecord hash);
}
