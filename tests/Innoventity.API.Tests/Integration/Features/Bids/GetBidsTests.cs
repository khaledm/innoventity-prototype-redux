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
/// Integration tests for T011: GET /innovations/{innovationId}/bids (List Bids for Innovation)
/// Tests business rules R6.1 (owner visibility) and R8.2 (non-owner forbidden)
/// </summary>
public class GetBidsTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    // Test data IDs
    private readonly Guid _innovationOwnerIdeaGeneratorId = new Guid("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nonOwnerActorId = new Guid("22222222-2222-2222-2222-222222222222");
    private readonly Guid _manufacturingBidder1Id = new Guid("33333333-3333-3333-3333-333333333333");
    private readonly Guid _manufacturingBidder2Id = new Guid("44444444-4444-4444-4444-444444444444");
    private readonly Guid _rdBidderId = new Guid("55555555-5555-5555-5555-555555555555");
    private readonly Guid _salesBidderId = new Guid("66666666-6666-6666-6666-666666666666");
    private readonly Guid _investorBidderId = new Guid("77777777-7777-7777-7777-777777777777");

    private readonly Guid _innovationWithBidsId = new Guid("88888888-8888-8888-8888-888888888888");
    private readonly Guid _innovationNoBidsId = new Guid("99999999-9999-9999-9999-999999999999");

    public GetBidsTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Generate unique database name once for this factory instance
        var databaseName = $"TestDb_GetBids_{Guid.NewGuid()}";

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

        // Create innovation owner (Idea Generator)
        var innovationOwner = new Actor(_innovationOwnerIdeaGeneratorId)
        {
            Email = "owner@test.com",
            FirstName = "Sarah",
            LastName = "Chen",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        // Create non-owner actor (another Idea Generator)
        var nonOwner = new Actor(_nonOwnerActorId)
        {
            Email = "nonowner@test.com",
            FirstName = "John",
            LastName = "Doe",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        // Create bidders
        var manufacturingBidder1 = new Actor(_manufacturingBidder1Id)
        {
            Email = "manufacturer1@test.com",
            FirstName = "Hans",
            LastName = "Mueller",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var manufacturingBidder2 = new Actor(_manufacturingBidder2Id)
        {
            Email = "manufacturer2@test.com",
            FirstName = "Franz",
            LastName = "Schmidt",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var rdBidder = new Actor(_rdBidderId)
        {
            Email = "researcher@test.com",
            FirstName = "Emily",
            LastName = "Watson",
            ActorType = ActorType.RD,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var salesBidder = new Actor(_salesBidderId)
        {
            Email = "sales@test.com",
            FirstName = "Michael",
            LastName = "Brown",
            ActorType = ActorType.SalesMarketing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var investorBidder = new Actor(_investorBidderId)
        {
            Email = "investor@test.com",
            FirstName = "David",
            LastName = "Smith",
            ActorType = ActorType.Investor,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        db.Actors.AddRange(
            innovationOwner,
            nonOwner,
            manufacturingBidder1,
            manufacturingBidder2,
            rdBidder,
            salesBidder,
            investorBidder);

        // Create published innovation with bids (owned by innovationOwner)
        var innovationWithBids = new Innovation(_innovationWithBidsId)
        {
            OwnerId = _innovationOwnerIdeaGeneratorId,
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
            AdvantageKeywords = "10x density, fast-charge, long-range, sustainable, cost-effective",
            RelevantMarketSize = 50000000,
            PotentialMarketSize = 150000000,
            Status = InnovationStatus.Published,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        // Create innovation without bids (owned by innovationOwner)
        var innovationNoBids = new Innovation(_innovationNoBidsId)
        {
            OwnerId = _innovationOwnerIdeaGeneratorId,
            Title = "Solar Panel Efficiency Booster",
            ProductType = "Renewable Energy Technology",
            ResearchBackground = "Novel coating technology to increase solar panel efficiency by 25% in low-light conditions through advanced photonic materials.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Advanced photonic coating for solar panels that improves efficiency in low-light conditions.",
            TechnologyDescription = "Quantum dot-based coating materials optimized for specific wavelengths.",
            TargetBeneficiaries = "Solar panel manufacturers, renewable energy installers, residential customers.",
            ProductAdvantages = "Higher efficiency in cloudy weather, better ROI for solar installations.",
            DevelopmentPhase = "Early Prototype",
            DevelopmentProcess = "Lab testing of coating formulations.",
            TargetMarket = "Global solar energy market.",
            TargetCustomerBase = "Solar panel manufacturers.",
            TargetCustomerType = "B2B",
            ProductKeywords = "solar, photonic, coating, efficiency, renewable",
            AdvantageKeywords = "25% efficiency, low-light, cost-effective, easy-install, weather-resistant",
            RelevantMarketSize = 30000000,
            PotentialMarketSize = 100000000,
            Status = InnovationStatus.Published,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        db.Innovations.AddRange(innovationWithBids, innovationNoBids);

        // Create bids for the first innovation (5 bids from different actor types)
        var bid1 = new Bid(Guid.NewGuid())
        {
            InnovationId = _innovationWithBidsId,
            ActorId = _manufacturingBidder1Id,
            Location = "Munich, Germany",
            ParticipationType = "Manufacturing Partner",
            ParticipationProposal = "We have 20 years of experience in precision battery manufacturing with ISO 9001 certification. Our facility in Munich has capacity for pilot production runs of 10,000 units/month with potential to scale to 100,000 units/month. We can provide design-for-manufacturing consultation during R&D phase and establish supply chain partnerships with tier-1 automotive suppliers. Our team includes 150 engineers specialized in battery production with extensive experience in lithium-ion and solid-state battery technologies.",
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-4)
        };

        var bid2 = new Bid(Guid.NewGuid())
        {
            InnovationId = _innovationWithBidsId,
            ActorId = _manufacturingBidder2Id,
            Location = "Stuttgart, Germany",
            ParticipationType = "Manufacturing Partner",
            ParticipationProposal = "Stuttgart Precision Manufacturing GmbH brings 15 years of automotive battery production expertise. We operate a state-of-the-art facility with automated production lines and quality assurance systems meeting automotive grade standards. Our experience with Tesla, BMW, and Volkswagen positions us uniquely to scale production from prototype to mass manufacturing. We can offer competitive pricing due to our efficient processes and existing supply chain relationships with raw material suppliers across Europe.",
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3)
        };

        var bid3 = new Bid(Guid.NewGuid())
        {
            InnovationId = _innovationWithBidsId,
            ActorId = _rdBidderId,
            Location = "Cambridge, UK",
            ParticipationType = "R&D Partner",
            ParticipationProposal = "Cambridge Research Institute has 25 years of materials science expertise focusing on energy storage technologies. Our team of 80 PhD researchers has published over 200 papers on battery technology and hold 45 patents in related fields. We offer access to world-class testing facilities including electron microscopy, X-ray diffraction, and electrochemical workstations. Our collaboration would accelerate product development by 6-12 months through parallel testing methodologies and our extensive network of industry and academic partners. We have successful track record with 12 technology commercializations.",
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3).AddHours(-2)
        };

        var bid4 = new Bid(Guid.NewGuid())
        {
            InnovationId = _innovationWithBidsId,
            ActorId = _salesBidderId,
            Location = "San Francisco, CA, USA",
            ParticipationType = "Sales & Marketing Partner",
            ParticipationProposal = "TechVentures Marketing has successfully launched 15 clean energy products into the North American market over the past 10 years. Our team of 50 sales and marketing professionals have established relationships with all major automotive OEMs and energy storage integrators. We offer comprehensive go-to-market services including brand positioning, channel development, trade show representation, and direct sales support. Our existing distribution network spans 35 states with partnerships covering both B2B2C and direct B2B channels. Expected first-year revenue: $5M with 200% growth in year two.",
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var bid5 = new Bid(Guid.NewGuid())
        {
            InnovationId = _innovationWithBidsId,
            ActorId = _investorBidderId,
            Location = "London, UK",
            ParticipationType = "Investment Partner",
            ParticipationProposal = "GreenTech Ventures is a $500M clean energy investment fund with successful exits in battery storage, solar, and electric vehicle sectors. We offer Series A funding of $10-25M with potential for follow-on rounds up to $100M based on milestones. Beyond capital, we provide strategic guidance through our advisory board of industry executives from Tesla, Panasonic, and CATL. Our portfolio companies benefit from shared resources including legal, accounting, HR, and business development services. We take minority positions (15-25% equity) and actively support board governance without operational interference. Expected ROI timeline: 5-7 years with 10x target multiple.",
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        db.Bids.AddRange(bid1, bid2, bid3, bid4, bid5);

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Test: Innovation owner can retrieve all bids with full details
    /// Expected: 200 OK with 5 bids including actor information
    /// </summary>
    [Fact]
    public async Task GetBids_AsOwner_ReturnsAllBids()
    {
        // Arrange
        var token = await GetAccessToken("owner@test.com", "IdeaGenerator");

        // Act
        var response = await _client.GetWithAuthAsync(
            $"/innovations/{_innovationWithBidsId}/bids",
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetBids.GetBidsResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Bids.Count);

        // Verify bids are ordered by SubmittedAt descending (newest first)
        Assert.Equal(_investorBidderId, result.Bids[0].Actor.ActorId); // Most recent
        Assert.Equal(_salesBidderId, result.Bids[1].Actor.ActorId);
        Assert.Equal(_rdBidderId, result.Bids[2].Actor.ActorId);
        Assert.Equal(_manufacturingBidder2Id, result.Bids[3].Actor.ActorId);
        Assert.Equal(_manufacturingBidder1Id, result.Bids[4].Actor.ActorId); // Oldest

        // Verify actor details are included
        var firstBid = result.Bids[0];
        Assert.NotNull(firstBid.Actor);
        Assert.Equal("David", firstBid.Actor.FirstName);
        Assert.Equal("Smith", firstBid.Actor.LastName);
        Assert.Equal("David Smith", firstBid.Actor.DisplayName);
        Assert.Equal("Investor", firstBid.Actor.ActorType);

        // Verify full proposal text is included
        Assert.Contains("GreenTech Ventures", firstBid.ParticipationProposal);
        Assert.True(firstBid.ParticipationProposal.Length > 200);

        // Verify bid status
        Assert.Equal("Pending", firstBid.Status);
    }

    /// <summary>
    /// Test: Non-owner actor cannot view bids (R8.2)
    /// Expected: 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetBids_AsNonOwner_Returns403Forbidden()
    {
        // Arrange
        var token = await GetAccessToken("nonowner@test.com", "IdeaGenerator");

        // Act
        var response = await _client.GetWithAuthAsync(
            $"/innovations/{_innovationWithBidsId}/bids",
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Only the innovation owner can view submitted bids", content);
    }

    /// <summary>
    /// Test: Verify bid counts grouped by actor type (R4.4 validation)
    /// Expected: 2 Manufacturing, 1 R&D, 1 Sales, 1 Investor
    /// </summary>
    [Fact]
    public async Task GetBids_GroupedByActorType_CorrectCounts()
    {
        // Arrange
        var token = await GetAccessToken("owner@test.com", "IdeaGenerator");

        // Act
        var response = await _client.GetWithAuthAsync(
            $"/innovations/{_innovationWithBidsId}/bids",
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetBids.GetBidsResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);

        // Group bids by actor type and verify counts
        var bidsByActorType = result.Bids.GroupBy(b => b.Actor.ActorType).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, bidsByActorType["Manufacturing"]); // 2 manufacturing bids
        Assert.Equal(1, bidsByActorType["RD"]); // 1 R&D bid
        Assert.Equal(1, bidsByActorType["SalesMarketing"]); // 1 Sales & Marketing bid
        Assert.Equal(1, bidsByActorType["Investor"]); // 1 Investor bid

        // Verify total types represented
        Assert.Equal(4, bidsByActorType.Keys.Count);
    }

    /// <summary>
    /// Test: Innovation with no bids returns empty list
    /// Expected: 200 OK with empty bids array
    /// </summary>
    [Fact]
    public async Task GetBids_InnovationWithNoBids_ReturnsEmptyList()
    {
        // Arrange
        var token = await GetAccessToken("owner@test.com", "IdeaGenerator");

        // Act
        var response = await _client.GetWithAuthAsync(
            $"/innovations/{_innovationNoBidsId}/bids",
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetBids.GetBidsResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Bids);
    }

    /// <summary>
    /// Test: Unauthenticated request returns 401 Unauthorized
    /// Expected: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetBids_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange: No token

        // Act
        var response = await _client.GetAsync($"/innovations/{_innovationWithBidsId}/bids");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test: Non-existent innovation returns 404 Not Found
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetBids_NonExistentInnovation_Returns404NotFound()
    {
        // Arrange
        var token = await GetAccessToken("owner@test.com", "IdeaGenerator");
        var nonExistentInnovationId = Guid.NewGuid();

        // Act
        var response = await _client.GetWithAuthAsync(
            $"/innovations/{nonExistentInnovationId}/bids",
            token);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Innovation not found", content);
    }

    /// <summary>
    /// Helper method to get JWT access token for authentication
    /// </summary>
    private async Task<string> GetAccessToken(string email, string actorType, string password = "Password123!")
    {
        var loginRequest = new
        {
            email = email,
            actorType = actorType,
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
