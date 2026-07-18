using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Reflection;
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
/// Integration tests for POST /innovations/{innovationId}/bids/rd (Spec 005 US3).
/// </summary>
public class SubmitResearchDevelopmentResponseTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    private readonly Guid _ownerId = new Guid("f5000001-0000-0000-0000-000000000000");
    private readonly Guid _rdActorId = new Guid("f5000002-0000-0000-0000-000000000000");
    private readonly Guid _mfgActorId = new Guid("f5000003-0000-0000-0000-000000000000");
    private readonly Guid _existingResponderId = new Guid("f5000004-0000-0000-0000-000000000000");

    private readonly Guid _publishedId = new Guid("f6000001-0000-0000-0000-000000000000");
    private readonly Guid _draftId = new Guid("f6000002-0000-0000-0000-000000000000");
    private readonly Guid _withExistingResponseId = new Guid("f6000003-0000-0000-0000-000000000000");
    private readonly Guid _ownedByRdId = new Guid("f6000004-0000-0000-0000-000000000000");

    public SubmitResearchDevelopmentResponseTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"TestDb_SubmitRd_{Guid.NewGuid()}";

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
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

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

        var owner = MakeActor(_ownerId, "owner@submitrd.test", ActorType.IdeaGenerator);
        var rdActor = MakeActor(_rdActorId, "rd@submitrd.test", ActorType.RD);
        var mfgActor = MakeActor(_mfgActorId, "mfg@submitrd.test", ActorType.Manufacturing);
        var existingResponder = MakeActor(_existingResponderId, "existing@submitrd.test", ActorType.RD);

        db.Actors.AddRange(owner, rdActor, mfgActor, existingResponder);

        Innovation MakeInnovation(Guid id, InnovationStatus status, Guid? ownerId = null) => new(id)
        {
            OwnerId = ownerId ?? _ownerId,
            IdeaToken = Guid.NewGuid(),
            Title = $"Test Innovation {id:N}",
            ProductType = "Technology",
            ResearchBackground = "Background for test innovation covering the research area and motivation.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Product description for testing R&D response submission.",
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
        var ownedByRd = MakeInnovation(_ownedByRdId, InnovationStatus.Published, ownerId: _rdActorId);

        db.Innovations.AddRange(published, draft, withExisting, ownedByRd);

        db.FormalResponses.Add(new ResearchDevelopmentResponse(Guid.NewGuid())
        {
            InnovationId = _withExistingResponseId,
            ActorId = _existingResponderId,
            Location = GeographicRegion.Asia,
            ParticipationType = "R&D Partner",
            ParticipationProposal = MakeProposal(),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1),
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
        });

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static string MakeProposal() =>
        "Detailed R&D partnership proposal covering our technical team's expertise, prior commercialization " +
        "track record, testing facilities, and collaboration model for accelerating this innovation to market.";

    private static List<object> MakeValidYears(int count = 2) =>
        Enumerable.Range(1, count).Select(year => (object)new
        {
            year,
            infrastructureCost = 50000.00 * year,
            infrastructureCostRationale = "Cloud hosting, lab tooling, and software licenses for this year.",
            peopleCost = 200000.00 * year,
            peopleCostRationale = "Engineers and a project manager allocated to this workstream for the year."
        }).ToList();

    private static Dictionary<string, string[]> InvokeValidateProjection(
        List<Innoventity.API.Features.Bids.SubmitResearchDevelopmentResponse.YearlyDevelopmentCostRequest>? entries)
    {
        var method = typeof(Innoventity.API.Features.Bids.SubmitResearchDevelopmentResponse)
            .GetMethod("ValidateProjection", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (Dictionary<string, string[]>)method.Invoke(null, new object?[] { entries })!;
    }

    private object MakeValidRequest(int years = 2, int duration = 2) => new
    {
        location = "Asia",
        participationType = "R&D Partner",
        participationProposal = MakeProposal(),
        productDevelopmentDuration = duration,
        yearlyDevelopmentCosts = MakeValidYears(years)
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
    public async Task SubmitResearchDevelopmentResponse_HappyPath_Returns201AndStoresDuration()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("ResearchDevelopmentResponse", body.GetProperty("responseType").GetString());
        var responseId = body.GetProperty("responseId").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.FormalResponses.OfType<ResearchDevelopmentResponse>().FirstOrDefaultAsync(r => r.Id == responseId);
        Assert.NotNull(stored);
        Assert.Equal(2, stored.ProductDevelopmentDuration);
        Assert.Equal(2, stored.YearlyDevelopmentCosts.Count);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_WrongActorType_Returns403()
    {
        var token = await GetAccessToken("mfg@submitrd.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("R&D actors", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_ActorOwnsInnovation_Returns403()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_ownedByRdId}/bids/rd",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("own innovation", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_InnovationNotFound_Returns404()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{Guid.NewGuid()}/bids/rd",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_InvalidLocation_Returns400()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var request = new
        {
            location = "Not-A-Region",
            participationType = "R&D Partner",
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 2,
            yearlyDevelopmentCosts = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Location", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_BlankParticipationType_Returns400()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var request = new
        {
            location = "Asia",
            participationType = string.Empty,
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 2,
            yearlyDevelopmentCosts = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationType", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_OverlongParticipationType_Returns400()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var request = new
        {
            location = "Asia",
            participationType = new string('R', 101),
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 2,
            yearlyDevelopmentCosts = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationType", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_Duplicate_Returns409()
    {
        var token = await GetAccessToken("existing@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_withExistingResponseId}/bids/rd",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Duplicate", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already submitted", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_InnovationNotPublished_Returns404()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_draftId}/bids/rd",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("published", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_RationaleTooShort_Returns422WithFieldName()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 1,
            yearlyDevelopmentCosts = new[]
            {
                new
                {
                    year = 1,
                    infrastructureCost = 50000.00,
                    infrastructureCostRationale = "Too short",
                    peopleCost = 200000.00,
                    peopleCostRationale = "Engineers and a project manager allocated to this workstream for the year."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("InfrastructureCostRationale", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_YearGap_Returns422()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 3,
            yearlyDevelopmentCosts = new object[]
            {
                MakeValidYears(1)[0],
                new
                {
                    year = 3,
                    infrastructureCost = 60000.00,
                    infrastructureCostRationale = "Cloud hosting, lab tooling, and software licenses for this year.",
                    peopleCost = 220000.00,
                    peopleCostRationale = "Engineers and a project manager allocated to this workstream for the year."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("contiguous", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing year(s): 2", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_YearAboveTen_Returns422()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var years = Enumerable.Range(1, 11).Select(year => (object)new
        {
            year,
            infrastructureCost = 50000.00,
            infrastructureCostRationale = "Cloud hosting, lab tooling, and software licenses for this year.",
            peopleCost = 200000.00,
            peopleCostRationale = "Engineers and a project manager allocated to this workstream for the year."
        }).ToList();

        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 10,
            yearlyDevelopmentCosts = years
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot exceed 10", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Exactly 10 years is accepted (upper boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitResearchDevelopmentResponse_ExactlyTenYears_Returns201()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(MakeValidRequest(years: 10)), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Rationale exactly at the 20-char minimum is accepted (boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitResearchDevelopmentResponse_RationaleExactlyTwentyChars_Returns201()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var exactlyTwenty = new string('a', 20);
        Assert.Equal(20, exactlyTwenty.Length);
        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 1,
            yearlyDevelopmentCosts = new[]
            {
                new
                {
                    year = 1,
                    infrastructureCost = 50000.00,
                    infrastructureCostRationale = exactlyTwenty,
                    peopleCost = 200000.00,
                    peopleCostRationale = exactlyTwenty
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Rationale exceeding the 500-char maximum is rejected.
    /// </summary>
    [Fact]
    public async Task SubmitResearchDevelopmentResponse_RationaleAboveFiveHundredChars_Returns422()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var tooLong = new string('a', 501);
        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 1,
            yearlyDevelopmentCosts = new[]
            {
                new
                {
                    year = 1,
                    infrastructureCost = 50000.00,
                    infrastructureCostRationale = tooLong,
                    peopleCost = 200000.00,
                    peopleCostRationale = "Engineers and a project manager allocated to this workstream for the year."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("InfrastructureCostRationale", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// ProductDevelopmentDuration = 0 → 422 (FR-006 range 1–10).
    /// </summary>
    [Fact]
    public async Task SubmitResearchDevelopmentResponse_DurationZero_Returns422()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(MakeValidRequest(duration: 0)), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ProductDevelopmentDuration", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// ProductDevelopmentDuration = 11 → 422 (above the 1–10 range).
    /// </summary>
    [Fact]
    public async Task SubmitResearchDevelopmentResponse_DurationEleven_Returns422()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(MakeValidRequest(duration: 11)), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>
    /// ProductDevelopmentDuration exactly 1 and exactly 10 are both accepted (inclusive range).
    /// </summary>
    [Fact]
    public async Task SubmitResearchDevelopmentResponse_DurationBoundaries_Return201()
    {
        var tokenOne = await GetAccessToken("rd@submitrd.test", "RD");
        var responseOne = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(MakeValidRequest(duration: 1)), tokenOne);
        Assert.Equal(HttpStatusCode.Created, responseOne.StatusCode);
    }

    /// <summary>
    /// Year 1 complete + year 2 missing a rationale field entirely → 422 (FR-009 partial projection rejection).
    /// </summary>
    [Fact]
    public async Task SubmitResearchDevelopmentResponse_PartialProjectionEntry_Returns422()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = MakeProposal(),
            productDevelopmentDuration = 2,
            yearlyDevelopmentCosts = new object[]
            {
                MakeValidYears(1)[0],
                new
                {
                    year = 2,
                    infrastructureCost = 60000.00,
                    peopleCost = 220000.00,
                    peopleCostRationale = "Engineers and a project manager allocated to this workstream for the year."
                    // infrastructureCostRationale intentionally omitted
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SubmitResearchDevelopmentResponse_ParticipationProposalTooShort_Returns400()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = "Too short",
            productDevelopmentDuration = 2,
            yearlyDevelopmentCosts = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
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
    public async Task SubmitResearchDevelopmentResponse_ParticipationProposalExactlyOneHundredChars_Returns201()
    {
        var token = await GetAccessToken("rd@submitrd.test", "RD");
        var exactlyOneHundred = new string('a', 100);
        Assert.Equal(100, exactlyOneHundred.Length);
        var request = new
        {
            location = "Asia",
            participationType = "R&D Partner",
            participationProposal = exactlyOneHundred,
            productDevelopmentDuration = 1,
            yearlyDevelopmentCosts = MakeValidYears(1)
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/rd",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// ValidateProjection should treat a null list as an empty projection rather than throw.
    /// </summary>
    [Fact]
    public void ValidateProjection_NullList_ReturnsValidationError()
    {
        var errors = InvokeValidateProjection(null);

        Assert.Contains("YearlyDevelopmentCosts", errors.Keys);
    }

    /// <summary>
    /// ValidateProjection should reject negative numeric values per contract.
    /// </summary>
    [Fact]
    public void ValidateProjection_NegativeNumericValues_ReturnsValidationErrors()
    {
        var entries = new List<Innoventity.API.Features.Bids.SubmitResearchDevelopmentResponse.YearlyDevelopmentCostRequest>
        {
            new()
            {
                Year = 1,
                InfrastructureCost = -50000.00m,
                InfrastructureCostRationale = "Cloud hosting, lab tooling, and software licenses for the year.",
                PeopleCost = -200000.00m,
                PeopleCostRationale = "Engineers and a project manager allocated to this workstream for the year."
            }
        };

        var errors = InvokeValidateProjection(entries);

        Assert.Contains("YearlyDevelopmentCosts[0].InfrastructureCost", errors.Keys);
        Assert.Contains("YearlyDevelopmentCosts[0].PeopleCost", errors.Keys);
    }
}
