using GameLog.Api.Extensions;
using GameLog.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddGameLogLogging();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddGameLogSwagger();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseGameLogSwagger();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
