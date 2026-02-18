using System.Net;
using System.Net.Http.Headers;
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
/// Integration tests for T012: PUT /bids/{bidId} (Update Bid)
/// Tests business rules: R8.2 (author-only updates), bid immutability after acceptance
/// </summary>
public class UpdateBidTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    // Test data IDs
    private readonly Guid _pendingBidId = new Guid("11111111-1111-1111-1111-111111111111");
    private readonly Guid _acceptedBidId = new Guid("22222222-2222-2222-2222-222222222222");
    private readonly Guid _rejectedBidId = new Guid("33333333-3333-3333-3333-333333333333");
    private readonly Guid _bidAuthorId = new Guid("44444444-4444-4444-4444-444444444444");
    private readonly Guid _differentActorId = new Guid("55555555-5555-5555-5555-555555555555");
    private readonly Guid _innovationOwnerId = new Guid("66666666-6666-6666-6666-666666666666");
    private readonly Guid _innovationId = new Guid("77777777-7777-7777-7777-777777777777");

    public UpdateBidTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Generate unique database name once for this factory instance
        var databaseName = $"TestDb_UpdateBid_{Guid.NewGuid()}";

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

        // Seed actors
        var bidAuthor = new Actor(_bidAuthorId)
        {
            Email = "manufacturer@test.com",
            FirstName = "Hans",
            LastName = "Mueller",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var differentActor = new Actor(_differentActorId)
        {
            Email = "researcher@test.com",
            FirstName = "Emily",
            LastName = "Watson",
            ActorType = ActorType.RD,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var innovationOwner = new Actor(_innovationOwnerId)
        {
            Email = "innovator@test.com",
            FirstName = "Sarah",
            LastName = "Chen",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        db.Actors.AddRange(bidAuthor, differentActor, innovationOwner);

        // Seed innovation
        var innovation = new Innovation(_innovationId)
        {
            OwnerId = _innovationOwnerId,
            Title = "Quantum Battery Prototype",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Advanced lithium-air battery technology with quantum-enhanced energy density for next-generation electric vehicles and renewable energy storage systems.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Revolutionary battery technology achieving 10x energy density compared to conventional lithium-ion batteries.",
            TechnologyDescription = "Quantum-enhanced cathode materials enabling unprecedented energy storage capacity.",
            TargetBeneficiaries = "Electric vehicle manufacturers, renewable energy providers, grid storage operators.",
            ProductAdvantages = "Longer range for EVs, faster charging times, reduced environmental impact.",
            AdvantageKeywords = "range, charging, efficiency, sustainability",
            DevelopmentPhase = "Prototype Testing",
            DevelopmentProcess = "Currently in lab validation phase with promising initial results.",
            TargetMarket = "Global electric vehicle and renewable energy markets.",
            TargetCustomerBase = "Automotive OEMs and energy storage companies.",
            TargetCustomerType = "B2B",
            ProductKeywords = "battery, quantum, energy, storage, EV",
            Status = InnovationStatus.Published
        };

        db.Innovations.Add(innovation);

        // Seed bids with different statuses
        var pendingBid = new Bid(_pendingBidId)
        {
            InnovationId = _innovationId,
            ActorId = _bidAuthorId,
            Location = "Munich, Germany",
            ParticipationType = "Manufacturing Partner",
            ParticipationProposal = "We have 20 years of experience in precision electronics manufacturing with ISO 9001 certification. Our facility in Munich has capacity for prototype production runs and can scale to mass production. We're interested in licensing your quantum battery technology for integration into our automotive component line.",
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var acceptedBid = new Bid(_acceptedBidId)
        {
            InnovationId = _innovationId,
            ActorId = _differentActorId,
            Location = "Boston, MA, USA",
            ParticipationType = "R&D Collaboration",
            ParticipationProposal = "Watson Research Lab specializes in advanced materials research with 15 publications in quantum materials. We propose a joint R&D partnership to optimize the cathode materials for commercial viability. Our team has successfully commercialized 3 battery technologies in the past decade.",
            Status = BidStatus.Accepted,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var rejectedBid = new Bid(_rejectedBidId)
        {
            InnovationId = _innovationId,
            ActorId = _differentActorId,
            Location = "London, UK",
            ParticipationType = "Investment Partner",
            ParticipationProposal = "Our venture capital firm specializes in deep tech investments with a $500M fund focused on clean energy technologies. We're interested in leading a Series A round of $10M to support scale-up and commercialization. We have experience with 12 successful battery technology exits.",
            Status = BidStatus.Rejected,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-10)
        };

        db.Bids.AddRange(pendingBid, acceptedBid, rejectedBid);
        db.SaveChanges();
    }

    private async Task<string> GetAccessToken(string email, string actorType, string password = "Password123!")
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

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult?.AccessToken ?? throw new InvalidOperationException("Failed to obtain access token");
    }

    private record LoginResponse(string AccessToken, string RefreshToken, string ActorType);

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Test 1: Bid author successfully updates a pending bid
    /// Validates: R8.2 (author can update), status remains Pending, ModifiedTimestamp updated
    /// </summary>
    [Fact]
    public async Task UpdateBid_PendingBidByAuthor_Returns200OK()
    {
        // Arrange
        var token = await GetAccessToken("manufacturer@test.com", "Manufacturing");
        var updateRequest = new UpdateBidRequest
        {
            Location = "Berlin, Germany",
            ParticipationType = "Manufacturing & Distribution Partner",
            ParticipationProposal = "UPDATED: We have expanded our capabilities to include distribution across EU markets. Our Munich facility now has capacity for 50,000 units per month with quality assurance protocols meeting automotive industry standards. We're committed to a long-term partnership for quantum battery commercialization."
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/bids/{_pendingBidId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(updateRequest);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UpdateBidResponse>();
        Assert.NotNull(result);
        Assert.Equal(_pendingBidId, result.BidId);
        Assert.Equal("Berlin, Germany", result.Location);
        Assert.Equal("Manufacturing & Distribution Partner", result.ParticipationType);
        Assert.Contains("UPDATED:", result.ParticipationProposal);
        Assert.Equal("Pending", result.Status);
        Assert.True(result.UpdatedAt > DateTimeOffset.UtcNow.AddMinutes(-1)); // Recently updated

        // Verify database update
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedBid = await db.Bids.FindAsync(_pendingBidId);
        Assert.NotNull(updatedBid);
        Assert.Equal("Berlin, Germany", updatedBid.Location);
        Assert.Equal(BidStatus.Pending, updatedBid.Status);
        Assert.NotNull(updatedBid.UpdatedAt);
        Assert.True(updatedBid.UpdatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    /// <summary>
    /// Test 2: Non-author actor receives 403 Forbidden when attempting to update bid
    /// Validates: R8.2 (only bid author can update)
    /// </summary>
    [Fact]
    public async Task UpdateBid_NonAuthor_Returns403Forbidden()
    {
        // Arrange - login as different actor (not the bid author)
        var token = await GetAccessToken("researcher@test.com", "RD"); // Different actor
        var updateRequest = new UpdateBidRequest
        {
            Location = "Tokyo, Japan",
            ParticipationType = "Technology License",
            ParticipationProposal = "Malicious actor attempting to modify someone else's bid. This should fail with 403 Forbidden as only the bid author (manufacturing actor) can update this bid. Authorization rules must prevent cross-actor bid modifications to maintain proposal integrity."
        };

        // Act - try to update someone else's bid
        var response = await _client.PutWithAuthAsync(
            $"/bids/{_pendingBidId}",
            JsonContent.Create(updateRequest),
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify bid was NOT updated in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bid = await db.Bids.FindAsync(_pendingBidId);
        Assert.NotNull(bid);
        Assert.Equal("Munich, Germany", bid.Location); // Original location unchanged
        Assert.Equal("Manufacturing Partner", bid.ParticipationType); // Original type unchanged
    }

    /// <summary>
    /// Test 3: Attempting to update an accepted bid returns 409 Conflict
    /// Validates: Accepted bids are immutable
    /// </summary>
    [Fact]
    public async Task UpdateBid_AcceptedBid_Returns409Conflict()
    {
        // Arrange
        var token = await GetAccessToken("researcher@test.com", "RD");
        var updateRequest = new UpdateBidRequest
        {
            Location = "Cambridge, MA, USA",
            ParticipationType = "R&D Partnership",
            ParticipationProposal = "Attempting to update an accepted bid. This should fail with 409 Conflict because accepted bids are immutable to preserve the integrity of partnership agreements. Once a bid is accepted, the terms are locked and cannot be changed to prevent disputes."
        };

        // Act
        var response = await _client.PutWithAuthAsync(
            $"/bids/{_acceptedBidId}",
            JsonContent.Create(updateRequest),
            token);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("accepted", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("immutable", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);

        // Verify bid was NOT updated in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bid = await db.Bids.FindAsync(_acceptedBidId);
        Assert.NotNull(bid);
        Assert.Equal("Boston, MA, USA", bid.Location); // Original location unchanged
        Assert.Equal(BidStatus.Accepted, bid.Status); // Status unchanged
    }

    private record ProblemDetails(string? Title, string? Detail, int Status);
}
