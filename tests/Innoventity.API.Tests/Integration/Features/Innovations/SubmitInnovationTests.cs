using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Tests.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Innoventity.API.Tests.Integration.Features.Innovations;

/// <summary>
/// Integration tests for PATCH /innovations/{id}/submit endpoint (T007)
/// Tests innovation publication with 13-rule completeness validation
/// </summary>
public class SubmitInnovationTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _ideaGeneratorId = new Guid("11111111-1111-1111-1111-111111111111");
    private readonly Guid _otherActorId = new Guid("22222222-2222-2222-2222-222222222222");
    private readonly Guid _completeInnovationId = new Guid("33333333-3333-3333-3333-333333333333");
    private readonly Guid _incompleteInnovationId = new Guid("44444444-4444-4444-4444-444444444444");
    private readonly Guid _publishedInnovationId = new Guid("55555555-5555-5555-5555-555555555555");

    public SubmitInnovationTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Generate unique database name once for this factory instance
        var databaseName = $"TestDb_SubmitInnovation_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add JWT configuration matching appsettings.Development.json
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

                    // Add in-memory database with unique name for isolation
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
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Infrastructure.Authentication.PasswordHasher>();

        // Seed Idea Generator actor
        var ideaGenerator = new Actor(_ideaGeneratorId)
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "test-generator@innoventity.dev",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("Test123!@#"),
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
        context.Actors.Add(ideaGenerator);

        // Seed another actor (for non-owner test)
        var otherActor = new Actor(_otherActorId)
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "test-other@innoventity.dev",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("Test123!@#"),
            PasswordSalt = "somesalt",
            ActivationToken = null,
            ContactAddress = new Address
            {
                Address1 = "456 Other St",
                City = "Other City",
                PostCode = "67890",
                CountryCode = "US"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Actors.Add(otherActor);

        // Seed industries
        var electronicsIndustry = new Industry("ELEC-001") { Name = "Electronics" };
        var energyIndustry = new Industry("ENRG-001") { Name = "Renewable Energy" };
        context.Industries.AddRange(electronicsIndustry, energyIndustry);

        // Seed complete innovation (meets all 13 validation rules)
        var completeInnovation = new Innovation(_completeInnovationId)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            Title = "Quantum Battery Prototype",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Lithium-air battery leveraging quantum tunneling for 10x energy density improvement through advanced material science.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Next-generation battery technology for electric vehicles enabling 1000-mile range.",
            TechnologyDescription = "Quantum tunneling mechanism enables unprecedented energy density through advanced material science",
            ProductAdvantages = "10x energy density, 50% faster charging time, 20-year operational lifespan",
            DevelopmentPhase = "Prototype",
            DevelopmentProcess = "Laboratory validation complete, seeking partners for commercial scale production",
            TargetMarket = "Electric vehicle manufacturers, renewable energy storage systems",
            TargetCustomerBase = "Automotive OEMs, grid-scale energy storage providers",
            TargetBeneficiaries = "Electric vehicle manufacturers, renewable energy providers",
            TargetCustomerType = "B2B",
            ProductKeywords = "battery, energy storage, electric vehicle, quantum",
            AdvantageKeywords = "energy density, fast charging, long lifespan",
            RelevantMarketSize = 50000000000m,
            PotentialMarketSize = 150000000000m,
            PartnersNeeded = "RD,Manufacturing,SalesMarketing",
            Status = InnovationStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            SubmittedAt = null,
            TargetIndustries = new List<Industry> { electronicsIndustry, energyIndustry }
        };
        context.Innovations.Add(completeInnovation);

        // Seed incomplete innovation (missing several required fields)
        var incompleteInnovation = new Innovation(_incompleteInnovationId)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            Title = "Incomplete Innovation",
            ProductType = "Test Product",
            ResearchBackground = "Short", // < 50 characters
            ResearchCategory = ResearchCategory.Management,
            IprStatus = "None",
            ProductDescription = string.Empty, // Missing
            TechnologyDescription = string.Empty, // Missing
            ProductAdvantages = "Some advantages",
            DevelopmentPhase = "Early",
            DevelopmentProcess = "In progress",
            TargetMarket = "Unknown",
            TargetCustomerBase = "TBD",
            TargetBeneficiaries = string.Empty, // Missing
            TargetCustomerType = "B2B",
            ProductKeywords = "test",
            AdvantageKeywords = "test",
            RelevantMarketSize = null, // Missing
            PotentialMarketSize = null, // Missing
            PartnersNeeded = null, // Missing
            Status = InnovationStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            SubmittedAt = null
            // No target industries - will fail validation
        };
        context.Innovations.Add(incompleteInnovation);

        // Seed already published innovation
        var publishedInnovation = new Innovation(_publishedInnovationId)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            Title = "Already Published Innovation",
            ProductType = "Published Product",
            ResearchBackground = "This innovation has already been published and cannot be submitted again for publication in the system.",
            ResearchCategory = ResearchCategory.NaturalScience,
            IprStatus = "Patent Granted",
            ProductDescription = "A product that is already published",
            TechnologyDescription = "Technology description for published product",
            ProductAdvantages = "Various advantages",
            DevelopmentPhase = "Commercial",
            DevelopmentProcess = "Production ready",
            TargetMarket = "Global market",
            TargetCustomerBase = "Enterprise customers",
            TargetBeneficiaries = "Industry partners",
            TargetCustomerType = "B2B",
            ProductKeywords = "published",
            AdvantageKeywords = "commercial",
            RelevantMarketSize = 1000000000m,
            PotentialMarketSize = 5000000000m,
            PartnersNeeded = "SalesMarketing",
            Status = InnovationStatus.Published, // Already published
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5),
            TargetIndustries = new List<Industry> { electronicsIndustry }
        };
        context.Innovations.Add(publishedInnovation);

        context.SaveChanges();
    }

    private async Task<string> GetAccessToken(string email, string actorType, string password = "Test123!@#")
    {
        var loginRequest = new
        {
            email,
            actorType,
            password
        };

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with status {response.StatusCode}: {errorContent}");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        return loginResponse.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token not found in login response");
    }

    [Fact]
    public async Task SubmitInnovation_Complete_Returns200AndPublishes()
    {
        // Arrange
        var token = await GetAccessToken("test-generator@innoventity.dev", "IdeaGenerator");

        // Act
        var response = await _client.PatchAsync(
            $"/innovations/{_completeInnovationId}/submit",
            null,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_completeInnovationId.ToString(), content.GetProperty("innovationId").GetString());
        Assert.Equal("Published", content.GetProperty("status").GetString());
        Assert.True(content.TryGetProperty("submittedAt", out _));

        // Verify database update
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await context.Innovations.FindAsync(_completeInnovationId);
        Assert.NotNull(innovation);
        Assert.Equal(InnovationStatus.Published, innovation.Status);
        Assert.NotNull(innovation.SubmittedAt);
    }

    [Fact]
    public async Task SubmitInnovation_Incomplete_Returns400WithErrors()
    {
        // Arrange
        var token = await GetAccessToken("test-generator@innoventity.dev", "IdeaGenerator");

        // Act
        var response = await _client.PatchAsync(
            $"/innovations/{_incompleteInnovationId}/submit",
            null,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify validation errors mentioning missing/invalid fields
        Assert.Contains("ResearchBackground", content); // < 50 chars
        Assert.Contains("ProductDescription", content); // Empty
        Assert.Contains("TechnologyDescription", content); // Empty
        Assert.Contains("TargetBeneficiaries", content); // Empty
        Assert.Contains("RelevantMarketSize", content); // Null
        Assert.Contains("PotentialMarketSize", content); // Null
        Assert.Contains("TargetIndustries", content); // Empty collection
        Assert.Contains("PartnersNeeded", content); // Null
    }

    [Fact]
    public async Task SubmitInnovation_AlreadyPublished_Returns409Conflict()
    {
        // Arrange
        var token = await GetAccessToken("test-generator@innoventity.dev", "IdeaGenerator");

        // Act
        var response = await _client.PatchAsync(
            $"/innovations/{_publishedInnovationId}/submit",
            null,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already published", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitInnovation_AsNonOwner_Returns403Forbidden()
    {
        // Arrange - login as different actor
        var token = await GetAccessToken("test-other@innoventity.dev", "IdeaGenerator");

        // Act - try to submit someone else's innovation
        var response = await _client.PatchAsync(
            $"/innovations/{_completeInnovationId}/submit",
            null,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("owner", content, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
