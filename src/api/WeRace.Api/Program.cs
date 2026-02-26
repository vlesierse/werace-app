var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery
builder.AddServiceDefaults();

// Aspire components: PostgreSQL and Redis
builder.AddNpgsqlDataSource("werace");
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
