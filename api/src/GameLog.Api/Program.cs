using System.Text.Json.Serialization;
using GameLog.Api.Extensions;
using GameLog.Api.Middleware;
using GameLog.Api.Services;
using GameLog.Application;
using GameLog.Application.Abstractions;
using GameLog.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddGameLogLogging();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddGameLogAuthentication();
builder.Services.AddGameLogAntiforgery();
builder.Services.AddGameLogCors(builder.Configuration);
builder.Services.AddGameLogRateLimiting(builder.Configuration);

builder.Services
    .AddControllers(options => options.AddAntiforgeryValidation())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddGameLogSwagger();

// The API is only reachable through the web app's proxy, so X-Forwarded-* is trusted
// (needed for per-client rate limiting). Do not expose the API port publicly without a trusted proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseGameLogSwagger();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.MigrateDatabaseAsync();
await app.SeedDatabaseAsync();
app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
