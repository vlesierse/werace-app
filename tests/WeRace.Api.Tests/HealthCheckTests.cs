using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace WeRace.Api.Tests;

/// <summary>
/// Integration tests for the health check endpoints.
/// Uses WebApplicationFactory to spin up the API in-memory.
/// </summary>
public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            // Provide placeholder connection strings so Aspire components can register
            builder.UseSetting("ConnectionStrings:werace", "Host=localhost;Database=werace");
            builder.UseSetting("ConnectionStrings:redis", "localhost");
            builder.ConfigureTestServices(services =>
            {
                // Remove external dependency health checks (PostgreSQL, Redis) so
                // the health endpoints return healthy without real services in tests
                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    var checksToRemove = options.Registrations
                        .Where(r => !r.Tags.Contains("live"))
                        .ToList();
                    foreach (var reg in checksToRemove)
                    {
                        options.Registrations.Remove(reg);
                    }
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task AliveCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Healthy");
    }
}
