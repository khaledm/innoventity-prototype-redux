using System.Net;
using System.Net.Http.Json;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Innoventity.API.Tests.Integration.Features.Health;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        // Configure factory with in-memory database by default
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("InMemoryTestDb"));
            });
        });
    }

    [Fact]
    public async Task Health_Endpoint_Returns_200_When_All_Dependencies_Healthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Contains("database", health.Checks.Keys);
        Assert.Equal("Healthy", health.Checks["database"]);
        Assert.Contains("jwt_config", health.Checks.Keys);
        Assert.Equal("Healthy", health.Checks["jwt_config"]);
    }

    [Fact]
    public async Task Health_Endpoint_Returns_503_When_Database_Unavailable()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add DbContext with invalid connection string
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer("Server=invalid-server;Database=InvalidDb;User Id=invalid;Password=invalid;TrustServerCertificate=true;"));
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("Unhealthy", health.Status);
        Assert.Contains("database", health.Checks.Keys);
        Assert.Equal("Unhealthy", health.Checks["database"]);
    }

    [Fact]
    public async Task Health_Endpoint_Returns_503_When_JWT_Configuration_Missing()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:SigningKey", "");
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("Unhealthy", health.Status);
        Assert.Contains("jwt_config", health.Checks.Keys);
        Assert.Equal("Unhealthy", health.Checks["jwt_config"]);
    }

    private record HealthResponse(string Status, Dictionary<string, string> Checks);
}
