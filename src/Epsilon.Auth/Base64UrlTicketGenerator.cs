using System.Security.Cryptography;

namespace Epsilon.Auth;

public sealed class Base64UrlTicketGenerator : ITicketGenerator
{
    public string Generate(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Ticket length must be greater than zero.");
        }

        int byteCount = Math.Max(24, length);
        byte[] buffer = RandomNumberGenerator.GetBytes(byteCount);
        string ticket = Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", string.Empty);

        return ticket.Length <= length ? ticket : ticket[..length];
    }
}

