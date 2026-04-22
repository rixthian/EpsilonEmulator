using System.Text.Json;

namespace Epsilon.Gateway;

public sealed record ProtocolExecuteInput(
    Dictionary<string, JsonElement>? Arguments);
