var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
