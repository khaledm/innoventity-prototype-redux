using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Innoventity.API.Tests.Integration.Features.Authentication;

/// <summary>
/// Integration tests for POST /auth/activate endpoint (User Story 1)
/// Tests R1.1 (Account Activation)
/// </summary>
public class ActivateAccountTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factoryBase;

    public ActivateAccountTests(WebApplicationFactory<Program> factory)
    {
        _factoryBase = factory;
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Create a new factory with unique in-memory database for each test
        return _factoryBase.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database with unique name for test isolation
                var dbName = $"ActivateTests_{Guid.NewGuid()}";
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });
            });
        });
    }

    [Fact]
    public async Task Activate_ChangesStatusToActive_R1_1()
    {
        // Arrange
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // First register a user
        var registerRequest = new
        {
            email = "activate@example.com",
            firstName = "Test",
            lastName = "User",
            contactAddress = new
            {
                address1 = "123 Test St",
                city = "London",
                postCode = "SW1A 1AA",
                countryCode = "GB"
            },
            actorType = "IdeaGenerator",
            password = "SecureP@ss123!"
        };

        var registerResponse = await client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var activationToken = registerResult.GetProperty("activationToken").GetString()!;

        // Act - activate account
        var activateRequest = new
        {
            email = "activate@example.com",
            token = activationToken
        };

        var activateResponse = await client.PostAsJsonAsync("/auth/activate", activateRequest);

        // Assert - R1.1: Account activated successfully
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        var activateResult = await activateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active", activateResult.GetProperty("accountStatus").GetString());
    }

    [Fact]
    public async Task Activate_RejectsInvalidToken_R1_1()
    {
        // Arrange
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // Register a user
        var registerRequest = new
        {
            email = "invalidtoken@example.com",
            firstName = "Test",
            lastName = "User",
            contactAddress = new
            {
                address1 = "123 Test St",
                city = "Manchester",
                postCode = "M1 1AA",
                countryCode = "GB"
            },
            actorType = "RD",
            password = "SecureP@ss123!"
        };

        var registerResponse = await client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Act - attempt activation with invalid token
        var activateRequest = new
        {
            email = "invalidtoken@example.com",
            token = "invalid-activation-token-12345"
        };

        var activateResponse = await client.PostAsJsonAsync("/auth/activate", activateRequest);

        // Assert - Invalid token rejected
        Assert.Equal(HttpStatusCode.BadRequest, activateResponse.StatusCode);

        var error = await activateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(error.TryGetProperty("detail", out var detail));
        Assert.Contains("invalid", detail.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Activate_RejectsAlreadyActivatedAccount()
    {
        // Arrange
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // Register and activate a user
        var registerRequest = new
        {
            email = "alreadyactive@example.com",
            firstName = "Test",
            lastName = "User",
            contactAddress = new
            {
                address1 = "123 Test St",
                city = "Birmingham",
                postCode = "B1 1AA",
                countryCode = "GB"
            },
            actorType = "Manufacturing",
            password = "SecureP@ss123!"
        };

        var registerResponse = await client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var activationToken = registerResult.GetProperty("activationToken").GetString()!;

        // First activation (should succeed)
        var activateRequest = new
        {
            email = "alreadyactive@example.com",
            token = activationToken
        };

        var firstActivation = await client.PostAsJsonAsync("/auth/activate", activateRequest);
        Assert.Equal(HttpStatusCode.OK, firstActivation.StatusCode);

        // Act - attempt second activation
        var secondActivation = await client.PostAsJsonAsync("/auth/activate", activateRequest);

        // Assert - Second activation rejected (account already active)
        Assert.Equal(HttpStatusCode.BadRequest, secondActivation.StatusCode);
    }
}
