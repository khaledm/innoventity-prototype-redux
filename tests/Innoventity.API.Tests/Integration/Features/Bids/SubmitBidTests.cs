using System.Net;
using System.Net.Http.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Features.Bids;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Tests.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Innoventity.API.Tests.Integration.Features.Bids;

/// <summary>
/// Integration tests for T010: POST /innovations/{innovationId}/bids (Submit Bid)
/// Tests business rules R4.1 (eligibility) and R4.2 (proposal requirements)
/// </summary>
public class SubmitBidTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    // Test data IDs
    private readonly Guid _publishedInnovationId = new Guid("11111111-1111-1111-1111-111111111111");
    private readonly Guid _draftInnovationId = new Guid("22222222-2222-2222-2222-222222222222");
    private readonly Guid _ideaGeneratorId = new Guid("33333333-3333-3333-3333-333333333333");
    private readonly Guid _manufacturingActorId = new Guid("44444444-4444-4444-4444-444444444444");
    private readonly Guid _rdActorId = new Guid("55555555-5555-5555-5555-555555555555");
    private readonly Guid _existingBidActorId = new Guid("66666666-6666-6666-6666-666666666666");

    public SubmitBidTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Generate unique database name once for this factory instance
        var databaseName = $"TestDb_SubmitBid_{Guid.NewGuid()}";

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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed test data
        var ideaGenerator = new Actor(_ideaGeneratorId)
        {
            Email = "innovator@test.com",
            FirstName = "Sarah",
            LastName = "Chen",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var manufacturingActor = new Actor(_manufacturingActorId)
        {
            Email = "manufacturer@test.com",
            FirstName = "Hans",
            LastName = "Mueller",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var rdActor = new Actor(_rdActorId)
        {
            Email = "researcher@test.com",
            FirstName = "Emily",
            LastName = "Watson",
            ActorType = ActorType.RD,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var existingBidActor = new Actor(_existingBidActorId)
        {
            Email = "investor@test.com",
            FirstName = "John",
            LastName = "Smith",
            ActorType = ActorType.Investor,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        db.Actors.AddRange(ideaGenerator, manufacturingActor, rdActor, existingBidActor);

        // Create published innovation (owned by idea generator)
        var publishedInnovation = new Innovation(_publishedInnovationId)
        {
            OwnerId = _ideaGeneratorId,
            Title = "Quantum Battery Prototype",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Advanced lithium-air battery technology with quantum-enhanced energy density for next-generation electric vehicles and renewable energy storage systems.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Revolutionary battery technology achieving 10x energy density compared to conventional lithium-ion batteries.",
            TechnologyDescription = "Quantum-enhanced cathode materials enabling unprecedented energy storage capacity.",
            TargetBeneficiaries = "Electric vehicle manufacturers, renewable energy providers, grid storage operators.",
            ProductAdvantages = "Longer range for EVs, faster charging times, reduced environmental impact.",
            DevelopmentPhase = "Prototype Testing",
            DevelopmentProcess = "Currently in lab validation phase with promising initial results.",
            TargetMarket = "Global electric vehicle and renewable energy markets.",
            TargetCustomerBase = "Automotive OEMs and energy storage companies.",
            TargetCustomerType = "B2B",
            ProductKeywords = "battery, quantum, energy, storage, EV",
            AdvantageKeywords = "high-density, fast-charging, sustainable",
            RelevantMarketSize = 50000000000m,
            PotentialMarketSize = 150000000000m,
            PartnersNeeded = "RD,Manufacturing,SalesMarketing",
            Status = InnovationStatus.Published,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        // Create draft innovation (not accepting bids)
        var draftInnovation = new Innovation(_draftInnovationId)
        {
            OwnerId = _ideaGeneratorId,
            Title = "Draft Innovation",
            ProductType = "Test Product",
            ResearchBackground = "This is a draft innovation that should not accept bids yet.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "None",
            ProductDescription = "Draft product description",
            TechnologyDescription = "Draft technology description for testing purposes",
            ProductAdvantages = "Draft advantages",
            DevelopmentPhase = "Concept",
            DevelopmentProcess = "Early stage",
            TargetMarket = "Test market",
            TargetCustomerBase = "Test customers",
            TargetBeneficiaries = "Test beneficiaries and early adopters",
            TargetCustomerType = "B2B",
            ProductKeywords = "draft, test",
            AdvantageKeywords = "testing",
            Status = InnovationStatus.Draft
        };

        db.Innovations.AddRange(publishedInnovation, draftInnovation);

        // Create existing bid (for duplicate check test)
        var existingBid = new Bid(Guid.NewGuid())
        {
            InnovationId = _publishedInnovationId,
            ActorId = _existingBidActorId,
            Location = "New York, USA",
            ParticipationType = "Investment Partner",
            ParticipationProposal = "We are a venture capital firm with $500M under management, specializing in clean energy technologies. We have successfully funded 15 battery technology companies, bringing 8 to market with combined market cap of $2.3B. We can provide Series A funding ($5-15M) and strategic guidance.",
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        db.Bids.Add(existingBid);

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Test: Manufacturing actor can successfully submit a valid bid
    /// Expected: 201 Created with bid details
    /// </summary>
    [Fact]
    public async Task SubmitBid_ValidManufacturing_Returns201Created()
    {
        // Arrange
        var token = await GetAccessToken("manufacturer@test.com", "Password123!");
        var request = new SubmitBid.SubmitBidRequest
        {
            Location = "Munich, Germany",
            ParticipationType = "Manufacturing Partner",
            ParticipationProposal = "We have 20 years of experience in precision battery manufacturing with ISO 9001 certification. Our facility in Munich has capacity for pilot production runs of 10,000 units/month with potential to scale to 100,000 units/month. We can provide design-for-manufacturing consultation during R&D phase and establish supply chain partnerships with tier-1 automotive suppliers."
        };

        var requestContent = JsonContent.Create(request, options: new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedInnovationId}/bids",
            requestContent,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SubmitBid.SubmitBidResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.BidId);
        Assert.Equal(_publishedInnovationId, result.InnovationId);
        Assert.Equal(_manufacturingActorId, result.ActorId);
        Assert.Equal("Munich, Germany", result.Location);
        Assert.Equal("Manufacturing Partner", result.ParticipationType);
        Assert.Equal("Pending", result.Status);

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/bids/{result.BidId}", response.Headers.Location.ToString());

        // Verify bid saved to database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedBid = await db.Bids.FindAsync(result.BidId);
        Assert.NotNull(savedBid);
        Assert.Equal(_publishedInnovationId, savedBid.InnovationId);
        Assert.Equal(_manufacturingActorId, savedBid.ActorId);
        Assert.Equal(BidStatus.Pending, savedBid.Status);
    }

    /// <summary>
    /// Test: Idea Generator actor cannot submit bids (R4.1)
    /// Expected: 403 Forbidden
    /// </summary>
    [Fact]
    public async Task SubmitBid_IdeaGeneratorAttempt_Returns403Forbidden()
    {
        // Arrange
        var token = await GetAccessToken("innovator@test.com", "Password123!");
        var request = new SubmitBid.SubmitBidRequest
        {
            Location = "San Francisco, USA",
            ParticipationType = "Technical Partner",
            ParticipationProposal = "I am an idea generator trying to bid on someone else's innovation. This should be forbidden per R4.1 business rule. Idea Generators can only submit their own innovations, not bid on others. This text is over 200 characters to pass validation."
        };

        var requestContent = JsonContent.Create(request, options: new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedInnovationId}/bids",
            requestContent,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idea Generators cannot submit bids", content);
    }

    /// <summary>
    /// Test: Actor cannot submit duplicate bid for same innovation (R4.1)
    /// Expected: 409 Conflict
    /// </summary>
    [Fact]
    public async Task SubmitBid_DuplicateBid_Returns409Conflict()
    {
        // Arrange
        var token = await GetAccessToken("investor@test.com", "Password123!");
        var request = new SubmitBid.SubmitBidRequest
        {
            Location = "Boston, USA",
            ParticipationType = "Investment Partner",
            ParticipationProposal = "This is a duplicate bid attempt. The investor actor already has a bid on this innovation. Per R4.1, duplicate bids are not allowed. This proposal text is over 200 characters to meet the minimum length requirement for the participation proposal field."
        };

        var requestContent = JsonContent.Create(request, options: new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedInnovationId}/bids",
            requestContent,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already submitted a bid", content);
    }

    /// <summary>
    /// Test: Proposal must be at least 200 characters (R4.2)
    /// Expected: 400 Bad Request with validation error
    /// </summary>
    [Fact]
    public async Task SubmitBid_ShortProposal_Returns400BadRequest()
    {
        // Arrange
        var token = await GetAccessToken("researcher@test.com", "Password123!");
        var request = new SubmitBid.SubmitBidRequest
        {
            Location = "Cambridge, UK",
            ParticipationType = "R&D Collaboration",
            ParticipationProposal = "Too short."  // Only 10 characters, minimum is 200
        };

        var requestContent = JsonContent.Create(request, options: new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedInnovationId}/bids",
            requestContent,
            token);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("at least 200 characters", content);
    }

    /// <summary>
    /// Helper method to get JWT access token for authentication
    /// </summary>
    private async Task<string> GetAccessToken(string email, string password)
    {
        var loginRequest = new
        {
            email = email,
            password = password
        };

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var accessToken = result.GetProperty("accessToken").GetString();
        Assert.NotNull(accessToken);

        return accessToken;
    }
}
