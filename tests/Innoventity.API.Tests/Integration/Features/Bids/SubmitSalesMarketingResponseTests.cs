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
/// Integration tests for POST /innovations/{innovationId}/bids/sales (Spec 005 US2).
/// </summary>
public class SubmitSalesMarketingResponseTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    private readonly Guid _ownerId = new Guid("e1000001-0000-0000-0000-000000000000");
    private readonly Guid _salesActorId = new Guid("e1000002-0000-0000-0000-000000000000");
    private readonly Guid _mfgActorId = new Guid("e1000003-0000-0000-0000-000000000000");
    private readonly Guid _existingResponderId = new Guid("e1000004-0000-0000-0000-000000000000");

    private readonly Guid _publishedId = new Guid("e2000001-0000-0000-0000-000000000000");
    private readonly Guid _draftId = new Guid("e2000002-0000-0000-0000-000000000000");
    private readonly Guid _withExistingResponseId = new Guid("e2000003-0000-0000-0000-000000000000");
    private readonly Guid _ownedBySalesId = new Guid("e2000004-0000-0000-0000-000000000000");

    public SubmitSalesMarketingResponseTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"TestDb_SubmitSales_{Guid.NewGuid()}";

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

        Actor MakeActor(Guid id, string email, ActorType type) => new(id)
        {
            Email = email,
            FirstName = "Test",
            LastName = "Actor",
            ActorType = type,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };

        var owner = MakeActor(_ownerId, "owner@submitsales.test", ActorType.IdeaGenerator);
        var salesActor = MakeActor(_salesActorId, "sales@submitsales.test", ActorType.SalesMarketing);
        var mfgActor = MakeActor(_mfgActorId, "mfg@submitsales.test", ActorType.Manufacturing);
        var existingResponder = MakeActor(_existingResponderId, "existing@submitsales.test", ActorType.SalesMarketing);

        db.Actors.AddRange(owner, salesActor, mfgActor, existingResponder);

        Innovation MakeInnovation(Guid id, InnovationStatus status, Guid? ownerId = null) => new(id)
        {
            OwnerId = ownerId ?? _ownerId,
            IdeaToken = Guid.NewGuid(),
            Title = $"Test Innovation {id:N}",
            ProductType = "Technology",
            ResearchBackground = "Background for test innovation covering the research area and motivation.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Product description for testing sales response submission.",
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
            Status = status,
            SubmittedAt = status == InnovationStatus.Draft ? null : DateTimeOffset.UtcNow.AddDays(-5)
        };

        var published = MakeInnovation(_publishedId, InnovationStatus.Published);
        var draft = MakeInnovation(_draftId, InnovationStatus.Draft);
        var withExisting = MakeInnovation(_withExistingResponseId, InnovationStatus.Published);
        var ownedBySales = MakeInnovation(_ownedBySalesId, InnovationStatus.Published, ownerId: _salesActorId);

        db.Innovations.AddRange(published, draft, withExisting, ownedBySales);

        db.FormalResponses.Add(new SalesMarketingResponse(Guid.NewGuid())
        {
            InnovationId = _withExistingResponseId,
            ActorId = _existingResponderId,
            Location = GeographicRegion.Americas,
            ParticipationType = "Sales Partner",
            ParticipationProposal = MakeProposal(),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1),
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
        });

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static string MakeProposal() =>
        "Detailed sales and marketing partnership proposal covering go-to-market strategy, channel " +
        "relationships, sales team scale, and revenue commitments for this collaboration opportunity.";

    private static List<object> MakeValidYears(int count = 2) =>
        Enumerable.Range(1, count).Select(year => (object)new
        {
            year,
            unitsSold = 5000 * year,
            unitsSoldRationale = "Conservative estimate based on comparable product launches in this segment.",
            unitPrice = 49.99,
            unitPriceRationale = "Market pricing analysis shows strong demand at this price point currently.",
            salesMarketingExpense = 25000.00,
            salesMarketingExpenseRationale = "Channel costs plus digital marketing spend for the launch quarter."
        }).ToList();

    private object MakeValidRequest(int years = 2) => new
    {
        location = "Americas",
        participationType = "Sales & Marketing Partner",
        participationProposal = MakeProposal(),
        yearlySales = MakeValidYears(years)
    };

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

    [Fact]
    public async Task SubmitSalesMarketingResponse_WrongActorType_Returns403()
    {
        var token = await GetAccessToken("mfg@submitsales.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sales & Marketing actors", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_ActorOwnsInnovation_Returns403()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_ownedBySalesId}/bids/sales",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("own innovation", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_InnovationNotFound_Returns404()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{Guid.NewGuid()}/bids/sales",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_InvalidLocation_Returns400()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var request = new
        {
            location = "Not-A-Region",
            participationType = "Sales & Marketing Partner",
            participationProposal = MakeProposal(),
            yearlySales = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Location", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_Duplicate_Returns409()
    {
        var token = await GetAccessToken("existing@submitsales.test", "SalesMarketing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_withExistingResponseId}/bids/sales",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Duplicate", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already submitted", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_InnovationNotPublished_Returns404()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_draftId}/bids/sales",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("published", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_RationaleTooShort_Returns422WithFieldName()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = MakeProposal(),
            yearlySales = new[]
            {
                new
                {
                    year = 1,
                    unitsSold = 5000,
                    unitsSoldRationale = "Too short",
                    unitPrice = 49.99,
                    unitPriceRationale = "Market pricing analysis shows strong demand at this price point currently.",
                    salesMarketingExpense = 25000.00,
                    salesMarketingExpenseRationale = "Channel costs plus digital marketing spend for the launch quarter."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("UnitsSoldRationale", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_YearGap_Returns422()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = MakeProposal(),
            yearlySales = new object[]
            {
                MakeValidYears(1)[0],
                new
                {
                    year = 3,
                    unitsSold = 6000,
                    unitsSoldRationale = "Conservative estimate based on comparable product launches in this segment.",
                    unitPrice = 51.99,
                    unitPriceRationale = "Market pricing analysis shows strong demand at this price point currently.",
                    salesMarketingExpense = 27000.00,
                    salesMarketingExpenseRationale = "Channel costs plus digital marketing spend for the launch quarter."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("contiguous", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing year(s): 2", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_YearAboveTen_Returns422()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var years = Enumerable.Range(1, 11).Select(year => (object)new
        {
            year,
            unitsSold = 5000,
            unitsSoldRationale = "Conservative estimate based on comparable product launches in this segment.",
            unitPrice = 49.99,
            unitPriceRationale = "Market pricing analysis shows strong demand at this price point currently.",
            salesMarketingExpense = 25000.00,
            salesMarketingExpenseRationale = "Channel costs plus digital marketing spend for the launch quarter."
        }).ToList();

        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = MakeProposal(),
            yearlySales = years
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot exceed 10", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Exactly 10 years is accepted (upper boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitSalesMarketingResponse_ExactlyTenYears_Returns201()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(MakeValidRequest(years: 10)), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Rationale exactly at the 20-char minimum is accepted (boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitSalesMarketingResponse_RationaleExactlyTwentyChars_Returns201()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var exactlyTwenty = new string('a', 20);
        Assert.Equal(20, exactlyTwenty.Length);
        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = MakeProposal(),
            yearlySales = new[]
            {
                new
                {
                    year = 1,
                    unitsSold = 5000,
                    unitsSoldRationale = exactlyTwenty,
                    unitPrice = 49.99,
                    unitPriceRationale = exactlyTwenty,
                    salesMarketingExpense = 25000.00,
                    salesMarketingExpenseRationale = exactlyTwenty
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Rationale exceeding the 500-char maximum is rejected.
    /// </summary>
    [Fact]
    public async Task SubmitSalesMarketingResponse_RationaleAboveFiveHundredChars_Returns422()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var tooLong = new string('a', 501);
        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = MakeProposal(),
            yearlySales = new[]
            {
                new
                {
                    year = 1,
                    unitsSold = 5000,
                    unitsSoldRationale = tooLong,
                    unitPrice = 49.99,
                    unitPriceRationale = "Market pricing analysis shows strong demand at this price point currently.",
                    salesMarketingExpense = 25000.00,
                    salesMarketingExpenseRationale = "Channel costs plus digital marketing spend for the launch quarter."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("UnitsSoldRationale", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_ParticipationProposalTooShort_Returns400()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = "Too short",
            yearlySales = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationProposal", body, StringComparison.Ordinal);
        Assert.Contains("100", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// participationProposal exactly at the 100-char minimum is accepted (boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitSalesMarketingResponse_ParticipationProposalExactlyOneHundredChars_Returns201()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var exactlyOneHundred = new string('a', 100);
        Assert.Equal(100, exactlyOneHundred.Length);
        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = exactlyOneHundred,
            yearlySales = MakeValidYears(1)
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Year 1 complete + year 2 missing a rationale field entirely → 422 (FR-009 partial projection rejection).
    /// </summary>
    [Fact]
    public async Task SubmitSalesMarketingResponse_PartialProjectionEntry_Returns422()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var request = new
        {
            location = "Americas",
            participationType = "Sales & Marketing Partner",
            participationProposal = MakeProposal(),
            yearlySales = new object[]
            {
                MakeValidYears(1)[0],
                new
                {
                    year = 2,
                    unitsSold = 6000,
                    unitPrice = 51.99,
                    unitPriceRationale = "Market pricing analysis shows strong demand at this price point currently.",
                    salesMarketingExpense = 27000.00,
                    salesMarketingExpenseRationale = "Channel costs plus digital marketing spend for the launch quarter."
                    // unitsSoldRationale intentionally omitted
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SubmitSalesMarketingResponse_HappyPath_Returns201()
    {
        var token = await GetAccessToken("sales@submitsales.test", "SalesMarketing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/sales",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("SalesMarketingResponse", body.GetProperty("responseType").GetString());
        Assert.Equal("Pending", body.GetProperty("status").GetString());

        var responseId = body.GetProperty("responseId").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.FormalResponses.OfType<SalesMarketingResponse>().FirstOrDefaultAsync(r => r.Id == responseId);
        Assert.NotNull(stored);
        Assert.Equal(2, stored.YearlySales.Count);
        Assert.All(stored.YearlySales, y => Assert.True(y.UnitsSoldRationale.Length >= 20));
    }
}
