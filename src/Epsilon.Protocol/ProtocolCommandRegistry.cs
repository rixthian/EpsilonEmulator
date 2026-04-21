namespace Epsilon.Protocol;

public sealed class ProtocolCommandRegistry
{
    private readonly Dictionary<string, ProtocolCommandDefinition> _commands;
    private readonly string _family;
    private readonly string _revision;

    public ProtocolCommandRegistry(ProtocolCommandManifestLoader loader)
    {
        ProtocolCommandManifest manifest = loader.Load();

        _family = RequireValue(manifest.Family, "Protocol command family is required.");
        _revision = RequireValue(manifest.Revision, "Protocol command revision is required.");

        _commands = new Dictionary<string, ProtocolCommandDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (ProtocolCommandDefinition command in manifest.Commands)
        {
            string commandName = RequireValue(command.Name, "Protocol command name is required.");
            RequireValue(command.PacketName, $"Protocol command '{commandName}' requires a packet name.");
            RequireValue(command.Flow, $"Protocol command '{commandName}' requires a flow name.");

            if (!_commands.TryAdd(commandName, command))
            {
                throw new InvalidOperationException($"Duplicate protocol command name '{commandName}' was found.");
            }
        }
    }

    public string Family => _family;
    public string Revision => _revision;
    public IReadOnlyDictionary<string, ProtocolCommandDefinition> Commands => _commands;

    public bool TryGet(string commandName, out ProtocolCommandDefinition? command)
    {
        bool found = _commands.TryGetValue(commandName, out ProtocolCommandDefinition? resolvedCommand);
        command = resolvedCommand;
        return found;
    }

    private static string RequireValue(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value.Trim();
    }
}
