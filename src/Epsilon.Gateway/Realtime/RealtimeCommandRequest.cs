using System.Text.Json;

namespace Epsilon.Gateway;

public sealed record RealtimeCommandRequest(
    string? Id,
    string? Command,
    string? Ticket,
    Dictionary<string, JsonElement>? Arguments);
