using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
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
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add JWT configuration for test environment
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SigningKey"] = "test-signing-key-minimum-32-characters-required-for-hs256",
                        ["Jwt:Issuer"] = "test-issuer",
                        ["Jwt:Audience"] = "test-audience",
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

                    // Add in-memory database with unique name for isolation
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed actor (Spec §6 Test Data Requirements)
        var testActor = new Actor
        {
            Id = _testActorId,
            FullName = "Dr. Sarah Chen",
            Email = "test-generator@innoventity.dev",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = "$2a$12$XYZ...", // Placeholder hash
            ActivationToken = null,
            ContactAddress = "123 Test St",
            CreatedAt = DateTime.UtcNow
        };
        context.Actors.Add(testActor);

        // Seed industries (Spec §6)
        var electronicsIndustry = new Industry { IndustryId = "ELEC-001", Name = "Electronics" };
        var energyIndustry = new Industry { IndustryId = "ENRG-001", Name = "Renewable Energy" };
        context.Set<Industry>().AddRange(electronicsIndustry, energyIndustry);

        // Seed innovation (Spec §6 Test Data Requirements)
        var testInnovation = new Innovation
        {
            Id = _testInnovationId,
            IdeaToken = new Guid("33333333-3333-3333-3333-333333333333"),
            OwnerId = _testActorId,
            Title = "Quantum Battery Prototype",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Lithium-air battery leveraging quantum tunneling for 10x energy density improvement over conventional Li-ion batteries. Based on 3 years of R&D at Advanced Energy Lab.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Next-generation battery technology for electric vehicles enabling 1000-mile range on single charge with 50% faster charging and 20-year lifespan.",
            ProductAdvantages = "10x energy density, 50% faster charging time, 20-year operational lifespan, environmentally sustainable materials",
            DevelopmentPhase = "Prototype",
            DevelopmentProcess = "Laboratory validation complete, seeking partners for commercial scale production",
            TargetMarket = "Electric vehicle manufacturers, renewable energy storage systems, consumer electronics",
            TargetCustomerBase = "Automotive OEMs, grid-scale energy storage providers",
            TargetCustomerType = "B2B",
            ProductKeywords = "battery, energy storage, electric vehicle, quantum, lithium-air",
            AdvantageKeywords = "energy density, fast charging, long lifespan, sustainable",
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
            Email = "test-generator@innoventity.dev",
            ActorType = "IdeaGenerator",
            Password = "Test123!@#" // This won't match the placeholder hash, so we need to update seed data
        };

        // For this test, we'll create a properly hashed password
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Infrastructure.Authentication.PasswordHasher>();

        var actor = await context.Actors.FindAsync(_testActorId);
        if (actor != null)
        {
            actor.PasswordHash = passwordHasher.HashPassword("Test123!@#");
            await context.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginResponse = await response.Content.ReadFromJsonAsync<dynamic>();
        return loginResponse?.accessToken?.ToString() ?? throw new InvalidOperationException("Failed to get access token");
    }

    [Fact]
    public async Task GetInnovation_WithValidId_ReturnsInnovationData()
    {
        // Arrange
        var token = await GetAccessToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/innovations/{_testInnovationId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var innovation = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(innovation);
        Assert.Equal("Quantum Battery Prototype", innovation?.title?.ToString());
        Assert.Equal("Engineering", innovation?.researchCategory?.ToString());
        Assert.Equal("Published", innovation?.status?.ToString());
    }

    [Fact]
    public async Task GetInnovation_WithNonExistentId_Returns404()
    {
        // Arrange
        var token = await GetAccessToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/innovations/{nonExistentId}");

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

        var manufacturingActor = new Actor
        {
            Id = Guid.NewGuid(),
            FullName = "Manufacturing Co",
            Email = "test-manufacturing@innoventity.dev",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("Test123!@#"),
            ActivationToken = null,
            ContactAddress = "456 Factory Lane",
            CreatedAt = DateTime.UtcNow
        };
        context.Actors.Add(manufacturingActor);
        await context.SaveChangesAsync();

        // Login as Manufacturing actor
        var loginRequest = new
        {
            Email = "test-manufacturing@innoventity.dev",
            ActorType = "Manufacturing",
            Password = "Test123!@#"
        };
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<dynamic>();
        var token = loginData?.accessToken?.ToString() ?? throw new InvalidOperationException("Failed to get token");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Manufacturing actor views IdeaGenerator's innovation
        var response = await _client.GetAsync($"/innovations/{_testInnovationId}");

        // Assert - Phase 0 open-discovery: any authenticated user can view
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
