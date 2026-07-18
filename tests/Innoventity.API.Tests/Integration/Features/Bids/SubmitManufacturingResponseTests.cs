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
/// Integration tests for POST /innovations/{innovationId}/bids/manufacturing (Spec 005 US1).
/// </summary>
public class SubmitManufacturingResponseTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    private readonly Guid _ownerId = new Guid("d1000001-0000-0000-0000-000000000000");
    private readonly Guid _mfgActorId = new Guid("d1000002-0000-0000-0000-000000000000");
    private readonly Guid _salesActorId = new Guid("d1000003-0000-0000-0000-000000000000");
    private readonly Guid _existingResponderId = new Guid("d1000004-0000-0000-0000-000000000000");

    private readonly Guid _publishedId = new Guid("d2000001-0000-0000-0000-000000000000");
    private readonly Guid _draftId = new Guid("d2000002-0000-0000-0000-000000000000");
    private readonly Guid _ownedByOwnerId = new Guid("d2000003-0000-0000-0000-000000000000");
    private readonly Guid _withExistingResponseId = new Guid("d2000004-0000-0000-0000-000000000000");

    public SubmitManufacturingResponseTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"TestDb_SubmitMfg_{Guid.NewGuid()}";

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

        var owner = MakeActor(_ownerId, "owner@submitmfg.test", ActorType.IdeaGenerator);
        var mfgActor = MakeActor(_mfgActorId, "mfg@submitmfg.test", ActorType.Manufacturing);
        var salesActor = MakeActor(_salesActorId, "sales@submitmfg.test", ActorType.SalesMarketing);
        var existingResponder = MakeActor(_existingResponderId, "existing@submitmfg.test", ActorType.Manufacturing);

        db.Actors.AddRange(owner, mfgActor, salesActor, existingResponder);

        Innovation MakeInnovation(Guid id, InnovationStatus status, Guid ownerId) => new(id)
        {
            OwnerId = ownerId,
            IdeaToken = Guid.NewGuid(),
            Title = $"Test Innovation {id:N}",
            ProductType = "Technology",
            ResearchBackground = "Background for test innovation covering the research area and motivation.",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Product description for testing manufacturing response submission.",
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

        var published = MakeInnovation(_publishedId, InnovationStatus.Published, _ownerId);
        var draft = MakeInnovation(_draftId, InnovationStatus.Draft, _ownerId);
        var ownedByOwner = MakeInnovation(_ownedByOwnerId, InnovationStatus.Published, _ownerId);
        var withExisting = MakeInnovation(_withExistingResponseId, InnovationStatus.Published, _ownerId);

        db.Innovations.AddRange(published, draft, ownedByOwner, withExisting);

        db.FormalResponses.Add(new ManufacturingResponse(Guid.NewGuid())
        {
            InnovationId = _withExistingResponseId,
            ActorId = _existingResponderId,
            Location = GeographicRegion.Europe,
            ParticipationType = "Manufacturing Partner",
            ParticipationProposal = MakeProposal(),
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1),
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

    private static string MakeProposal() =>
        "Detailed manufacturing partnership proposal covering production capacity, quality standards, " +
        "certifications, timeline commitments, and strategic partnership vision for full-scale collaboration.";

    private static List<object> MakeValidYears(int count = 3) =>
        Enumerable.Range(1, count).Select(year => (object)new
        {
            year,
            productionVolume = 10000 * year,
            productionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput estimates.",
            unitCost = 5.50,
            unitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
            averageGlobalDistributionExpense = 1.20,
            avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
        }).ToList();

    private static Dictionary<string, string[]> InvokeValidateProjection(
        List<Innoventity.API.Features.Bids.SubmitManufacturingResponse.YearlyManufacturingCostRequest>? entries)
    {
        var method = typeof(Innoventity.API.Features.Bids.SubmitManufacturingResponse)
            .GetMethod("ValidateProjection", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (Dictionary<string, string[]>)method.Invoke(null, new object?[] { entries })!;
    }

    private object MakeValidRequest(int years = 3, string? location = "Europe") => new
    {
        location,
        participationType = "Manufacturing Partner",
        participationProposal = MakeProposal(),
        yearlyManufacturingCosts = MakeValidYears(years)
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

    /// <summary>
    /// (1) Happy path: 3-year projection → 201 + stored discriminator.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_HappyPath_Returns201AndPersists()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("ManufacturingResponse", body.GetProperty("responseType").GetString());
        Assert.Equal("Pending", body.GetProperty("status").GetString());

        var responseId = body.GetProperty("responseId").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.FormalResponses.OfType<ManufacturingResponse>().FirstOrDefaultAsync(r => r.Id == responseId);
        Assert.NotNull(stored);
        Assert.Equal(3, stored.YearlyManufacturingCosts.Count);
        Assert.All(stored.YearlyManufacturingCosts, y => Assert.True(y.ProductionVolumeRationale.Length >= 20));
    }

    /// <summary>
    /// (2) Wrong actor type (SalesMarketing) → 403.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_WrongActorType_Returns403()
    {
        var token = await GetAccessToken("sales@submitmfg.test", "SalesMarketing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Manufacturing actors", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Non-existent innovation → 404 (distinct guard path from "exists but not Published").
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_InnovationNotFound_Returns404()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{Guid.NewGuid()}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Invalid location string → 400.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_InvalidLocation_Returns400()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest(location: "Not-A-Region")), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Location", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitManufacturingResponse_BlankParticipationType_Returns400()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var request = new
        {
            location = "Europe",
            participationType = string.Empty,
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationType", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitManufacturingResponse_OverlongParticipationType_Returns400()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var request = new
        {
            location = "Europe",
            participationType = new string('M', 101),
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = MakeValidYears()
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationType", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// (3) Actor owns the innovation → 403.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_ActorOwnsInnovation_Returns403()
    {
        // owner is IdeaGenerator, not Manufacturing — use a Manufacturing actor who owns a different innovation instead
        // to isolate the "owns innovation" guard, we register the owner as Manufacturing for this one case is not possible
        // since ActorType is immutable; instead verify guard fires for an owner-typed actor attempting on their own innovation
        // is unreachable (they'd fail the actor-type check first). This test instead verifies a Manufacturing actor cannot
        // respond on an innovation they own — requires the innovation to be owned by a Manufacturing actor.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var selfOwned = new Innovation(Guid.NewGuid())
            {
                OwnerId = _mfgActorId,
                IdeaToken = Guid.NewGuid(),
                Title = "Self Owned Innovation",
                ProductType = "Technology",
                ResearchBackground = "Background for self-owned innovation test covering the research area.",
                ResearchCategory = ResearchCategory.Engineering,
                IprStatus = "Patent Pending",
                ProductDescription = "Product description for self-owned innovation test.",
                TechnologyDescription = "Technology description for self-owned innovation test.",
                TargetBeneficiaries = "Manufacturers and distributors.",
                ProductAdvantages = "Cost reduction and quality improvement.",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Lab validation planned.",
                TargetMarket = "Global technology markets.",
                TargetCustomerBase = "Enterprise customers.",
                TargetCustomerType = "B2B",
                ProductKeywords = "technology, testing",
                AdvantageKeywords = "efficiency, scale",
                Status = InnovationStatus.Published,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            db.Innovations.Add(selfOwned);
            await db.SaveChangesAsync();

            var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
            var response = await _client.PostWithAuthAsync(
                $"/innovations/{selfOwned.Id}/bids/manufacturing",
                JsonContent.Create(MakeValidRequest()), token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    /// <summary>
    /// (4) Duplicate response → 409.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_Duplicate_Returns409()
    {
        var token = await GetAccessToken("existing@submitmfg.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_withExistingResponseId}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Duplicate", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already submitted", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// (5) Innovation not Published (Draft) → 404.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_InnovationNotPublished_Returns404()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_draftId}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest()), token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("published", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// (6) Rationale field under 20 chars → 422 with field name in error.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_RationaleTooShort_Returns422WithFieldName()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = new[]
            {
                new
                {
                    year = 1,
                    productionVolume = 10000,
                    productionVolumeRationale = "Too short",
                    unitCost = 5.50,
                    unitCostRationale = "Materials, labor, and overhead costed against supplier agreements.",
                    averageGlobalDistributionExpense = 1.20,
                    avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ProductionVolumeRationale", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// (7) Non-contiguous years [1,3] → 422.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_NonContiguousYears_Returns422()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var validEntry = MakeValidYears(1)[0];
        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = new object[]
            {
                validEntry,
                new
                {
                    year = 3,
                    productionVolume = 20000,
                    productionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput estimates.",
                    unitCost = 5.50,
                    unitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
                    averageGlobalDistributionExpense = 1.20,
                    avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("contiguous", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing year(s): 2", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// (8) Year &gt; 10 → 422.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_YearAboveTen_Returns422()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var years = Enumerable.Range(1, 11).Select(year => (object)new
        {
            year,
            productionVolume = 10000,
            productionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput estimates.",
            unitCost = 5.50,
            unitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
            averageGlobalDistributionExpense = 1.20,
            avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
        }).ToList();

        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = years
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot exceed 10", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Exactly 10 years is accepted (upper boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_ExactlyTenYears_Returns201()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest(years: 10)), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Rationale exactly at the 20-char minimum is accepted (boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_RationaleExactlyTwentyChars_Returns201()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var exactlyTwenty = new string('a', 20);
        Assert.Equal(20, exactlyTwenty.Length);
        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = new[]
            {
                new
                {
                    year = 1,
                    productionVolume = 10000,
                    productionVolumeRationale = exactlyTwenty,
                    unitCost = 5.50,
                    unitCostRationale = exactlyTwenty,
                    averageGlobalDistributionExpense = 1.20,
                    avgDistributionExpenseRationale = exactlyTwenty
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Rationale exceeding the 500-char maximum is rejected.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_RationaleAboveFiveHundredChars_Returns422()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var tooLong = new string('a', 501);
        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = new[]
            {
                new
                {
                    year = 1,
                    productionVolume = 10000,
                    productionVolumeRationale = tooLong,
                    unitCost = 5.50,
                    unitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
                    averageGlobalDistributionExpense = 1.20,
                    avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ProductionVolumeRationale", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// participationProposal exactly at the 100-char minimum is accepted (boundary is inclusive).
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_ParticipationProposalExactlyOneHundredChars_Returns201()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var exactlyOneHundred = new string('a', 100);
        Assert.Equal(100, exactlyOneHundred.Length);
        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = exactlyOneHundred,
            yearlyManufacturingCosts = MakeValidYears(1)
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// participationProposal one character under the 100-char minimum is rejected.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_ParticipationProposalNinetyNineChars_Returns400()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var ninetyNine = new string('a', 99);
        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = ninetyNine,
            yearlyManufacturingCosts = MakeValidYears(1)
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ParticipationProposal", body, StringComparison.Ordinal);
        Assert.Contains("100", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// (9) Year 1 complete + year 2 missing a rationale field entirely → 422 (FR-009 partial projection rejection).
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_PartialProjectionEntry_Returns422()
    {
        var token = await GetAccessToken("mfg@submitmfg.test", "Manufacturing");
        var request = new
        {
            location = "Europe",
            participationType = "Manufacturing Partner",
            participationProposal = MakeProposal(),
            yearlyManufacturingCosts = new object[]
            {
                MakeValidYears(1)[0],
                new
                {
                    year = 2,
                    productionVolume = 15000,
                    unitCost = 5.75,
                    unitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
                    averageGlobalDistributionExpense = 1.25,
                    avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
                    // productionVolumeRationale intentionally omitted
                }
            }
        };

        var response = await _client.PostWithAuthAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(request), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ProductionVolumeRationale", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// ValidateProjection should treat null input as an empty list rather than throw.
    /// </summary>
    [Fact]
    public void ValidateProjection_NullList_ReturnsValidationError()
    {
        var errors = InvokeValidateProjection(null);

        Assert.Contains("YearlyManufacturingCosts", errors.Keys);
    }

    /// <summary>
    /// ValidateProjection should reject negative numeric values per contract.
    /// </summary>
    [Fact]
    public void ValidateProjection_NegativeNumericValues_ReturnsValidationErrors()
    {
        var entries = new List<Innoventity.API.Features.Bids.SubmitManufacturingResponse.YearlyManufacturingCostRequest>
        {
            new()
            {
                Year = 1,
                ProductionVolume = -1,
                ProductionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput estimates.",
                UnitCost = -5.50m,
                UnitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
                AverageGlobalDistributionExpense = -1.20m,
                AvgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
            }
        };

        var errors = InvokeValidateProjection(entries);

        Assert.Contains("YearlyManufacturingCosts[0].ProductionVolume", errors.Keys);
        Assert.Contains("YearlyManufacturingCosts[0].UnitCost", errors.Keys);
        Assert.Contains("YearlyManufacturingCosts[0].AverageGlobalDistributionExpense", errors.Keys);
    }

    /// <summary>
    /// ValidateProjection should report duplicate projection years explicitly.
    /// </summary>
    [Fact]
    public void ValidateProjection_DuplicateYears_ReturnsDuplicateYearError()
    {
        var entries = new List<Innoventity.API.Features.Bids.SubmitManufacturingResponse.YearlyManufacturingCostRequest>
        {
            new()
            {
                Year = 1,
                ProductionVolume = 10000,
                ProductionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput estimates.",
                UnitCost = 5.50m,
                UnitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
                AverageGlobalDistributionExpense = 1.20m,
                AvgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
            },
            new()
            {
                Year = 1,
                ProductionVolume = 12000,
                ProductionVolumeRationale = "Based on Q2 supplier capacity quotes and pilot line throughput estimates.",
                UnitCost = 5.10m,
                UnitCostRationale = "Materials, labor, and overhead costed against updated supplier agreements.",
                AverageGlobalDistributionExpense = 1.10m,
                AvgDistributionExpenseRationale = "Weighted by updated target market logistics rates across regions."
            }
        };

        var errors = InvokeValidateProjection(entries);

        Assert.Contains("YearlyManufacturingCosts", errors.Keys);
        Assert.Contains("duplicate year(s): 1", errors["YearlyManufacturingCosts"][0], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ValidateProjection should report invalid years explicitly instead of masking them as gaps.
    /// </summary>
    [Fact]
    public void ValidateProjection_InvalidYears_ReturnsInvalidYearError()
    {
        var entries = new List<Innoventity.API.Features.Bids.SubmitManufacturingResponse.YearlyManufacturingCostRequest>
        {
            new()
            {
                Year = 0,
                ProductionVolume = 10000,
                ProductionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput estimates.",
                UnitCost = 5.50m,
                UnitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
                AverageGlobalDistributionExpense = 1.20m,
                AvgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
            }
        };

        var errors = InvokeValidateProjection(entries);

        Assert.Contains("YearlyManufacturingCosts", errors.Keys);
        Assert.Contains("invalid year(s): 0", errors["YearlyManufacturingCosts"][0], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Unauthenticated request returns 401.
    /// </summary>
    [Fact]
    public async Task SubmitManufacturingResponse_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsync(
            $"/innovations/{_publishedId}/bids/manufacturing",
            JsonContent.Create(MakeValidRequest()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
