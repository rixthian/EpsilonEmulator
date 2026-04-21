using Epsilon.Protocol;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProtocolServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/health", (PacketRegistry registry) => Results.Ok(new
{
    service = "Epsilon.Gateway",
    status = "ok",
    compatibility = registry.Family,
    incomingPacketCount = registry.Incoming.Count,
    outgoingPacketCount = registry.Outgoing.Count,
    utc = DateTime.UtcNow
}));

app.Run();

