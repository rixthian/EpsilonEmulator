using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(options => options.SessionTtlMinutes > 0, "Auth session TTL must be greater than zero.")
            .Validate(options => options.TicketLength is >= 32 and <= 256, "Auth ticket length must be between 32 and 256.")
            .Validate(
                options => string.IsNullOrWhiteSpace(options.RedisConnectionString) || !options.AllowInMemorySessions,
                "Auth.RedisConnectionString should be used with AllowInMemorySessions disabled to prevent process-local session drift.")
            .ValidateOnStart();
        services.AddOptions<CryptographyOptions>()
            .Bind(configuration.GetSection(CryptographyOptions.SectionName))
            .Validate(options => options.Pbkdf2IterationCount >= 600_000, "PBKDF2 iteration count must be at least 600000.")
            .Validate(options => options.SaltSizeBytes >= 16, "Cryptography salt size must be at least 16 bytes.")
            .Validate(options => options.SubkeySizeBytes >= 32, "Cryptography subkey size must be at least 32 bytes.")
            .ValidateOnStart();

        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<ITicketGenerator, Base64UrlTicketGenerator>();
        services.AddSingleton<IPasswordHashService, Pbkdf2PasswordHashService>();
        services.AddSingleton<InMemorySessionStore>();
        services.AddSingleton<RedisSessionStore>();
        services.AddSingleton<ISessionStore>(provider =>
        {
            AuthOptions options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.RedisConnectionString) && !options.AllowInMemorySessions)
            {
                return provider.GetRequiredService<RedisSessionStore>();
            }

            return provider.GetRequiredService<InMemorySessionStore>();
        });
        services.AddSingleton<IAuthenticator, DevelopmentAuthenticator>();

        return services;
    }
}
