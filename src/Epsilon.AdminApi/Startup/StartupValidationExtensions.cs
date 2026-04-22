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
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Infrastructure provider is required.")
            .Validate(options =>
                    !string.Equals(options.Provider, "Mongo", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(options.MongoConnectionString),
                "MongoDB connection string is required when Infrastructure.Provider is Mongo.")
            .Validate(options =>
                    !string.Equals(options.Provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(options.PostgresConnectionString),
                "PostgreSQL connection string is required when Infrastructure.Provider is Postgres.")
            .ValidateOnStart();

        return services;
    }
}
