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

namespace Innoventity.API.Tests.Integration.Features.Bids;

/// <summary>
/// Integration tests for GET /innovations/{innovationId}/bids (Spec 005 US5).
/// Covers 3-tier visibility (owner / submitting actor / other authenticated actors),
/// the discriminator-based `type` filter, and the InvestorResponse.Feedback visibility rule.
/// </summary>
public class GetBidsTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    // Actors
    private readonly Guid _ownerId = new Guid("11111111-1111-1111-1111-111111111111");
    private readonly Guid _manufacturingActorId = new Guid("22222222-2222-2222-2222-222222222222");
    private readonly Guid _salesActorId = new Guid("33333333-3333-3333-3333-333333333333");
    private readonly Guid _rdActorId = new Guid("44444444-4444-4444-4444-444444444444");
    private readonly Guid _investorActorId = new Guid("55555555-5555-5555-5555-555555555555");
    private readonly Guid _thirdPartyActorId = new Guid("66666666-6666-6666-6666-666666666666");

    // Innovations
    private readonly Guid _innovationWithResponsesId = new Guid("88888888-8888-8888-8888-888888888888");
    private readonly Guid _innovationNoResponsesId = new Guid("99999999-9999-9999-9999-999999999999");

    // Response ids
    private readonly Guid _manufacturingResponseId = new Guid("aa111111-1111-1111-1111-111111111111");
    private readonly Guid _salesResponseId = new Guid("aa222222-2222-2222-2222-222222222222");
    private readonly Guid _rdResponseId = new Guid("aa333333-3333-3333-3333-333333333333");
    private readonly Guid _investorResponseId = new Guid("aa444444-4444-4444-4444-444444444444");

    public GetBidsTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"TestDb_GetBids_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
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
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    private static string MakeProposal(string who) =>
        $"{who} detailed partnership proposal covering capabilities, production capacity, quality standards, " +
        "timeline commitments, and strategic partnership vision for this collaboration opportunity at full scale.";

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Actor MakeActor(Guid id, string email, string first, string last, ActorType type) => new(id)
        {
            Email = email,
            FirstName = first,
            LastName = last,
            ActorType = type,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var owner = MakeActor(_ownerId, "owner@getbids.test", "Sarah", "Chen", ActorType.IdeaGenerator);
        var mfgActor = MakeActor(_manufacturingActorId, "mfg@getbids.test", "Hans", "Mueller", ActorType.Manufacturing);
        var salesActor = MakeActor(_salesActorId, "sales@getbids.test", "Nina", "Park", ActorType.SalesMarketing);
        var rdActor = MakeActor(_rdActorId, "rd@getbids.test", "Emily", "Watson", ActorType.RD);
        var investorActor = MakeActor(_investorActorId, "investor@getbids.test", "David", "Smith", ActorType.Investor);
        var thirdParty = MakeActor(_thirdPartyActorId, "thirdparty@getbids.test", "John", "Doe", ActorType.IdeaGenerator);

        db.Actors.AddRange(owner, mfgActor, salesActor, rdActor, investorActor, thirdParty);

        Innovation MakeInnovation(Guid id) => new(id)
        {
            OwnerId = _ownerId,
            IdeaToken = Guid.NewGuid(),
            Title = $"Test Innovation {id:N}",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Background covering the research area and motivation for this innovation.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Product description for testing GetBids visibility.",
            TechnologyDescription = "Technology description providing technical details for the innovation.",
            TargetBeneficiaries = "Manufacturers, distributors, and end consumers of the product.",
            ProductAdvantages = "Cost reduction, quality improvement, and faster time-to-market.",
            DevelopmentPhase = "Prototype",
            DevelopmentProcess = "Lab validation with pilot production runs planned.",
            TargetMarket = "Global technology markets.",
            TargetCustomerBase = "Enterprise customers in manufacturing and distribution.",
            TargetCustomerType = "B2B",
            ProductKeywords = "technology, innovation, testing",
            AdvantageKeywords = "efficiency, quality, scale",
            Status = InnovationStatus.Published,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var innovationWithResponses = MakeInnovation(_innovationWithResponsesId);
        var innovationNoResponses = MakeInnovation(_innovationNoResponsesId);
        db.Innovations.AddRange(innovationWithResponses, innovationNoResponses);

        var manufacturingResponse = new ManufacturingResponse(_manufacturingResponseId)
        {
            InnovationId = _innovationWithResponsesId,
            ActorId = _manufacturingActorId,
            Location = GeographicRegion.Europe,
            ParticipationType = "Manufacturing Partner",
            ParticipationProposal = MakeProposal("Manufacturing"),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-4),
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
        };

        var salesResponse = new SalesMarketingResponse(_salesResponseId)
        {
            InnovationId = _innovationWithResponsesId,
            ActorId = _salesActorId,
            Location = GeographicRegion.Americas,
            ParticipationType = "Sales & Marketing Partner",
            ParticipationProposal = MakeProposal("Sales & Marketing"),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
            YearlySales =
            [
                new YearlySale
                {
                    Year = 1,
                    UnitsSold = 5000,
                    UnitsSoldRationale = "Conservative estimate based on comparable product launches.",
                    UnitPrice = 49.99m,
                    UnitPriceRationale = "Market pricing analysis shows strong demand at this price point.",
                    SalesMarketingExpense = 25000.00m,
                    SalesMarketingExpenseRationale = "Channel costs plus digital marketing spend for launch quarter."
                }
            ]
        };

        var rdResponse = new ResearchDevelopmentResponse(_rdResponseId)
        {
            InnovationId = _innovationWithResponsesId,
            ActorId = _rdActorId,
            Location = GeographicRegion.Asia,
            ParticipationType = "R&D Partner",
            ParticipationProposal = MakeProposal("R&D"),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2),
            ProductDevelopmentDuration = 2,
            YearlyDevelopmentCosts =
            [
                new YearlyDevelopmentCost
                {
                    Year = 1,
                    InfrastructureCost = 50000.00m,
                    InfrastructureCostRationale = "Cloud hosting, lab tooling, and software licenses for year one.",
                    PeopleCost = 200000.00m,
                    PeopleCostRationale = "Three engineers and one project manager allocated to this workstream."
                }
            ]
        };

        var investorResponse = new InvestorResponse(_investorResponseId)
        {
            InnovationId = _innovationWithResponsesId,
            ActorId = _investorActorId,
            Location = GeographicRegion.Europe,
            ParticipationType = "Investment Partner",
            ParticipationProposal = MakeProposal("Investor"),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a Series A round."
        };

        db.FormalResponses.AddRange(manufacturingResponse, salesResponse, rdResponse, investorResponse);

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> GetAccessToken(string email, string actorType)
    {
        var loginRequest = new { email, actorType, password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var token = result.GetProperty("accessToken").GetString();
        Assert.NotNull(token);
        return token;
    }

    private async Task<JsonElement> GetResponses(Guid innovationId, string token, string? typeFilter = null)
    {
        var url = $"/innovations/{innovationId}/bids";
        if (typeFilter != null)
        {
            url += $"?type={typeFilter}";
        }

        var response = await _client.GetWithAuthAsync(url, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static JsonElement FindByType(JsonElement responses, string responseType) =>
        responses.EnumerateArray().First(e => e.GetProperty("responseType").GetString() == responseType);

    /// <summary>
    /// (1) Owner receives full projection data for all response types.
    /// </summary>
    [Fact]
    public async Task GetBids_AsOwner_ReturnsFullDataForAllResponseTypes()
    {
        var token = await GetAccessToken("owner@getbids.test", "IdeaGenerator");
        var body = await GetResponses(_innovationWithResponsesId, token);
        var responses = body.GetProperty("responses");
        Assert.Equal(4, responses.GetArrayLength());

        var mfg = FindByType(responses, "ManufacturingResponse");
        Assert.True(mfg.TryGetProperty("participationProposal", out _));
        Assert.True(mfg.TryGetProperty("yearlyManufacturingCosts", out var mfgCosts));
        Assert.Equal(1, mfgCosts.GetArrayLength());

        var sales = FindByType(responses, "SalesMarketingResponse");
        Assert.True(sales.TryGetProperty("yearlySales", out var yearlySales));
        Assert.Equal(1, yearlySales.GetArrayLength());

        var rd = FindByType(responses, "ResearchDevelopmentResponse");
        Assert.True(rd.TryGetProperty("productDevelopmentDuration", out var duration));
        Assert.Equal(2, duration.GetInt32());
        Assert.True(rd.TryGetProperty("yearlyDevelopmentCosts", out var devCosts));
        Assert.Equal(1, devCosts.GetArrayLength());

        var investor = FindByType(responses, "InvestorResponse");
        Assert.True(investor.TryGetProperty("feedback", out var feedback));
        Assert.False(string.IsNullOrWhiteSpace(feedback.GetString()));
    }

    /// <summary>
    /// (2) Submitting actor receives own full response; other responses show public fields only.
    /// </summary>
    [Fact]
    public async Task GetBids_AsSubmittingActor_ReturnsOwnFullDataAndPublicForOthers()
    {
        var token = await GetAccessToken("mfg@getbids.test", "Manufacturing");
        var body = await GetResponses(_innovationWithResponsesId, token);
        var responses = body.GetProperty("responses");

        var own = FindByType(responses, "ManufacturingResponse");
        Assert.True(own.TryGetProperty("participationProposal", out _));
        Assert.True(own.TryGetProperty("yearlyManufacturingCosts", out _));

        var other = FindByType(responses, "SalesMarketingResponse");
        Assert.False(other.TryGetProperty("participationProposal", out _));
        Assert.False(other.TryGetProperty("yearlySales", out _));
    }

    /// <summary>
    /// (3) Third-party actor (no submission on this innovation) sees only public summary fields.
    /// </summary>
    [Fact]
    public async Task GetBids_AsThirdPartyActor_ReturnsPublicFieldsOnlyForAllResponses()
    {
        var token = await GetAccessToken("thirdparty@getbids.test", "IdeaGenerator");
        var body = await GetResponses(_innovationWithResponsesId, token);
        var responses = body.GetProperty("responses");
        Assert.Equal(4, responses.GetArrayLength());

        foreach (var entry in responses.EnumerateArray())
        {
            Assert.True(entry.TryGetProperty("responseType", out _));
            Assert.True(entry.TryGetProperty("actorId", out _));
            Assert.True(entry.TryGetProperty("location", out _));
            Assert.True(entry.TryGetProperty("participationType", out _));
            Assert.True(entry.TryGetProperty("status", out _));
            Assert.True(entry.TryGetProperty("submittedAt", out _));

            Assert.False(entry.TryGetProperty("participationProposal", out _));
            Assert.False(entry.TryGetProperty("feedback", out _));
            Assert.False(entry.TryGetProperty("yearlyManufacturingCosts", out _));
            Assert.False(entry.TryGetProperty("yearlySales", out _));
            Assert.False(entry.TryGetProperty("yearlyDevelopmentCosts", out _));
        }
    }

    /// <summary>
    /// (4) ?type=manufacturing filter returns only ManufacturingResponse entries.
    /// </summary>
    [Fact]
    public async Task GetBids_TypeFilterManufacturing_ReturnsOnlyManufacturingResponses()
    {
        var token = await GetAccessToken("owner@getbids.test", "IdeaGenerator");
        var body = await GetResponses(_innovationWithResponsesId, token, "manufacturing");
        var responses = body.GetProperty("responses");

        Assert.Equal(1, responses.GetArrayLength());
        Assert.All(responses.EnumerateArray(), e => Assert.Equal("ManufacturingResponse", e.GetProperty("responseType").GetString()));
    }

    /// <summary>
    /// (5) responseType discriminator is present in every view tier (SC-008).
    /// </summary>
    [Fact]
    public async Task GetBids_ResponseTypeDiscriminatorPresent_InAllVisibilityTiers()
    {
        var ownerToken = await GetAccessToken("owner@getbids.test", "IdeaGenerator");
        var submitterToken = await GetAccessToken("mfg@getbids.test", "Manufacturing");
        var thirdPartyToken = await GetAccessToken("thirdparty@getbids.test", "IdeaGenerator");

        foreach (var token in new[] { ownerToken, submitterToken, thirdPartyToken })
        {
            var body = await GetResponses(_innovationWithResponsesId, token);
            var responses = body.GetProperty("responses");
            Assert.All(responses.EnumerateArray(), e => Assert.True(e.TryGetProperty("responseType", out var rt) && !string.IsNullOrEmpty(rt.GetString())));
        }
    }

    /// <summary>
    /// (6) InvestorResponse.Feedback visibility follows the same 3-tier rule as participationProposal.
    /// </summary>
    [Fact]
    public async Task GetBids_InvestorFeedback_VisibleToOwnerOnly_AbsentForThirdParty()
    {
        var ownerToken = await GetAccessToken("owner@getbids.test", "IdeaGenerator");
        var ownerBody = await GetResponses(_innovationWithResponsesId, ownerToken);
        var ownerInvestor = FindByType(ownerBody.GetProperty("responses"), "InvestorResponse");
        Assert.True(ownerInvestor.TryGetProperty("feedback", out _));

        var thirdPartyToken = await GetAccessToken("thirdparty@getbids.test", "IdeaGenerator");
        var thirdPartyBody = await GetResponses(_innovationWithResponsesId, thirdPartyToken);
        var thirdPartyInvestor = FindByType(thirdPartyBody.GetProperty("responses"), "InvestorResponse");
        Assert.False(thirdPartyInvestor.TryGetProperty("feedback", out _));
    }

    /// <summary>
    /// Innovation with no responses returns an empty list.
    /// </summary>
    [Fact]
    public async Task GetBids_InnovationWithNoResponses_ReturnsEmptyList()
    {
        var token = await GetAccessToken("owner@getbids.test", "IdeaGenerator");
        var body = await GetResponses(_innovationNoResponsesId, token);
        Assert.Equal(0, body.GetProperty("responses").GetArrayLength());
    }

    /// <summary>
    /// Unauthenticated request returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetBids_Unauthenticated_Returns401Unauthorized()
    {
        var response = await _client.GetAsync($"/innovations/{_innovationWithResponsesId}/bids");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Non-existent innovation returns 404 Not Found.
    /// </summary>
    [Fact]
    public async Task GetBids_NonExistentInnovation_Returns404NotFound()
    {
        var token = await GetAccessToken("owner@getbids.test", "IdeaGenerator");
        var nonExistentInnovationId = Guid.NewGuid();

        var response = await _client.GetWithAuthAsync(
            $"/innovations/{nonExistentInnovationId}/bids",
            token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Innovation not found", content);
    }
}
