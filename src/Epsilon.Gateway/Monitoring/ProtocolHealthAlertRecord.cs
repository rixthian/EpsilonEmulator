namespace Epsilon.Gateway;

public sealed record ProtocolHealthAlertRecord(
    DateTime RecordedAtUtc,
    ProtocolHealthState State,
    string Message);
