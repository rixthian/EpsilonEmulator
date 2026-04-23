using Epsilon.Auth;

namespace Epsilon.Gateway;

public sealed record ProtocolCommandExecutionResult(
    int StatusCode,
    object Payload,
    SessionTicket? BoundSession);
