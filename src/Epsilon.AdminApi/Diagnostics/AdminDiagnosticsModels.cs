using Epsilon.Persistence;
using Epsilon.Gateway;

namespace Epsilon.AdminApi;

public sealed record AdminServiceStatus(
    string Service,
    DateTime Utc);

public sealed record AdminOverallStatus(
    bool Healthy,
    string Status);

public sealed record AdminDiagnosticsSummary(
    AdminServiceStatus Admin,
    PersistenceReadinessReport Persistence,
    GatewayDiagnosticsSummary? Gateway,
    string? GatewayError,
    object? Launcher,
    string? LauncherError,
    AdminOverallStatus Overall);
