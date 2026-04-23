using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Epsilon.Launcher;

public sealed class LauncherAccessCodeStore
{
    private readonly ConcurrentDictionary<string, LauncherAccessCodeSnapshot> byCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> activeCodeByTicket = new(StringComparer.Ordinal);

    public LauncherAccessCodeSnapshot Issue(string ticket, string? platformKind)
    {
        ClearExpired();

        if (activeCodeByTicket.TryGetValue(ticket, out string? existingCode)
            && byCode.TryGetValue(existingCode, out LauncherAccessCodeSnapshot? existing)
            && existing.ExpiresAtUtc > DateTime.UtcNow
            && existing.RedeemedAtUtc is null)
        {
            return existing;
        }

        string code = GenerateCode();
        DateTime now = DateTime.UtcNow;
        LauncherAccessCodeSnapshot snapshot = new(
            Code: code,
            Ticket: ticket,
            PlatformKind: string.IsNullOrWhiteSpace(platformKind) ? null : platformKind.Trim(),
            IssuedAtUtc: now,
            ExpiresAtUtc: now.AddMinutes(10),
            RedeemedAtUtc: null);

        byCode[code] = snapshot;
        activeCodeByTicket[ticket] = code;
        return snapshot;
    }

    public LauncherAccessCodeSnapshot? GetCurrentByTicket(string ticket)
    {
        ClearExpired();

        if (!activeCodeByTicket.TryGetValue(ticket, out string? code))
        {
            return null;
        }

        if (!byCode.TryGetValue(code, out LauncherAccessCodeSnapshot? snapshot))
        {
            activeCodeByTicket.TryRemove(ticket, out _);
            return null;
        }

        if (snapshot.ExpiresAtUtc <= DateTime.UtcNow || snapshot.RedeemedAtUtc is not null)
        {
            byCode.TryRemove(code, out _);
            activeCodeByTicket.TryRemove(ticket, out _);
            return null;
        }

        return snapshot;
    }

    public LauncherAccessCodeSnapshot? Redeem(string code)
    {
        ClearExpired();

        if (!byCode.TryGetValue(code, out LauncherAccessCodeSnapshot? snapshot))
        {
            return null;
        }

        if (snapshot.ExpiresAtUtc <= DateTime.UtcNow || snapshot.RedeemedAtUtc is not null)
        {
            byCode.TryRemove(code, out _);
            activeCodeByTicket.TryRemove(snapshot.Ticket, out _);
            return null;
        }

        LauncherAccessCodeSnapshot redeemed = snapshot with { RedeemedAtUtc = DateTime.UtcNow };
        byCode[code] = redeemed;
        activeCodeByTicket.TryRemove(snapshot.Ticket, out _);
        return redeemed;
    }

    private void ClearExpired()
    {
        DateTime now = DateTime.UtcNow;
        foreach ((string code, LauncherAccessCodeSnapshot snapshot) in byCode)
        {
            if (snapshot.ExpiresAtUtc > now && snapshot.RedeemedAtUtc is null)
            {
                continue;
            }

            byCode.TryRemove(code, out _);
            activeCodeByTicket.TryRemove(snapshot.Ticket, out _);
        }
    }

    private static string GenerateCode()
    {
        Span<byte> bytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(bytes);
        string raw = Convert.ToHexString(bytes);
        return string.Create(14, raw, static (buffer, state) =>
        {
            state.AsSpan(0, 4).CopyTo(buffer);
            buffer[4] = '-';
            state.AsSpan(4, 4).CopyTo(buffer[5..]);
            buffer[9] = '-';
            state.AsSpan(8, 4).CopyTo(buffer[10..]);
        });
    }
}
