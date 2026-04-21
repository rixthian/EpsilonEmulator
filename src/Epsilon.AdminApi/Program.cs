using Epsilon.AdminApi;
using Epsilon.Persistence;
using Epsilon.CoreGame;
using Microsoft.Extensions.Options;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "EPSILON_");

builder.Services.AddAdminRuntime(builder.Configuration);
builder.Services.AddPersistenceRuntime(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", (IOptions<AdminRuntimeOptions> adminOptions) => Results.Ok(new
{
    service = adminOptions.Value.ServiceName,
    status = "ok",
    version = ResolveInformationalVersion(typeof(AdminRuntimeOptions).Assembly),
    utc = DateTime.UtcNow
}));

app.MapGet("/readiness", (IPersistenceReadinessChecker persistenceChecker) =>
{
    PersistenceReadinessReport report = persistenceChecker.Check();
    return report.IsReady ? Results.Ok(report) : Results.Problem(
        detail: string.Join(" ", report.Issues),
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Infrastructure is not ready.");
});

app.MapGet("/housekeeping/characters/{characterId:long}", async (
    long characterId,
    IHousekeepingSnapshotService housekeepingSnapshotService,
    CancellationToken cancellationToken) =>
{
    HousekeepingSnapshot? snapshot = await housekeepingSnapshotService.BuildAsync(new CharacterId(characterId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.Run();

static string ResolveInformationalVersion(Assembly assembly)
{
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}
