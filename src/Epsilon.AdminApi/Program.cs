var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "Epsilon.AdminApi",
    status = "ok",
    utc = DateTime.UtcNow
}));

app.Run();

