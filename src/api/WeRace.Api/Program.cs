using Microsoft.EntityFrameworkCore;
using WeRace.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery
builder.AddServiceDefaults();

// Aspire components: PostgreSQL (EF Core) and Redis
builder.AddNpgsqlDbContext<WeRaceDbContext>("werace", configureDbContextOptions: options =>
{
    options.UseSnakeCaseNamingConvention();
});
builder.AddRedisClient("redis");

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Aspire default endpoints: /health, /alive
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

// Make Program accessible to WebApplicationFactory in integration tests
public partial class Program { }
