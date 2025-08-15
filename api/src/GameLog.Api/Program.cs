using GameLog.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddGameLogSwagger();

var app = builder.Build();

app.UseGameLogSwagger();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
