using System.Net;

// TODO: Uncomment WebApplicationFactory usage once WeRace.Api project is created by Gilfoyle.
// using Microsoft.AspNetCore.Mvc.Testing;

namespace WeRace.Api.Tests;

/// <summary>
/// Integration tests for the /health endpoint.
/// Uses WebApplicationFactory to spin up the API in-memory.
/// </summary>
public class HealthCheckTests // : IClassFixture<WebApplicationFactory<Program>>
{
    // private readonly HttpClient _client;

    // public HealthCheckTests(WebApplicationFactory<Program> factory)
    // {
    //     _client = factory.CreateClient();
    // }

    [Fact]
    public void Placeholder_TestInfrastructureWorks()
    {
        // Verifies xUnit + FluentAssertions are wired correctly.
        // Replace with real health check test once the API project exists.
        true.Should().BeTrue();
    }

    // [Fact]
    // public async Task HealthCheck_ReturnsOk()
    // {
    //     // Arrange & Act
    //     var response = await _client.GetAsync("/health");
    //
    //     // Assert
    //     response.StatusCode.Should().Be(HttpStatusCode.OK);
    // }

    // [Fact]
    // public async Task HealthCheck_ReturnsHealthyStatus()
    // {
    //     // Arrange & Act
    //     var response = await _client.GetAsync("/health");
    //     var content = await response.Content.ReadAsStringAsync();
    //
    //     // Assert
    //     response.StatusCode.Should().Be(HttpStatusCode.OK);
    //     content.Should().Contain("Healthy");
    // }
}
