using Epsilon.Persistence;
using Microsoft.Extensions.Options;

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

app.Run();
