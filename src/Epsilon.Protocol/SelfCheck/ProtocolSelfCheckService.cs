namespace Epsilon.Protocol;

/// <summary>
/// Validates consistency between packet and command manifests.
/// </summary>
public sealed class ProtocolSelfCheckService
{
    private readonly PacketRegistry _packetRegistry;
    private readonly ProtocolCommandRegistry _commandRegistry;

    /// <summary>
    /// Creates a protocol self-check service.
    /// </summary>
    public ProtocolSelfCheckService(
        PacketRegistry packetRegistry,
        ProtocolCommandRegistry commandRegistry)
    {
        _packetRegistry = packetRegistry;
        _commandRegistry = commandRegistry;
    }

    /// <summary>
    /// Runs protocol consistency validation.
    /// </summary>
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

        return new ProtocolSelfCheckReport
        {
            IsHealthy = issues.Count == 0,
            PacketFamily = _packetRegistry.Family,
            CommandFamily = _commandRegistry.Family,
            CommandRevision = _commandRegistry.Revision,
            IncomingPacketCount = _packetRegistry.Incoming.Count,
            OutgoingPacketCount = _packetRegistry.Outgoing.Count,
            CommandCount = _commandRegistry.Commands.Count,
            Issues = issues
        };
    }
}
