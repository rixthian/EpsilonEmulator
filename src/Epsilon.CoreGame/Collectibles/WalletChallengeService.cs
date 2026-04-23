using System.Security.Cryptography;

namespace Epsilon.CoreGame;

public sealed class WalletChallengeService : IWalletChallengeService
{
    private static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);

    private readonly IWalletLinkRepository _walletLinkRepository;
    private readonly Dictionary<string, WalletChallengeSnapshot> _activeChallenges = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public WalletChallengeService(IWalletLinkRepository walletLinkRepository)
    {
        _walletLinkRepository = walletLinkRepository;
    }

    public ValueTask<WalletChallengeSnapshot> IssueAsync(
        CharacterId characterId,
        string walletAddress,
        string walletProvider,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new ArgumentException("Wallet address is required.", nameof(walletAddress));
        }

        string normalizedAddress = walletAddress.Trim();
        string normalizedProvider = string.IsNullOrWhiteSpace(walletProvider) ? "metamask" : walletProvider.Trim().ToLowerInvariant();
        DateTime createdAtUtc = DateTime.UtcNow;
        string challengeId = Guid.NewGuid().ToString("N");
        string nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        string message =
            $"Epsilon wallet link challenge\nCharacter:{characterId.Value}\nWallet:{normalizedAddress}\nNonce:{nonce}";
        WalletChallengeSnapshot challenge = new(
            challengeId,
            characterId,
            normalizedAddress,
            normalizedProvider,
            nonce,
            message,
            "development_challenge",
            $"Use signature value devsig:{nonce}",
            createdAtUtc,
            createdAtUtc.Add(ChallengeTtl));

        lock (_sync)
        {
            RemoveExpiredChallenges(createdAtUtc);
            _activeChallenges[challengeId] = challenge;
        }

        return ValueTask.FromResult(challenge);
    }

    public async ValueTask<WalletLinkSnapshot?> VerifyAndLinkAsync(
        CharacterId characterId,
        string challengeId,
        string signature,
        CancellationToken cancellationToken = default)
    {
        WalletChallengeSnapshot? challenge;

        lock (_sync)
        {
            RemoveExpiredChallenges(DateTime.UtcNow);
            _activeChallenges.TryGetValue(challengeId, out challenge);
        }

        if (challenge is null ||
            challenge.CharacterId != characterId ||
            challenge.ExpiresAtUtc <= DateTime.UtcNow ||
            !string.Equals(signature?.Trim(), $"devsig:{challenge.Nonce}", StringComparison.Ordinal))
        {
            return null;
        }

        IReadOnlyList<WalletLinkSnapshot> existingLinks =
            await _walletLinkRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        WalletLinkSnapshot? currentLink = existingLinks.FirstOrDefault(candidate =>
            string.Equals(candidate.WalletAddress, challenge.WalletAddress, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.WalletProvider, challenge.WalletProvider, StringComparison.OrdinalIgnoreCase));

        WalletLinkSnapshot linked = new(
            characterId,
            challenge.WalletAddress,
            challenge.WalletProvider,
            challenge.VerificationMode,
            IsPrimary: currentLink?.IsPrimary ?? existingLinks.Count == 0,
            LinkedAtUtc: currentLink?.LinkedAtUtc ?? DateTime.UtcNow,
            LastVerifiedAtUtc: DateTime.UtcNow);

        await _walletLinkRepository.StoreAsync(linked, cancellationToken);

        lock (_sync)
        {
            _activeChallenges.Remove(challengeId);
        }

        return linked;
    }

    private void RemoveExpiredChallenges(DateTime utcNow)
    {
        string[] expiredKeys = _activeChallenges.Values
            .Where(candidate => candidate.ExpiresAtUtc <= utcNow)
            .Select(candidate => candidate.ChallengeId)
            .ToArray();

        foreach (string expiredKey in expiredKeys)
        {
            _activeChallenges.Remove(expiredKey);
        }
    }
}
