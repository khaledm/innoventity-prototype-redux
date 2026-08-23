using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Features.Bids;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Tests.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Innoventity.API.Tests.Integration.Features.Bids;

/// <summary>
/// Integration tests for POST /innovations/{innovationId}/select-partners
/// Covers all 8 verification scenarios from specs/004-partner-selection/spec.md, updated for
/// Spec 005's FormalResponse hierarchy (typed seed data, SelectedResponseIds field rename).
/// </summary>
public class SelectPartnersTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    // Actors
    private readonly Guid _ownerId = new Guid("aa000001-0000-0000-0000-000000000000");
    private readonly Guid _mfgActorId = new Guid("aa000002-0000-0000-0000-000000000000");
    private readonly Guid _salesActorId = new Guid("aa000003-0000-0000-0000-000000000000");
    private readonly Guid _rdActorId = new Guid("aa000004-0000-0000-0000-000000000000");
    private readonly Guid _mfg2ActorId = new Guid("aa000005-0000-0000-0000-000000000000");
    private readonly Guid _investorActorId = new Guid("aa000006-0000-0000-0000-000000000000");

    // Innovations
    private readonly Guid _publishedId = new Guid("bb000001-0000-0000-0000-000000000000"); // all 5 responses — validation tests
    private readonly Guid _target1Id = new Guid("bb000002-0000-0000-0000-000000000000"); // mfg+sales+rd responses — success test 3
    private readonly Guid _target2Id = new Guid("bb000003-0000-0000-0000-000000000000"); // mfg+sales+rd responses — success test 7
    private readonly Guid _insufficientId = new Guid("bb000004-0000-0000-0000-000000000000"); // only mfg response — readiness fail
    private readonly Guid _completedId = new Guid("bb000005-0000-0000-0000-000000000000"); // already PartnersSelected
    private readonly Guid _draftId = new Guid("bb000006-0000-0000-0000-000000000000"); // Draft status

    // Responses on _publishedId (for non-mutating validation tests)
    private readonly Guid _pMfgResponseId = new Guid("cc000001-0000-0000-0000-000000000000");
    private readonly Guid _pSalesResponseId = new Guid("cc000002-0000-0000-0000-000000000000");
    private readonly Guid _pRdResponseId = new Guid("cc000003-0000-0000-0000-000000000000");
    private readonly Guid _pMfg2ResponseId = new Guid("cc000004-0000-0000-0000-000000000000");
    private readonly Guid _pInvestorResponseId = new Guid("cc000005-0000-0000-0000-000000000000");

    // Responses on _target1Id (for success test 3)
    private readonly Guid _t1MfgResponseId = new Guid("cc000011-0000-0000-0000-000000000000");
    private readonly Guid _t1SalesResponseId = new Guid("cc000012-0000-0000-0000-000000000000");
    private readonly Guid _t1RdResponseId = new Guid("cc000013-0000-0000-0000-000000000000");

    // Responses on _target2Id (for success test 7)
    private readonly Guid _t2MfgResponseId = new Guid("cc000021-0000-0000-0000-000000000000");
    private readonly Guid _t2SalesResponseId = new Guid("cc000022-0000-0000-0000-000000000000");
    private readonly Guid _t2RdResponseId = new Guid("cc000023-0000-0000-0000-000000000000");

    // Response on _insufficientId (only manufacturing)
    private readonly Guid _insuffMfgResponseId = new Guid("cc000031-0000-0000-0000-000000000000");

    // Responses on _completedId (accepted + rejected — for AC3 response-state preservation)
    private readonly Guid _cMfgResponseId = new Guid("cc000041-0000-0000-0000-000000000000");
    private readonly Guid _cSalesResponseId = new Guid("cc000042-0000-0000-0000-000000000000");
    private readonly Guid _cRdResponseId = new Guid("cc000043-0000-0000-0000-000000000000");
    private readonly Guid _cRejectedResponseId = new Guid("cc000044-0000-0000-0000-000000000000");

    public SelectPartnersTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"TestDb_SelectPartners_{Guid.NewGuid()}";

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
                        options.UseInMemoryDatabase(databaseName));
                });
            });
    }

    private static string MakeProposal() =>
        "Detailed proposal covering capabilities, production capacity, quality standards, timeline commitments, " +
        "and strategic partnership vision for this collaboration opportunity at full scale.";

    private static IList<YearlyManufacturingCost> MakeManufacturingCosts() =>
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
    ];

    private static IList<YearlySale> MakeYearlySales() =>
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
    ];

    private static IList<YearlyDevelopmentCost> MakeDevelopmentCosts() =>
    [
        new YearlyDevelopmentCost
        {
            Year = 1,
            InfrastructureCost = 50000.00m,
            InfrastructureCostRationale = "Cloud hosting, lab tooling, and software licenses for year one.",
            PeopleCost = 200000.00m,
            PeopleCostRationale = "Three engineers and one project manager allocated to this workstream."
        }
    ];

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // --- Actors ---
        var owner = new Actor(_ownerId)
        {
            Email = "owner@select.test",
            FirstName = "Sarah", LastName = "Chen",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };
        var mfgActor = new Actor(_mfgActorId)
        {
            Email = "mfg@select.test",
            FirstName = "Hans", LastName = "Mueller",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };
        var salesActor = new Actor(_salesActorId)
        {
            Email = "sales@select.test",
            FirstName = "Nina", LastName = "Park",
            ActorType = ActorType.SalesMarketing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };
        var rdActor = new Actor(_rdActorId)
        {
            Email = "rd@select.test",
            FirstName = "Emily", LastName = "Watson",
            ActorType = ActorType.RD,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };
        var mfg2Actor = new Actor(_mfg2ActorId)
        {
            Email = "mfg2@select.test",
            FirstName = "Karl", LastName = "Benz",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };
        var investorActor = new Actor(_investorActorId)
        {
            Email = "investor@select.test",
            FirstName = "John", LastName = "Smith",
            ActorType = ActorType.Investor,
            AccountStatus = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            PasswordSalt = Convert.ToBase64String(new byte[32])
        };
        db.Actors.AddRange(owner, mfgActor, salesActor, rdActor, mfg2Actor, investorActor);

        // --- Common innovation fields ---
        Innovation MakeInnovation(Guid id, InnovationStatus status, DateTimeOffset? partnerSelectionCompletedOn = null) =>
            new Innovation(id)
            {
                OwnerId = _ownerId,
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
                    ProductDescription = "Product description for testing partner selection workflow.",
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
                CollaborationRequirement = new CollaborationRequirement
                {
                    PartnersNeeded = "Manufacturing,SalesMarketing,RD"
                },
                Status = status,
                SubmittedAt = status == InnovationStatus.Draft ? null : DateTimeOffset.UtcNow.AddDays(-5),
                PartnerSelectionCompletedOn = partnerSelectionCompletedOn
            };

        // _publishedId: all 5 responses (for non-mutating validation tests)
        var publishedInnovation = MakeInnovation(_publishedId, InnovationStatus.Published);

        // _target1Id: exactly mfg+sales+rd (for success test 3)
        var target1Innovation = MakeInnovation(_target1Id, InnovationStatus.Published);

        // _target2Id: exactly mfg+sales+rd (for success test 7)
        var target2Innovation = MakeInnovation(_target2Id, InnovationStatus.Published);

        // _insufficientId: only Manufacturing response (for readiness failure test 1)
        var insufficientInnovation = MakeInnovation(_insufficientId, InnovationStatus.Published);

        // _completedId: already PartnersSelected (for immutability test 4)
        var completedInnovation = MakeInnovation(
            _completedId,
            InnovationStatus.PartnersSelected,
            DateTimeOffset.UtcNow.AddDays(-1));

        // _draftId: Draft status (for state check test 8)
        var draftInnovation = MakeInnovation(_draftId, InnovationStatus.Draft);

        db.Innovations.AddRange(
            publishedInnovation, target1Innovation, target2Innovation,
            insufficientInnovation, completedInnovation, draftInnovation);

        // --- Responses on _publishedId ---
        db.FormalResponses.AddRange(
            new ManufacturingResponse(_pMfgResponseId)
            {
                InnovationId = _publishedId, ActorId = _mfgActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Manufacturing Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                YearlyManufacturingCosts = MakeManufacturingCosts()
            },
            new SalesMarketingResponse(_pSalesResponseId)
            {
                InnovationId = _publishedId, ActorId = _salesActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Sales Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                YearlySales = MakeYearlySales()
            },
            new ResearchDevelopmentResponse(_pRdResponseId)
            {
                InnovationId = _publishedId, ActorId = _rdActorId,
                Location = GeographicRegion.Americas, ParticipationType = "R&D Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                ProductDevelopmentDuration = 2, YearlyDevelopmentCosts = MakeDevelopmentCosts()
            },
            new ManufacturingResponse(_pMfg2ResponseId)
            {
                InnovationId = _publishedId, ActorId = _mfg2ActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Manufacturing Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2),
                YearlyManufacturingCosts = MakeManufacturingCosts()
            },
            new InvestorResponse(_pInvestorResponseId)
            {
                InnovationId = _publishedId, ActorId = _investorActorId,
                Location = GeographicRegion.Americas, ParticipationType = "Investment Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2),
                Feedback = "Strong technical differentiation and a credible go-to-market plan for this innovation."
            }
        );

        // --- Responses on _target1Id (success test 3) ---
        db.FormalResponses.AddRange(
            new ManufacturingResponse(_t1MfgResponseId)
            {
                InnovationId = _target1Id, ActorId = _mfgActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Manufacturing Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                YearlyManufacturingCosts = MakeManufacturingCosts()
            },
            new SalesMarketingResponse(_t1SalesResponseId)
            {
                InnovationId = _target1Id, ActorId = _salesActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Sales Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                YearlySales = MakeYearlySales()
            },
            new ResearchDevelopmentResponse(_t1RdResponseId)
            {
                InnovationId = _target1Id, ActorId = _rdActorId,
                Location = GeographicRegion.Americas, ParticipationType = "R&D Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                ProductDevelopmentDuration = 2, YearlyDevelopmentCosts = MakeDevelopmentCosts()
            }
        );

        // --- Responses on _target2Id (success test 7) ---
        db.FormalResponses.AddRange(
            new ManufacturingResponse(_t2MfgResponseId)
            {
                InnovationId = _target2Id, ActorId = _mfgActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Manufacturing Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                YearlyManufacturingCosts = MakeManufacturingCosts()
            },
            new SalesMarketingResponse(_t2SalesResponseId)
            {
                InnovationId = _target2Id, ActorId = _salesActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Sales Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                YearlySales = MakeYearlySales()
            },
            new ResearchDevelopmentResponse(_t2RdResponseId)
            {
                InnovationId = _target2Id, ActorId = _rdActorId,
                Location = GeographicRegion.Americas, ParticipationType = "R&D Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
                ProductDevelopmentDuration = 2, YearlyDevelopmentCosts = MakeDevelopmentCosts()
            }
        );

        // --- Response on _insufficientId (only Manufacturing) ---
        db.FormalResponses.Add(new ManufacturingResponse(_insuffMfgResponseId)
        {
            InnovationId = _insufficientId, ActorId = _mfgActorId,
            Location = GeographicRegion.Europe, ParticipationType = "Manufacturing Partner",
            ParticipationProposal = MakeProposal(), Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3),
            YearlyManufacturingCosts = MakeManufacturingCosts()
        });

        // --- Responses on _completedId (accepted + rejected, for AC3 response-state preservation) ---
        db.FormalResponses.AddRange(
            new ManufacturingResponse(_cMfgResponseId)
            {
                InnovationId = _completedId, ActorId = _mfgActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Manufacturing Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Accepted,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5),
                AcceptedAt = DateTimeOffset.UtcNow.AddDays(-1),
                YearlyManufacturingCosts = MakeManufacturingCosts()
            },
            new SalesMarketingResponse(_cSalesResponseId)
            {
                InnovationId = _completedId, ActorId = _salesActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Sales Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Accepted,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5),
                AcceptedAt = DateTimeOffset.UtcNow.AddDays(-1),
                YearlySales = MakeYearlySales()
            },
            new ResearchDevelopmentResponse(_cRdResponseId)
            {
                InnovationId = _completedId, ActorId = _rdActorId,
                Location = GeographicRegion.Americas, ParticipationType = "R&D Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Accepted,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5),
                AcceptedAt = DateTimeOffset.UtcNow.AddDays(-1),
                ProductDevelopmentDuration = 2, YearlyDevelopmentCosts = MakeDevelopmentCosts()
            },
            new ManufacturingResponse(_cRejectedResponseId)
            {
                InnovationId = _completedId, ActorId = _mfg2ActorId,
                Location = GeographicRegion.Europe, ParticipationType = "Manufacturing Partner",
                ParticipationProposal = MakeProposal(), Status = ResponseStatus.Rejected,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-5),
                YearlyManufacturingCosts = MakeManufacturingCosts()
            }
        );

        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // --- Helpers ---

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

    private static JsonContent MakeRequest(params Guid[] responseIds)
    {
        var payload = new SelectPartners.SelectPartnersRequest { SelectedResponseIds = responseIds };
        return JsonContent.Create(payload, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string CreateRawJwtToken(string sub)
    {
        const string signingKey = "DEV-ONLY-KEY-REPLACE-IN-PRODUCTION-VIA-CONFIGURATION-MINIMUM-32-CHARACTERS";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "Innoventity",
            audience: "Innoventity.API",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, sub)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ==========================================
    // Verification Scenario 1 (FR-009, FR-015)
    // ==========================================

    /// <summary>
    /// Spec Scenario 1: Insufficient readiness threshold → 409, no mutations
    /// Innovation has only a Manufacturing response; SalesMarketing and RD are missing.
    /// </summary>
    [Fact]
    public async Task SelectPartners_InsufficientReadiness_Returns409()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");

        // Payload doesn't matter — readiness check fires before payload validation
        var content = MakeRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_insufficientId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Insufficient Readiness", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("threshold", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SalesMarketing, RD", body, StringComparison.OrdinalIgnoreCase);

        // Verify: innovation status unchanged
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_insufficientId);
        Assert.NotNull(innovation);
        Assert.Equal(InnovationStatus.Published, innovation.Status);
        Assert.Null(innovation.PartnerSelectionCompletedOn);
    }

    // ==========================================
    // Verification Scenario 2 (FR-010, FR-004)
    // ==========================================

    /// <summary>
    /// Spec Scenario 2: Missing required actor type in payload → 422, no mutations
    /// Payload includes Manufacturing and SalesMarketing but omits RD.
    /// </summary>
    [Fact]
    public async Task SelectPartners_MissingRequiredActorType_Returns422()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");

        // Only 2 responses: Manufacturing + SalesMarketing (missing RD)
        var content = MakeRequest(_pMfgResponseId, _pSalesResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("3 response IDs", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SelectedResponseIds", body, StringComparison.Ordinal);

        // Verify: no mutations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_publishedId);
        Assert.Equal(InnovationStatus.Published, innovation!.Status);
        Assert.Null(innovation.PartnerSelectionCompletedOn);
        var responses = await db.FormalResponses.Where(r => r.InnovationId == _publishedId).ToListAsync();
        Assert.All(responses, r => Assert.Equal(ResponseStatus.Pending, r.Status));
    }

    // ==========================================
    // Verification Scenario 3 (FR-006, FR-012)
    // ==========================================

    /// <summary>
    /// Spec Scenario 3: Successful structured selection → 200 + accepted/rejected transitions + innovation PartnersSelected
    /// </summary>
    [Fact]
    public async Task SelectPartners_ValidSelection_Returns200AndTransitions()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");
        var content = MakeRequest(_t1MfgResponseId, _t1SalesResponseId, _t1RdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_target1Id}/select-partners", content, token);

        // Assert — response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SelectPartners.SelectPartnersResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(_target1Id, result.InnovationId);
        Assert.Equal("PartnersSelected", result.Status);
        Assert.NotEqual(default, result.PartnerSelectionCompletedOn);
        Assert.Equal(3, result.AcceptedBids.Count);

        var acceptedResponseIds = result.AcceptedBids.Select(b => b.BidId).ToHashSet();
        Assert.Contains(_t1MfgResponseId, acceptedResponseIds);
        Assert.Contains(_t1SalesResponseId, acceptedResponseIds);
        Assert.Contains(_t1RdResponseId, acceptedResponseIds);

        var acceptedActorTypes = result.AcceptedBids.Select(b => b.ActorType).ToHashSet();
        Assert.Contains("Manufacturing", acceptedActorTypes);
        Assert.Contains("SalesMarketing", acceptedActorTypes);
        Assert.Contains("RD", acceptedActorTypes);

        // Assert — database state
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var innovation = await db.Innovations.FindAsync(_target1Id);
        Assert.NotNull(innovation);
        Assert.Equal(InnovationStatus.PartnersSelected, innovation.Status);
        Assert.NotNull(innovation.PartnerSelectionCompletedOn);
        Assert.Equal(_ownerId, innovation.SelectedByActorId); // NFR-003: selector actor recorded

        var mfgResponse = await db.FormalResponses.FindAsync(_t1MfgResponseId);
        var salesResponse = await db.FormalResponses.FindAsync(_t1SalesResponseId);
        var rdResponse = await db.FormalResponses.FindAsync(_t1RdResponseId);

        Assert.Equal(ResponseStatus.Accepted, mfgResponse!.Status);
        Assert.NotNull(mfgResponse.AcceptedAt);
        Assert.Equal(ResponseStatus.Accepted, salesResponse!.Status);
        Assert.NotNull(salesResponse.AcceptedAt);
        Assert.Equal(ResponseStatus.Accepted, rdResponse!.Status);
        Assert.NotNull(rdResponse.AcceptedAt);
    }

    // ==========================================
    // Verification Scenario 4 (FR-007, SelectPartner-4)
    // ==========================================

    /// <summary>
    /// Spec Scenario 4: Repeated selection attempt after commitment → 403 "partner selection is final", no further mutations
    /// </summary>
    [Fact]
    public async Task SelectPartners_AlreadyCompleted_Returns403WithImmutableMessage()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");
        var content = MakeRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_completedId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("partner selection is final", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("immutable", body, StringComparison.OrdinalIgnoreCase);

        // Verify: innovation status unchanged
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_completedId);
        Assert.NotNull(innovation);
        Assert.Equal(InnovationStatus.PartnersSelected, innovation.Status);
        Assert.NotNull(innovation.PartnerSelectionCompletedOn);

        // Verify: existing accepted/rejected response outcomes are preserved (US SelectPartner-4 AC3)
        var cMfgResponse = await db.FormalResponses.FindAsync(_cMfgResponseId);
        Assert.Equal(ResponseStatus.Accepted, cMfgResponse!.Status);
        Assert.NotNull(cMfgResponse.AcceptedAt);

        var cSalesResponse = await db.FormalResponses.FindAsync(_cSalesResponseId);
        Assert.Equal(ResponseStatus.Accepted, cSalesResponse!.Status);

        var cRdResponse = await db.FormalResponses.FindAsync(_cRdResponseId);
        Assert.Equal(ResponseStatus.Accepted, cRdResponse!.Status);

        var cRejectedResponse = await db.FormalResponses.FindAsync(_cRejectedResponseId);
        Assert.Equal(ResponseStatus.Rejected, cRejectedResponse!.Status);
    }

    // ==========================================
    // Verification Scenario 5 (FR-004, SelectPartner-3 AC2)
    // ==========================================

    /// <summary>
    /// Spec Scenario 5: Duplicate selected responses from the same required actor type → 422, no mutations
    /// Both _pMfgResponseId and _pMfg2ResponseId are Manufacturing type.
    /// </summary>
    [Fact]
    public async Task SelectPartners_DuplicateActorType_Returns422()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");

        // Two manufacturing responses + one sales (duplicate Manufacturing type)
        var content = MakeRequest(_pMfgResponseId, _pMfg2ResponseId, _pSalesResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("duplicate actor types", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SelectedResponseIds", body, StringComparison.Ordinal);

        // Verify: no mutations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_publishedId);
        Assert.Equal(InnovationStatus.Published, innovation!.Status);
        Assert.Null(innovation.PartnerSelectionCompletedOn);
    }

    // ==========================================
    // Verification Scenario 6 (FR-014, SelectPartner-3)
    // ==========================================

    /// <summary>
    /// Spec Scenario 6: Selection payload includes unsupported actor type → 422, no mutations
    /// _pInvestorResponseId is from an Investor actor, which is not a required type in Phase 1c.
    /// </summary>
    [Fact]
    public async Task SelectPartners_UnsupportedActorType_Returns422()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");

        // Manufacturing + SalesMarketing + Investor (Investor is not a required type)
        var content = MakeRequest(_pMfgResponseId, _pSalesResponseId, _pInvestorResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("not required for partner selection", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SelectedResponseIds", body, StringComparison.Ordinal);

        // Verify: no mutations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_publishedId);
        Assert.Equal(InnovationStatus.Published, innovation!.Status);
        Assert.Null(innovation.PartnerSelectionCompletedOn);
    }

    // ==========================================
    // Verification Scenario 7 (FR-005, FR-015)
    // ==========================================

    /// <summary>
    /// Spec Scenario 7: Exactly one eligible response per required type → readiness passes and selection succeeds
    /// _target2Id has exactly one Pending response per type.
    /// </summary>
    [Fact]
    public async Task SelectPartners_ExactlyOneEligiblePerType_ReadinessPassesAndSelectionSucceeds()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");
        var content = MakeRequest(_t2MfgResponseId, _t2SalesResponseId, _t2RdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_target2Id}/select-partners", content, token);

        // Assert — readiness check passed, selection succeeded
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_target2Id);
        Assert.NotNull(innovation);
        Assert.Equal(InnovationStatus.PartnersSelected, innovation.Status);
        Assert.NotNull(innovation.PartnerSelectionCompletedOn);
    }

    // ==========================================
    // Verification Scenario 8 (FR-016)
    // ==========================================

    /// <summary>
    /// Spec Scenario 8: Innovation state is not Published → selection rejected, no mutations
    /// </summary>
    [Fact]
    public async Task SelectPartners_InnovationNotPublished_Returns409()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");
        var content = MakeRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_draftId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Conflict", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Published status", body, StringComparison.OrdinalIgnoreCase);

        // Verify: innovation status unchanged
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_draftId);
        Assert.NotNull(innovation);
        Assert.Equal(InnovationStatus.Draft, innovation.Status);
        Assert.Null(innovation.PartnerSelectionCompletedOn);
    }

    // ==========================================
    // Additional guard tests
    // ==========================================

    /// <summary>
    /// Non-owner caller returns 403 Forbidden (FR-011)
    /// </summary>
    [Fact]
    public async Task SelectPartners_NonOwner_Returns403()
    {
        // Arrange — manufacturing actor tries to select partners (not the owner)
        var token = await GetAccessToken("mfg@select.test", "Manufacturing");
        var content = MakeRequest(_t1MfgResponseId, _t1SalesResponseId, _t1RdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_target1Id}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Forbidden", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("owner", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Response IDs that belong to a different innovation return 422 (SelectPartner-3 AC3)
    /// </summary>
    [Fact]
    public async Task SelectPartners_ResponsesFromDifferentInnovation_Returns422()
    {
        // Arrange — use _target1 responses but send request to _target2 innovation
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");
        var content = MakeRequest(_t1MfgResponseId, _t1SalesResponseId, _t1RdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_target2Id}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("do not belong to this innovation", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SelectedResponseIds", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Duplicate response IDs in payload → 422 (Step 9 guard)
    /// Sending [mfg, mfg, sales] — same ID twice — is structurally invalid.
    /// </summary>
    [Fact]
    public async Task SelectPartners_DuplicateResponseIds_Returns422()
    {
        // Arrange
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");

        // Same _pMfgResponseId appears twice
        var content = MakeRequest(_pMfgResponseId, _pMfgResponseId, _pSalesResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Duplicate response IDs", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SelectedResponseIds", body, StringComparison.Ordinal);

        // Verify: no mutations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innovation = await db.Innovations.FindAsync(_publishedId);
        Assert.Equal(InnovationStatus.Published, innovation!.Status);
        Assert.Null(innovation.PartnerSelectionCompletedOn);
        var responses = await db.FormalResponses.Where(r => r.InnovationId == _publishedId).ToListAsync();
        Assert.All(responses, r => Assert.Equal(ResponseStatus.Pending, r.Status));
    }

    /// <summary>
    /// Unauthenticated request (no Bearer token) returns 401 Unauthorized (spec error table, FR step 1-2).
    /// </summary>
    [Fact]
    public async Task SelectPartners_Unauthenticated_Returns401()
    {
        // Arrange — no Authorization header attached
        var content = MakeRequest(_t1MfgResponseId, _t1SalesResponseId, _t1RdResponseId);

        // Act
        var response = await _client.PostAsync(
            $"/innovations/{_target1Id}/select-partners", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Non-existent innovationId returns 404 Not Found with RFC7807 Problem Details (spec error table, Step 3).
    /// </summary>
    [Fact]
    public async Task SelectPartners_InnovationNotFound_Returns404()
    {
        // Arrange — authenticated owner, but innovation does not exist in the database
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");
        var nonExistentId = new Guid("ee000001-0000-0000-0000-000000000000");
        var content = MakeRequest(_t1MfgResponseId, _t1SalesResponseId, _t1RdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{nonExistentId}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Not Found", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nonExistentId.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// JWT with a valid signature but a non-Guid 'sub' claim returns 401 Problem Details.
    /// Claim parsing fails in ActorResolutionFilter before the handler is reached.
    /// </summary>
    [Fact]
    public async Task SelectPartners_InvalidSubClaim_Returns401ProblemDetails()
    {
        // Arrange — token is cryptographically valid but sub is not a Guid
        var token = CreateRawJwtToken("not-a-guid");
        var content = MakeRequest(_t1MfgResponseId, _t1SalesResponseId, _t1RdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_target1Id}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unauthorized", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("resolved", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// JWT with a valid Guid 'sub' that does not match any Actor row returns 401 Problem Details.
    /// DB lookup returns null in ActorResolutionFilter before the handler is reached.
    /// </summary>
    [Fact]
    public async Task SelectPartners_UnknownActor_Returns401ProblemDetails()
    {
        // Arrange — token sub is a well-formed Guid but no Actor with that ID exists in the DB
        var token = CreateRawJwtToken(new Guid("ff000001-0000-0000-0000-000000000000").ToString());
        var content = MakeRequest(_t1MfgResponseId, _t1SalesResponseId, _t1RdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_target1Id}/select-partners", content, token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unauthorized", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("resolved", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Successful selection on an innovation with extra Pending responses proves those responses
    /// are Rejected after commit — exercises the else-if(Pending) branch in the mutation loop.
    /// </summary>
    [Fact]
    public async Task SelectPartners_NonSelectedPendingResponsesAreRejected()
    {
        // Arrange — _publishedId has 5 Pending responses; we select only the 3 required types
        var token = await GetAccessToken("owner@select.test", "IdeaGenerator");
        var content = MakeRequest(_pMfgResponseId, _pSalesResponseId, _pRdResponseId);

        // Act
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/select-partners", content, token);

        // Assert — selection succeeds
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert — non-selected Pending responses (_pMfg2ResponseId, _pInvestorResponseId) are Rejected
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var mfg2Response = await db.FormalResponses.FindAsync(_pMfg2ResponseId);
        Assert.Equal(ResponseStatus.Rejected, mfg2Response!.Status);

        var investorResponse = await db.FormalResponses.FindAsync(_pInvestorResponseId);
        Assert.Equal(ResponseStatus.Rejected, investorResponse!.Status);

        // Assert — selected responses are Accepted
        var mfgResponse = await db.FormalResponses.FindAsync(_pMfgResponseId);
        Assert.Equal(ResponseStatus.Accepted, mfgResponse!.Status);
        Assert.NotNull(mfgResponse.AcceptedAt);

        var salesResponse = await db.FormalResponses.FindAsync(_pSalesResponseId);
        Assert.Equal(ResponseStatus.Accepted, salesResponse!.Status);

        var rdResponse = await db.FormalResponses.FindAsync(_pRdResponseId);
        Assert.Equal(ResponseStatus.Accepted, rdResponse!.Status);

        // Assert — innovation transitioned
        var innovation = await db.Innovations.FindAsync(_publishedId);
        Assert.Equal(InnovationStatus.PartnersSelected, innovation!.Status);
        Assert.NotNull(innovation.PartnerSelectionCompletedOn);
    }
}
