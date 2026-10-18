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
/// Integration tests for POST /innovations/{innovationId}/bids/investor (Spec 005 US4).
/// </summary>
public class SubmitInvestorResponseTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    private readonly Guid _ownerId = new Guid("a7000001-0000-0000-0000-000000000000");
    private readonly Guid _investorActorId = new Guid("a7000002-0000-0000-0000-000000000000");
    private readonly Guid _mfgActorId = new Guid("a7000003-0000-0000-0000-000000000000");
    private readonly Guid _existingResponderId = new Guid("a7000004-0000-0000-0000-000000000000");

    private readonly Guid _publishedId = new Guid("a8000001-0000-0000-0000-000000000000");
    private readonly Guid _withExistingResponseId = new Guid("a8000002-0000-0000-0000-000000000000");
    private readonly Guid _ownedByInvestorId = new Guid("a8000003-0000-0000-0000-000000000000");

    public SubmitInvestorResponseTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"TestDb_SubmitInvestor_{Guid.NewGuid()}";

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

        var owner = MakeActor(_ownerId, "owner@submitinvestor.test", ActorType.IdeaGenerator);
        var investorActor = MakeActor(_investorActorId, "investor@submitinvestor.test", ActorType.Investor);
        var mfgActor = MakeActor(_mfgActorId, "mfg@submitinvestor.test", ActorType.Manufacturing);
        var existingResponder = MakeActor(_existingResponderId, "existing@submitinvestor.test", ActorType.Investor);

        db.Actors.AddRange(owner, investorActor, mfgActor, existingResponder);

        Innovation MakeInnovation(Guid id, InnovationStatus status, Guid? ownerId = null) => new(id)
        {
            OwnerId = ownerId ?? _ownerId,
            IdeaToken = Guid.NewGuid(),
            IdeaSummary = new IdeaSummary
            {
                Title = $"Test Innovation {id:N}",
                ProductType = "Technology",
                ResearchBackground = "Background for test innovation covering the research area and motivation.",
                ResearchCategory = ResearchCategory.Engineering,
                IprStatus = "Patent Pending"
            },
            Product = new Product
            {
                ProductDescription = "Product description for testing investor response submission.",
                TechnologyDescription = "Technology description providing technical details for the innovation.",
                TargetBeneficiaries = "Manufacturers, distributors, and end consumers of the product.",
                ProductAdvantages = "Cost reduction, quality improvement, and faster time-to-market.",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Lab validation with pilot production runs planned.",
                ProductKeywords = "technology, innovation, testing",
                AdvantageKeywords = "efficiency, quality, scale"
            },
            Market = new Market
            {
                TargetMarket = "Global technology markets.",
                TargetCustomerBase = "Enterprise customers in manufacturing and distribution.",
                TargetCustomerType = "B2B"
            },
            Status = status,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var published = MakeInnovation(_publishedId, InnovationStatus.Published);
        var withExisting = MakeInnovation(_withExistingResponseId, InnovationStatus.Published);
        var ownedByInvestor = MakeInnovation(_ownedByInvestorId, InnovationStatus.Published, ownerId: _investorActorId);

        db.Innovations.AddRange(published, withExisting, ownedByInvestor);

        db.FormalResponses.Add(new InvestorResponse(Guid.NewGuid())
        {
            InnovationId = _withExistingResponseId,
            ActorId = _existingResponderId,
            Location = GeographicRegion.Europe,
            ParticipationType = "Investment Partner",
            ParticipationProposal = MakeProposal(),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Feedback = "Strong technical differentiation and a credible go-to-market plan for this innovation."
        });

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static string MakeProposal() =>
        "Detailed investment partnership proposal covering our fund's thesis, check size range, board " +
        "involvement approach, and portfolio support model for this collaboration opportunity going forward.";

    private object MakeValidRequest() => new
    {
        location = "Europe",
        participationType = "Investment Partner",
        participationProposal = MakeProposal(),
        feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a round."
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
    public async Task SubmitInvestorResponse_HappyPath_Returns201WithInvestorType()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("InvestorResponse", body.GetProperty("responseType").GetString());
        Assert.Equal("Pending", body.GetProperty("status").GetString());
        Assert.Equal(_publishedId, body.GetProperty("innovationId").GetGuid());
        Assert.NotEqual(Guid.Empty, body.GetProperty("responseId").GetGuid());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.FormalResponses.OfType<InvestorResponse>()
            .FirstOrDefaultAsync(r => r.Id == body.GetProperty("responseId").GetGuid());
        Assert.NotNull(stored);
        Assert.Equal(GeographicRegion.Europe, stored.Location);
        Assert.Contains("go-to-market", stored.Feedback);
    }

    [Fact]
    public async Task SubmitInvestorResponse_WrongActorType_Returns403()
    {
        var token = await GetAccessToken("mfg@submitinvestor.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Forbidden", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Investor actors", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitInvestorResponse_ActorOwnsInnovation_Returns403()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_ownedByInvestorId}/bids/investor",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("own innovation", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitInvestorResponse_InnovationNotFound_Returns404()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{Guid.NewGuid()}/bids/investor",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitInvestorResponse_Duplicate_Returns409()
    {
        var token = await GetAccessToken("existing@submitinvestor.test", "Investor");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_withExistingResponseId}/bids/investor",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Duplicate", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already submitted", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitInvestorResponse_InvalidLocation_Returns400()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var request = new
        {
            location = "Not-A-Region",
            participationType = "Investment Partner",
            participationProposal = MakeProposal(),
            feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a round."
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Location", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitInvestorResponse_BlankParticipationType_Returns400()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var request = new
        {
            location = "Europe",
            participationType = string.Empty,
            participationProposal = MakeProposal(),
            feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a round."
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationType", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitInvestorResponse_OverlongParticipationType_Returns400()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var request = new
        {
            location = "Europe",
            participationType = new string('I', 101),
            participationProposal = MakeProposal(),
            feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a round."
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationType", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitInvestorResponse_FeedbackTooShort_Returns422()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var request = new
        {
            location = "Europe",
            participationType = "Investment Partner",
            participationProposal = MakeProposal(),
            feedback = "Too short"
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Feedback", body, StringComparison.Ordinal);
        Assert.Contains("50", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Feedback exactly at the 50-char minimum is accepted (boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitInvestorResponse_FeedbackExactlyFiftyChars_Returns201()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var exactlyFifty = new string('a', 50);
        Assert.Equal(50, exactlyFifty.Length);
        var request = new
        {
            location = "Europe",
            participationType = "Investment Partner",
            participationProposal = MakeProposal(),
            feedback = exactlyFifty
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Feedback one character under the 50-char minimum is rejected.
    /// </summary>
    [Fact]
    public async Task SubmitInvestorResponse_FeedbackFortyNineChars_Returns422()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var fortyNine = new string('a', 49);
        var request = new
        {
            location = "Europe",
            participationType = "Investment Partner",
            participationProposal = MakeProposal(),
            feedback = fortyNine
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>
    /// participationProposal is required for all 4 types (confirmed in clarification session 2026-06-07).
    /// </summary>
    [Fact]
    public async Task SubmitInvestorResponse_ParticipationProposalTooShort_Returns400()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var request = new
        {
            location = "Europe",
            participationType = "Investment Partner",
            participationProposal = "Too short",
            feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a round."
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
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
    public async Task SubmitInvestorResponse_ParticipationProposalExactlyOneHundredChars_Returns201()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var exactlyOneHundred = new string('a', 100);
        Assert.Equal(100, exactlyOneHundred.Length);
        var request = new
        {
            location = "Europe",
            participationType = "Investment Partner",
            participationProposal = exactlyOneHundred,
            feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a round."
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// participationProposal over the 5000-char maximum is rejected.
    /// </summary>
    [Fact]
    public async Task SubmitInvestorResponse_ParticipationProposalOverFiveThousandChars_Returns400()
    {
        var token = await GetAccessToken("investor@submitinvestor.test", "Investor");
        var tooLong = new string('a', 5001);
        var request = new
        {
            location = "Europe",
            participationType = "Investment Partner",
            participationProposal = tooLong,
            feedback = "Strong technical differentiation and a credible go-to-market plan; interested in leading a round."
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/investor",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationProposal", body, StringComparison.Ordinal);
        Assert.Contains("5000", body, StringComparison.Ordinal);
    }
}
