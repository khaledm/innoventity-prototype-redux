using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Tests.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Innoventity.API.Tests.Integration.Features.Innovations;

public class GetInnovationTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _testInnovationId = new Guid("22222222-2222-2222-2222-222222222222");
    private readonly Guid _testActorId = new Guid("11111111-1111-1111-1111-111111111111");

    public GetInnovationTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Generate unique database name once for this factory instance
        var databaseName = $"TestDb_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add JWT configuration for test environment
                    // Use values that MATCH appsettings.json to ensure token generation and validation agree
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SigningKey"] = "DEV-ONLY-KEY-REPLACE-IN-PRODUCTION-VIA-CONFIGURATION-MINIMUM-32-CHARACTERS",
                        ["Jwt:Issuer"] = "Innoventity",
                        ["Jwt:Audience"] = "Innoventity.API",
                        ["Jwt:AccessTokenExpirationMinutes"] = "60",
                        ["Jwt:RefreshTokenExpirationDays"] = "7"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database with unique name for isolation (captured in closure)
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed actor (Spec §6 Test Data Requirements)
        var testActor = new Actor(_testActorId)
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "test-generator@innoventity.dev",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = "$2a$12$XYZ...", // Placeholder hash
            PasswordSalt = "somesalt",
            ActivationToken = null,
            ContactAddress = new Address
            {
                Address1 = "123 Test St",
                City = "Test City",
                PostCode = "12345",
                CountryCode = "US"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Actors.Add(testActor);

        // Seed industries (Spec §6)
        var electronicsIndustry = new Industry("ELEC-001") { Name = "Electronics" };
        var energyIndustry = new Industry("ENRG-001") { Name = "Renewable Energy" };
        context.Set<Industry>().AddRange(electronicsIndustry, energyIndustry);

        // Seed innovation (Spec §6 Test Data Requirements)
        var testInnovation = new Innovation(_testInnovationId)
        {
            IdeaToken = new Guid("33333333-3333-3333-3333-333333333333"),
            OwnerId = _testActorId,
            IdeaSummary = new IdeaSummary
            {
                Title = "Quantum Battery Prototype",
                ProductType = "Energy Storage Device",
                ResearchBackground = "Lithium-air battery leveraging quantum tunneling for 10x energy density improvement over conventional Li-ion batteries. Based on 3 years of R&D at Advanced Energy Lab.",
                ResearchCategory = ResearchCategory.Engineering,
                IprStatus = "Patent Pending"
            },
            Product = new Product
            {
                ProductDescription = "Next-generation battery technology for electric vehicles enabling 1000-mile range on single charge with 50% faster charging and 20-year lifespan.",
                TechnologyDescription = "Quantum tunneling mechanism enables unprecedented energy density through advanced cathode materials and electrolyte chemistry",
                TargetBeneficiaries = "Electric vehicle manufacturers, renewable energy storage providers, consumer electronics companies",
                ProductAdvantages = "10x energy density, 50% faster charging time, 20-year operational lifespan, environmentally sustainable materials",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Laboratory validation complete, seeking partners for commercial scale production",
                ProductKeywords = "battery, energy storage, electric vehicle, quantum, lithium-air",
                AdvantageKeywords = "energy density, fast charging, long lifespan, sustainable"
            },
            Market = new Market
            {
                TargetMarket = "Electric vehicle manufacturers, renewable energy storage systems, consumer electronics",
                TargetCustomerBase = "Automotive OEMs, grid-scale energy storage providers",
                TargetCustomerType = "B2B"
            },
            CollaborationRequirement = new CollaborationRequirement
            {
                PartnersNeeded = "RD,Manufacturing"
            },
            Status = InnovationStatus.Published,
            CreatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            SubmittedAt = new DateTime(2026, 1, 15, 14, 30, 0, DateTimeKind.Utc),
            TargetIndustries = new List<Industry> { electronicsIndustry, energyIndustry }
        };
        context.Set<Innovation>().Add(testInnovation);

        context.SaveChanges();
    }

    private async Task<string> GetAccessToken()
    {
        // Login to get access token
        var loginRequest = new
        {
            email = "test-generator@innoventity.dev",
            actorType = "IdeaGenerator",
            password = "Test123!@#" // This won't match the placeholder hash, so we need to update seed data
        };

        // For this test, we'll create a properly hashed password
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Infrastructure.Authentication.PasswordHasher>();

        var actor = await context.Actors.FindAsync(_testActorId);
        if (actor == null)
        {
            throw new InvalidOperationException($"Actor with ID {_testActorId} not found in database");
        }

        actor.PasswordHash = passwordHasher.HashPassword("Test123!@#");
        await context.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with status {response.StatusCode}: {errorContent}");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        return loginResponse.GetProperty("accessToken").GetString() ?? throw new InvalidOperationException("Failed to get access token");
    }

    [Fact]
    public async Task GetInnovation_WithValidId_ReturnsInnovationData()
    {
        // Arrange
        var token = await GetAccessToken();

        // Act - Use per-request token attachment (T003 fix)
        var response = await _client.GetWithAuthAsync($"/innovations/{_testInnovationId}", token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var innovation = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ideaSummary = innovation.GetProperty("ideaSummary");
        Assert.Equal("Quantum Battery Prototype", ideaSummary.GetProperty("title").GetString());
        Assert.Equal("Engineering", ideaSummary.GetProperty("researchCategory").GetString());
        Assert.Equal("Published", innovation.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetInnovation_WithNonExistentId_Returns404()
    {
        // Arrange
        var token = await GetAccessToken();
        var nonExistentId = Guid.NewGuid();

        // Act - Use per-request token attachment (T003 fix)
        var response = await _client.GetWithAuthAsync($"/innovations/{nonExistentId}", token);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInnovation_WithoutAuthToken_Returns401()
    {
        // Arrange - No authorization header set

        // Act
        var response = await _client.GetAsync($"/innovations/{_testInnovationId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInnovation_CrossActorAccess_Returns200()
    {
        // Arrange - Create Manufacturing actor
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Infrastructure.Authentication.PasswordHasher>();

        var manufacturingActor = new Actor(Guid.NewGuid())
        {
            FirstName = "Manufacturing",
            LastName = "Co",
            Email = "test-manufacturing@innoventity.dev",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("Test123!@#"),
            PasswordSalt = "somesalt",
            ActivationToken = null,
            ContactAddress = new Address
            {
                Address1 = "456 Factory Lane",
                City = "Factory City",
                PostCode = "67890",
                CountryCode = "US"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Actors.Add(manufacturingActor);
        await context.SaveChangesAsync();

        // Login as Manufacturing actor
        var loginRequest = new
        {
            email = "test-manufacturing@innoventity.dev",
            actorType = "Manufacturing",
            password = "Test123!@#"
        };
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginData.GetProperty("accessToken").GetString() ?? throw new InvalidOperationException("Failed to get token");

        // Act - Manufacturing actor views IdeaGenerator's innovation (use per-request token attachment - T003 fix)
        var response = await _client.GetWithAuthAsync($"/innovations/{_testInnovationId}", token);

        // Assert - Phase 0 open-discovery: any authenticated user can view
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
