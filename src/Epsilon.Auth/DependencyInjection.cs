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
            .ValidateOnStart();

        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<ITicketGenerator, Base64UrlTicketGenerator>();
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<IAuthenticator, DevelopmentAuthenticator>();

        return services;
    }
}

