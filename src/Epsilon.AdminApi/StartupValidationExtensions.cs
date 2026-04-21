namespace Epsilon.AdminApi;

public static class StartupValidationExtensions
{
    public static IServiceCollection AddAdminRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AdminRuntimeOptions>()
            .Bind(configuration.GetSection(AdminRuntimeOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ServiceName), "Admin service name is required.")
            .ValidateOnStart();

        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetSection(InfrastructureOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.PostgresConnectionString), "PostgreSQL connection string is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RedisConnectionString), "Redis connection string is required.")
            .ValidateOnStart();

        return services;
    }
}

