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
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Innoventity.API.Tests.Integration.Features.Innovations;

/// <summary>
/// Integration tests for PUT /innovations/{id} endpoint (T006)
/// Tests innovation draft update with ownership and status validation
/// </summary>
public class UpdateInnovationTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _ideaGeneratorId = new Guid("11111111-1111-1111-1111-111111111111");
    private readonly Guid _otherIdeaGeneratorId = new Guid("33333333-3333-3333-3333-333333333333");
    private readonly Guid _draftInnovationId = new Guid("44444444-4444-4444-4444-444444444444");
    private readonly Guid _publishedInnovationId = new Guid("55555555-5555-5555-5555-555555555555");

    public UpdateInnovationTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Generate unique database name once for this factory instance (T002 fix)
        var databaseName = $"TestDb_UpdateInnovation_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add JWT configuration matching appsettings.Development.json (T003 fix)
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

                    // Add in-memory database with unique name for isolation (captured in closure - T002)
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

        // Seed first Idea Generator actor (owner)
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

        // Seed second Idea Generator actor (non-owner)
        var otherIdeaGenerator = new Actor(_otherIdeaGeneratorId)
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "other-generator@innoventity.dev",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("Test123!@#"),
            PasswordSalt = "somesalt",
            ActivationToken = null,
            ContactAddress = new Address
            {
                Address1 = "456 Other St",
                City = "Other City",
                PostCode = "54321",
                CountryCode = "US"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Actors.Add(otherIdeaGenerator);

        // Seed draft innovation (owned by first actor)
        var draftInnovation = new Innovation(_draftInnovationId)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            Title = "Original Draft Title",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Original research background",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Original product description",
            ProductAdvantages = "Energy efficient",
            DevelopmentPhase = "Prototype",
            DevelopmentProcess = "In-house R&D",
            TargetMarket = "EV manufacturers",
            TargetCustomerBase = "Corporate",
            TargetCustomerType = "B2B",
            ProductKeywords = "battery, energy",
            AdvantageKeywords = "efficient, sustainable",
            Status = InnovationStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            SubmittedAt = null
        };
        context.Innovations.Add(draftInnovation);

        // Seed published innovation (owned by first actor)
        var publishedInnovation = new Innovation(_publishedInnovationId)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            Title = "Published Innovation",
            ProductType = "Medical Device",
            ResearchBackground = "Published research background",
            ResearchCategory = ResearchCategory.NaturalScience,
            IprStatus = "Patent Pending",
            ProductDescription = "Published product description",
            ProductAdvantages = "Health improving",
            DevelopmentPhase = "Clinical trials",
            DevelopmentProcess = "FDA approved process",
            TargetMarket = "Healthcare providers",
            TargetCustomerBase = "Hospitals",
            TargetCustomerType = "B2B",
            ProductKeywords = "medical, healthcare",
            AdvantageKeywords = "safe, effective",
            Status = InnovationStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
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
            ?? throw new InvalidOperationException("Failed to get access token");
    }

    /// <summary>
    /// Test 1: Update draft innovation as owner - returns 200 OK with updated data
    /// Spec §US2 Acceptance Scenario 2: "When PUT /innovations/{id} with updated title, Then system returns 200 OK with updated innovation"
    /// </summary>
    [Fact]
    public async Task UpdateInnovation_AsOwner_Returns200OK()
    {
        // Arrange
        var token = await GetAccessToken("test-generator@innoventity.dev", "IdeaGenerator");
        var updateRequest = new
        {
            title = "Updated Draft Title",
            productDescription = "Updated product description with more details",
            researchBackground = "Updated research background with new findings"
        };

        // Act
        var response = await _client.PutWithAuthAsync(
            $"/innovations/{_draftInnovationId}",
            JsonContent.Create(updateRequest),
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.Equal(_draftInnovationId.ToString(), root.GetProperty("innovationId").GetString());
        Assert.Equal("Updated Draft Title", root.GetProperty("title").GetString());
        Assert.Equal("Draft", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("modifiedAt", out _), "Response should include modifiedAt timestamp");

        // Verify database was updated
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedInnovation = await context.Innovations.FindAsync(_draftInnovationId);
        Assert.NotNull(updatedInnovation);
        Assert.Equal("Updated Draft Title", updatedInnovation.Title);
        Assert.Equal("Updated product description with more details", updatedInnovation.ProductDescription);
    }

    /// <summary>
    /// Test 2: Update innovation as non-owner - returns 403 Forbidden
    /// Spec §US2 Acceptance Scenario 3: "Given innovation draft owned by different actor, When PUT /innovations/{id} attempted, Then system returns 403 Forbidden"
    /// </summary>
    [Fact]
    public async Task UpdateInnovation_AsNonOwner_Returns403Forbidden()
    {
        // Arrange - use different actor (not owner)
        var token = await GetAccessToken("other-generator@innoventity.dev", "IdeaGenerator");
        var updateRequest = new
        {
            title = "Attempted unauthorized update"
        };

        // Act
        var response = await _client.PutWithAuthAsync(
            $"/innovations/{_draftInnovationId}",
            JsonContent.Create(updateRequest),
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Only the innovation owner can update this innovation", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 3: Update published innovation - returns 409 Conflict
    /// T006 Requirement: "Published innovation cannot be edited (409 Conflict)"
    /// </summary>
    [Fact]
    public async Task UpdateInnovation_PublishedInnovation_Returns409Conflict()
    {
        // Arrange - use published innovation
        var token = await GetAccessToken("test-generator@innoventity.dev", "IdeaGenerator");
        var updateRequest = new
        {
            title = "Attempted update on published innovation"
        };

        // Act
        var response = await _client.PutWithAuthAsync(
            $"/innovations/{_publishedInnovationId}",
            JsonContent.Create(updateRequest),
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("published", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 4: Update non-existent innovation - returns 404 Not Found
    /// T006 Requirement: "Non-existent innovation returns 404 Not Found"
    /// </summary>
    [Fact]
    public async Task UpdateInnovation_NotFound_Returns404()
    {
        // Arrange - use non-existent innovation ID
        var nonExistentId = Guid.NewGuid();
        var token = await GetAccessToken("test-generator@innoventity.dev", "IdeaGenerator");
        var updateRequest = new
        {
            title = "Update for non-existent innovation"
        };

        // Act
        var response = await _client.PutWithAuthAsync(
            $"/innovations/{nonExistentId}",
            JsonContent.Create(updateRequest),
            token);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
