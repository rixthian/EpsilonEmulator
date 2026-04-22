using Epsilon.Auth;
using Epsilon.CoreGame;
using Epsilon.Launcher;
using Epsilon.Persistence;
using System.Reflection;

const string SessionTicketHeaderName = "X-Epsilon-Session-Ticket";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

AddRootConfiguration(builder.Configuration, builder.Environment.EnvironmentName, "launcher");
builder.Configuration.AddEnvironmentVariables(prefix: "EPSILON_");

builder.Services.AddLauncherRuntime(builder.Configuration);
builder.Services.AddPersistenceRuntime(builder.Configuration);
builder.Services.AddAuthRuntime(builder.Configuration);
builder.Services.AddCoreGameRuntime();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/health", (Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions) => Results.Ok(new
{
    service = launcherOptions.Value.ServiceName,
    status = "ok",
    version = ResolveInformationalVersion(typeof(LauncherRuntimeOptions).Assembly),
    utc = DateTime.UtcNow
}));

app.MapGet("/launcher/profiles", async (
    ILauncherBootstrapService launcherBootstrapService,
    CancellationToken cancellationToken) =>
{
    IReadOnlyList<LauncherClientProfileSnapshot> profiles = await launcherBootstrapService.GetProfilesAsync(cancellationToken);
    return Results.Ok(profiles);
});

app.MapGet("/launcher/bootstrap/{profileKey}", async (
    HttpContext httpContext,
    string profileKey,
    string? deviceKind,
    ILauncherBootstrapService launcherBootstrapService,
    CancellationToken cancellationToken) =>
{
    string? sessionTicket = httpContext.Request.Headers[SessionTicketHeaderName].FirstOrDefault();
    LauncherBootstrapSnapshot? snapshot = await launcherBootstrapService.BuildAsync(
        profileKey,
        sessionTicket,
        httpContext.Request.Headers.UserAgent.FirstOrDefault(),
        TryParseDeviceKind(deviceKind),
        cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/launcher/bootstrap", async (
    HttpContext httpContext,
    string? deviceKind,
    ILauncherBootstrapService launcherBootstrapService,
    CancellationToken cancellationToken) =>
{
    string? sessionTicket = httpContext.Request.Headers[SessionTicketHeaderName].FirstOrDefault();
    LauncherBootstrapSnapshot? snapshot = await launcherBootstrapService.BuildAsync(
        string.Empty,
        sessionTicket,
        httpContext.Request.Headers.UserAgent.FirstOrDefault(),
        TryParseDeviceKind(deviceKind),
        cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.Run();

static string ResolveInformationalVersion(Assembly assembly)
{
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}

static LauncherDeviceKind? TryParseDeviceKind(string? rawValue)
{
    if (string.IsNullOrWhiteSpace(rawValue))
    {
        return null;
    }

    return Enum.TryParse<LauncherDeviceKind>(rawValue, true, out LauncherDeviceKind deviceKind)
        ? deviceKind
        : null;
}

static void AddRootConfiguration(ConfigurationManager configuration, string environmentName, string applicationKey)
{
    string? repositoryRoot = TryFindRepositoryRoot(AppContext.BaseDirectory)
        ?? TryFindRepositoryRoot(Directory.GetCurrentDirectory());

    if (string.IsNullOrWhiteSpace(repositoryRoot))
    {
        return;
    }

    string configurationDirectory = Path.Combine(repositoryRoot, "configuration");
    if (!Directory.Exists(configurationDirectory))
    {
        return;
    }

    configuration
        .AddJsonFile(Path.Combine(configurationDirectory, "shared.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"shared.{environmentName}.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"{applicationKey}.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"{applicationKey}.{environmentName}.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, "features.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"features.{environmentName}.json"), optional: true, reloadOnChange: true);
}

static string? TryFindRepositoryRoot(string startPath)
{
    if (string.IsNullOrWhiteSpace(startPath))
    {
        return null;
    }

    DirectoryInfo? directory = new(startPath);
    if (!directory.Exists)
    {
        directory = directory.Parent;
    }

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "EpsilonEmulator.sln")) ||
            Directory.Exists(Path.Combine(directory.FullName, "configuration")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return null;
}
