using System.Net;
using System.Net.Http.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Tests.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Innoventity.API.Tests.Integration.Features.Bids;

/// <summary>
/// Integration tests verifying that ActorResolutionFilter correctly short-circuits with 401
/// when the JWT identifies an actor that no longer exists in the database. All FormalResponse
/// endpoints (Spec 005) share the same filter, so these are characterisation tests locking in
/// the expected 401 behaviour across the GetBids and SubmitManufacturingResponse endpoints.
/// </summary>
public class ActorResolutionFilterTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    private readonly Guid _ownerId = new("f1000001-0000-0000-0000-000000000000");
    private readonly Guid _manufacturerId = new("f1000002-0000-0000-0000-000000000000");
    private readonly Guid _innovationId = new("f2000001-0000-0000-0000-000000000000");
    private readonly Guid _responseId  = new("f3000001-0000-0000-0000-000000000000");

    public ActorResolutionFilterTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"TestDb_ActorResolutionFilter_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
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
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Actors.AddRange(
            new Actor(_ownerId)
            {
                Email = "owner@filter.test",
                FirstName = "Sarah",
                LastName = "Chen",
                ActorType = ActorType.IdeaGenerator,
                AccountStatus = AccountStatus.Active,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                PasswordSalt = Convert.ToBase64String(new byte[32])
            },
            new Actor(_manufacturerId)
            {
                Email = "manufacturer@filter.test",
                FirstName = "Hans",
                LastName = "Mueller",
                ActorType = ActorType.Manufacturing,
                AccountStatus = AccountStatus.Active,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                PasswordSalt = Convert.ToBase64String(new byte[32])
            });

        db.Innovations.Add(new Innovation(_innovationId)
        {
            OwnerId = _ownerId,
            Title = "Filter Test Innovation",
            ProductType = "Test Product",
            ResearchBackground = "Background for actor resolution filter tests — validates that revoked actors cannot reach endpoint handlers.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "None",
            ProductDescription = "Product description for filter test.",
            TechnologyDescription = "Technology description for filter test.",
            TargetBeneficiaries = "Test beneficiaries.",
            ProductAdvantages = "Test advantages.",
            AdvantageKeywords = "test, filter, security",
            DevelopmentPhase = "Concept",
            DevelopmentProcess = "Lab validation.",
            TargetMarket = "Test market.",
            TargetCustomerBase = "Test customers.",
            TargetCustomerType = "B2B",
            ProductKeywords = "test, security",
            RelevantMarketSize = 1_000_000,
            PotentialMarketSize = 5_000_000,
            Status = InnovationStatus.Published,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        });

        db.FormalResponses.Add(new ManufacturingResponse(_responseId)
        {
            InnovationId = _innovationId,
            ActorId = _manufacturerId,
            Location = GeographicRegion.Europe,
            ParticipationType = "Manufacturing Partner",
            ParticipationProposal = "We have 20 years of experience in precision battery manufacturing with ISO 9001 certification. Our Munich facility supports pilot runs of 10,000 units per month scalable to 100,000 units, with full supply chain and DfM consultation services included.",
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddHours(-2),
            YearlyManufacturingCosts =
            [
                new YearlyManufacturingCost
                {
                    Year = 1,
                    ProductionVolume = 10000,
                    ProductionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput.",
                    UnitCost = 5.50m,
                    UnitCostRationale = "Materials, labor, and overhead costed against supplier agreements.",
                    AverageGlobalDistributionExpense = 1.20m,
                    AvgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
                }
            ]
        });

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> GetAccessTokenAsync(string email, string actorType)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { email, actorType, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var token = result.GetProperty("accessToken").GetString();
        Assert.NotNull(token);
        return token;
    }

    private async Task DeleteActorAsync(Guid actorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actor = await db.Actors.FindAsync(actorId);
        if (actor != null)
        {
            db.Actors.Remove(actor);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Characterisation: GetBids already queries the Actors table, so it returns 401 for a
    /// revoked actor both before and after the filter refactor. This test locks that behaviour.
    /// </summary>
    [Fact]
    public async Task GetBids_WithValidJwtButActorDeletedFromDb_Returns401()
    {
        // Arrange
        var token = await GetAccessTokenAsync("manufacturer@filter.test", "Manufacturing");
        await DeleteActorAsync(_manufacturerId);

        // Act
        var response = await _client.GetWithAuthAsync(
            $"/innovations/{_innovationId}/bids", token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Characterisation: SubmitManufacturingResponse queries the Actors table via
    /// ActorResolutionFilter, so it returns 401 for a revoked actor. This test locks that
    /// behaviour for the FormalResponse hierarchy's typed submission endpoints (Spec 005).
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_WithValidJwtButActorDeletedFromDb_Returns401()
    {
        // Arrange
        var token = await GetAccessTokenAsync("manufacturer@filter.test", "Manufacturing");
        await DeleteActorAsync(_manufacturerId);

        var content = JsonContent.Create(new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = "Full proposal text for the SubmitManufacturingResponse characterisation test. We have 20 years of experience in precision manufacturing with ISO 9001 certification and a Munich facility capable of scaling from prototype to mass production with comprehensive supply chain management and quality assurance.",
            yearlyManufacturingCosts = new[]
            {
                new
                {
                    year = 1,
                    productionVolume = 10000,
                    productionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput.",
                    unitCost = 5.50,
                    unitCostRationale = "Materials, labor, and overhead costed against supplier agreements.",
                    averageGlobalDistributionExpense = 1.20,
                    avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
                }
            }
        });

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_innovationId}/bids/manufacturing", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
