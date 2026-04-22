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
        if (string.IsNullOrWhiteSpace(request.Username) || !UsernamePattern.IsMatch(request.Username))
        {
            return new RegistrationResult(false, "username_invalid", null, null);
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailPattern.IsMatch(request.Email))
        {
            return new RegistrationResult(false, "email_invalid", null, null);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return new RegistrationResult(false, "password_too_short", null, null);
        }

        CharacterProfile? existingUsername =
            await _characterProfileRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUsername is not null)
        {
            return new RegistrationResult(false, "username_taken", null, null);
        }

        AccountRecord? existingEmail =
            await _accountRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail is not null)
        {
            return new RegistrationResult(false, "email_taken", null, null);
        }

        PasswordHashRecord hashRecord = _passwordHashService.HashPassword(request.Password);
        string passwordHashJson = hashRecord.ToString();

        AccountId accountId = await _accountRepository.CreateAsync(
            request.Email,
            passwordHashJson,
            cancellationToken);

        CharacterProfile profile = await _characterProfileRepository.CreateAsync(
            accountId,
            request.Username,
            DefaultHomeRoomId,
            cancellationToken);

        return new RegistrationResult(true, null, accountId.Value, profile.CharacterId.Value);
    }
}
