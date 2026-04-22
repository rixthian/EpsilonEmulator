namespace Epsilon.Protocol;

public sealed class ProtocolSelfCheckService
{
    private readonly PacketRegistry _packetRegistry;
    private readonly ProtocolCommandRegistry _commandRegistry;

    public ProtocolSelfCheckService(
        PacketRegistry packetRegistry,
        ProtocolCommandRegistry commandRegistry)
    {
        _packetRegistry = packetRegistry;
        _commandRegistry = commandRegistry;
    }

    public ProtocolSelfCheckReport Run()
    {
        List<string> issues = [];

        if (!_packetRegistry.Family.Equals(_commandRegistry.Family, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("Packet manifest family does not match command manifest family.");
        }

        foreach ((string commandName, ProtocolCommandDefinition command) in _commandRegistry.Commands)
        {
            if (!_packetRegistry.ContainsIncoming(command.PacketName))
            {
                issues.Add($"Command '{commandName}' references missing incoming packet '{command.PacketName}'.");
            }
        }

        foreach ((string commandName, ProtocolCommandDefinition command) in _commandRegistry.Commands)
        {
            if (!command.Required)
            {
                continue;
            }

            if (!_packetRegistry.TryGetIncomingId(command.PacketName, out _))
            {
                issues.Add($"Required command '{commandName}' is not backed by a registered incoming packet.");
            }
        }

        return new ProtocolSelfCheckReport(
            issues.Count == 0,
            _packetRegistry.Family,
            _commandRegistry.Family,
            _commandRegistry.Revision,
            _packetRegistry.Incoming.Count,
            _packetRegistry.Outgoing.Count,
            _commandRegistry.Commands.Count,
            issues);
    }
}
