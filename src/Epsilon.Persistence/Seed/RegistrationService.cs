using System.Text.RegularExpressions;
using Epsilon.Auth;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class RegistrationService : IRegistrationService
{
    // Default home room — first public room in the hotel.
    private static readonly RoomId DefaultHomeRoomId = new(1);

    private static readonly Regex UsernamePattern =
        new(@"^[a-zA-Z0-9_\-\.]{3,24}$", RegexOptions.Compiled);

    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IAccountRepository _accountRepository;
    private readonly ICharacterProfileRepository _characterProfileRepository;
    private readonly IPasswordHashService _passwordHashService;

    public RegistrationService(
        IAccountRepository accountRepository,
        ICharacterProfileRepository characterProfileRepository,
        IPasswordHashService passwordHashService)
    {
        _accountRepository = accountRepository;
        _characterProfileRepository = characterProfileRepository;
        _passwordHashService = passwordHashService;
    }

    public async ValueTask<RegistrationResult> RegisterAsync(
        RegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        string normalizedUsername = request.Username?.Trim() ?? string.Empty;
        string normalizedEmail = request.Email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedUsername) || !UsernamePattern.IsMatch(normalizedUsername))
        {
            return new RegistrationResult(false, "username_invalid", null, null);
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail) || !EmailPattern.IsMatch(normalizedEmail))
        {
            return new RegistrationResult(false, "email_invalid", null, null);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return new RegistrationResult(false, "password_too_short", null, null);
        }

        CharacterProfile? existingUsername =
            await _characterProfileRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);
        if (existingUsername is not null)
        {
            return new RegistrationResult(false, "username_taken", null, null);
        }

        AccountRecord? existingEmail =
            await _accountRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingEmail is not null)
        {
            return new RegistrationResult(false, "email_taken", null, null);
        }

        PasswordHashRecord hashRecord = _passwordHashService.HashPassword(request.Password);
        string passwordHashJson = hashRecord.ToString();

        try
        {
            AccountId accountId = await _accountRepository.CreateAsync(
                normalizedEmail,
                passwordHashJson,
                cancellationToken);

            CharacterProfile profile = await _characterProfileRepository.CreateAsync(
                accountId,
                normalizedUsername,
                DefaultHomeRoomId,
                cancellationToken);

            return new RegistrationResult(true, null, accountId.Value, profile.CharacterId.Value);
        }
        catch (InvalidOperationException exception) when (
            string.Equals(exception.Message, "username_taken", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(exception.Message, "email_taken", StringComparison.OrdinalIgnoreCase))
        {
            return new RegistrationResult(false, exception.Message, null, null);
        }
    }
}
