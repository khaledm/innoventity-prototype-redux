using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Tests.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Innoventity.API.Tests.E2E.Journeys;

/// <summary>
/// Journey 2: Innovation Discovery &amp; Formal Response Submission (Spec 005)
/// Tests the complete flow of discovering published innovations and submitting typed
/// partnership proposals (FormalResponse hierarchy) via the type-specific endpoints.
/// </summary>
public class Journey2_BiddingTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public Journey2_BiddingTests()
    {
        _databaseName = $"Journey2TestDb_{Guid.NewGuid()}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database with unique name per test instance
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(_databaseName);
                    });

                    // Build service provider to seed reference data
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<AppDbContext>();

                    // Seed reference data (Industries)
                    SeedReferenceData(db);
                });
            });

        _client = _factory.CreateClient();
    }

    private void SeedReferenceData(AppDbContext db)
    {
        // Seed 10 ICB industries (matches production data)
        var industries = new[]
        {
            new Industry("HLTH-001") { Name = "Healthcare Equipment & Services" },
            new Industry("TECH-001") { Name = "Technology Hardware & Equipment" },
            new Industry("BANK-001") { Name = "Banks" },
            new Industry("AUTO-001") { Name = "Automobiles & Parts" },
            new Industry("FOOD-001") { Name = "Food Producers" },
            new Industry("CHEM-001") { Name = "Chemicals" },
            new Industry("TELE-001") { Name = "Telecommunications" },
            new Industry("UTIL-001") { Name = "Utilities" },
            new Industry("CONS-001") { Name = "Construction & Materials" },
            new Industry("MINE-001") { Name = "Mining" }
        };

        db.Industries.AddRange(industries);
        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    #region Helper Methods

    /// <summary>
    /// Helper: Register a new actor
    /// </summary>
    private async Task<ActorRegistrationResult> RegisterActor(string actorType, string email, string password = "TestPassword123!")
    {
        var request = new
        {
            email,
            password,
            actorType,
            firstName = "Test",
            lastName = "User",
            phone = "+1-555-555-5555",
            contactAddress = new
            {
                address1 = "123 Test Street",
                city = "Test City",
                postCode = "12345",
                countryCode = "US"
            }
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new ActorRegistrationResult
        {
            ActorId = Guid.Parse(data.GetProperty("actorId").GetString() ?? throw new InvalidOperationException()),
            Email = email,
            ActivationToken = data.GetProperty("activationToken").GetString() ?? throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Helper: Activate account with activation token
    /// </summary>
    private async Task<HttpStatusCode> ActivateAccount(string email, string activationToken)
    {
        var request = new { email, token = activationToken };
        var response = await _client.PostAsJsonAsync("/auth/activate", request);
        return response.StatusCode;
    }

    /// <summary>
    /// Helper: Login and obtain JWT token
    /// </summary>
    private async Task<LoginResult> Login(string email, string actorType, string password = "TestPassword123!")
    {
        var request = new { email, actorType, password };
        var response = await _client.PostAsJsonAsync("/auth/login", request);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new LoginResult
        {
            AccessToken = data.GetProperty("accessToken").GetString() ?? throw new InvalidOperationException(),
            RefreshToken = data.GetProperty("refreshToken").GetString() ?? throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Helper: Create innovation (returns Created result with InnovationId)
    /// </summary>
    private async Task<InnovationCreationResult> CreateInnovation(string token, object request)
    {
        var response = await _client.PostWithAuthAsync("/innovations",
            JsonContent.Create(request), token);

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            return new InnovationCreationResult
            {
                StatusCode = response.StatusCode,
                InnovationId = Guid.Parse(data.GetProperty("innovationId").GetString() ?? throw new InvalidOperationException())
            };
        }

        return new InnovationCreationResult
        {
            StatusCode = response.StatusCode,
            InnovationId = Guid.Empty
        };
    }

    /// <summary>
    /// Helper: Submit innovation for publication
    /// </summary>
    private async Task<InnovationSubmissionResult> SubmitInnovation(Guid innovationId, string token)
    {
        var response = await _client.PatchWithAuthAsync($"/innovations/{innovationId}/submit",
            JsonContent.Create(new { }), token);

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            return new InnovationSubmissionResult
            {
                StatusCode = response.StatusCode,
                Status = data.GetProperty("status").GetString() ?? string.Empty
            };
        }

        return new InnovationSubmissionResult
        {
            StatusCode = response.StatusCode,
            Status = "Failed"
        };
    }

    /// <summary>
    /// Helper: List innovations (with optional filters)
    /// </summary>
    private async Task<HttpResponseMessage> ListInnovations(string token, string? queryString = null)
    {
        var url = string.IsNullOrEmpty(queryString) ? "/innovations" : $"/innovations?{queryString}";
        return await _client.GetWithAuthAsync(url, token);
    }

    /// <summary>
    /// Helper: Get innovation details
    /// </summary>
    private async Task<HttpResponseMessage> GetInnovation(Guid innovationId, string token)
    {
        return await _client.GetWithAuthAsync($"/innovations/{innovationId}", token);
    }

    /// <summary>
    /// Helper: Submit a Manufacturing formal response on an innovation
    /// </summary>
    private async Task<ResponseSubmissionResult> SubmitManufacturingResponse(Guid innovationId, string token, object request)
    {
        var response = await _client.PostWithAuthAsync($"/innovations/{innovationId}/bids/manufacturing",
            JsonContent.Create(request), token);
        return await ReadSubmissionResult(response);
    }

    /// <summary>
    /// Helper: Submit a Research &amp; Development formal response on an innovation
    /// </summary>
    private async Task<ResponseSubmissionResult> SubmitResearchDevelopmentResponse(Guid innovationId, string token, object request)
    {
        var response = await _client.PostWithAuthAsync($"/innovations/{innovationId}/bids/rd",
            JsonContent.Create(request), token);
        return await ReadSubmissionResult(response);
    }

    private static async Task<ResponseSubmissionResult> ReadSubmissionResult(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            return new ResponseSubmissionResult
            {
                StatusCode = response.StatusCode,
                ResponseId = Guid.Parse(data.GetProperty("responseId").GetString() ?? throw new InvalidOperationException()),
                ResponseType = data.GetProperty("responseType").GetString() ?? string.Empty,
                Status = data.GetProperty("status").GetString() ?? string.Empty
            };
        }

        return new ResponseSubmissionResult
        {
            StatusCode = response.StatusCode,
            ResponseId = Guid.Empty,
            ResponseType = string.Empty,
            Status = "Failed"
        };
    }

    /// <summary>
    /// Helper: Get formal responses for an innovation (3-tier visibility per caller)
    /// </summary>
    private async Task<HttpResponseMessage> GetBidsForInnovation(Guid innovationId, string token)
    {
        return await _client.GetWithAuthAsync($"/innovations/{innovationId}/bids", token);
    }

    private static object MakeManufacturingRequest(string location = "Europe") => new
    {
        location,
        participationType = "Manufacturing Partner",
        participationProposal = "We are a leading manufacturing company with 20+ years of experience in clean energy production. Our state-of-the-art facilities and global distribution network position us perfectly to scale this innovation to market. We can offer advanced prototyping, quality assurance, and mass production capabilities with competitive pricing.",
        yearlyManufacturingCosts = new[]
        {
            new
            {
                year = 1,
                productionVolume = 10000,
                productionVolumeRationale = "Based on Q1 supplier capacity quotes and pilot line throughput estimates.",
                unitCost = 5.50,
                unitCostRationale = "Materials, labor, and overhead costed against current supplier agreements.",
                averageGlobalDistributionExpense = 1.20,
                avgDistributionExpenseRationale = "Weighted by target market logistics rates across regions."
            }
        }
    };

    private static object MakeResearchDevelopmentRequest() => new
    {
        location = "Europe",
        participationType = "R&D Partner",
        participationProposal = "We are an established R&D organization with extensive experience in technology development and commercialization. Our team of experts can provide valuable insights and accelerate the innovation's path to market with proven methodologies.",
        productDevelopmentDuration = 2,
        yearlyDevelopmentCosts = new[]
        {
            new
            {
                year = 1,
                infrastructureCost = 50000.00,
                infrastructureCostRationale = "Cloud hosting, lab tooling, and software licenses for year one.",
                peopleCost = 200000.00,
                peopleCostRationale = "Three engineers and one project manager allocated to this workstream."
            }
        }
    };

    #endregion

    #region Journey 2 Tests

    /// <summary>
    /// PRIMARY JOURNEY: Manufacturing actor discovers and submits a typed response
    /// </summary>
    [Fact]
    public async Task Journey2_ManufacturingActorSubmitsResponse_ResponseRecorded()
    {
        // Step 1: Seed published innovation (owned by Idea Generator)
        var ideaGenerator = await RegisterActor("IdeaGenerator", $"owner-{Guid.NewGuid()}@test.com");
        await ActivateAccount(ideaGenerator.Email, ideaGenerator.ActivationToken);
        var ownerLogin = await Login(ideaGenerator.Email, "IdeaGenerator");
        var ownerToken = ownerLogin.AccessToken;

        var createRequest = new
        {
            title = "Revolutionary Green Energy Innovation",
            productType = "Clean Energy Device",
            researchCategory = "Engineering",
            researchBackground = "This innovation represents a breakthrough in renewable energy technology with extensive research backing and proven prototypes demonstrating significant efficiency improvements.",
            hasIPR = true,
            hasRightToUse = true,
            productDescription = "Advanced clean energy solution",
            technologyDescription = "Novel green technology",
            targetBeneficiaries = "Energy sector",
            productAdvantages = "High efficiency, low cost",
            developmentPhase = "Prototype",
            developmentProcess = "Rigorous testing completed",
            targetMarket = "Global energy market",
            targetCustomerBase = "Energy providers",
            targetCustomerType = "B2B",
            productKeywords = "energy, green, innovation",
            advantageKeywords = "efficiency, sustainable",
            relevantMarketSize = 2000000,
            potentialMarketSize = 10000000,
            targetIndustryIds = new[] { "UTIL-001" },
            partnersNeeded = new[] { "Manufacturing" }
        };

        var createResult = await CreateInnovation(ownerToken, createRequest);
        Assert.Equal(HttpStatusCode.Created, createResult.StatusCode);
        var innovationId = createResult.InnovationId;

        var submitResult = await SubmitInnovation(innovationId, ownerToken);
        Assert.Equal(HttpStatusCode.OK, submitResult.StatusCode);
        Assert.Equal("Published", submitResult.Status);

        // Step 2: Register Manufacturing actor
        var manufacturingActor = await RegisterActor("Manufacturing", $"manufacturer-{Guid.NewGuid()}@test.com");

        // Step 3: Activate and Login
        await ActivateAccount(manufacturingActor.Email, manufacturingActor.ActivationToken);
        var manufacturingLogin = await Login(manufacturingActor.Email, "Manufacturing");
        var manufacturingToken = manufacturingLogin.AccessToken;

        // Step 4: Discover innovations (with industry filter)
        var discoverResponse = await ListInnovations(manufacturingToken, "targetIndustryIds=UTIL-001");
        Assert.Equal(HttpStatusCode.OK, discoverResponse.StatusCode);
        var innovations = await discoverResponse.Content.ReadFromJsonAsync<InnovationListResponse>();
        Assert.NotNull(innovations);
        Assert.Contains(innovations.Items, i => i.InnovationId == innovationId);

        // Step 5: View innovation details
        var detailsResponse = await GetInnovation(innovationId, manufacturingToken);
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

        // Step 6: Submit typed Manufacturing response
        var submitResponseResult = await SubmitManufacturingResponse(innovationId, manufacturingToken, MakeManufacturingRequest());
        Assert.Equal(HttpStatusCode.Created, submitResponseResult.StatusCode);
        Assert.NotEqual(Guid.Empty, submitResponseResult.ResponseId);
        Assert.Equal("ManufacturingResponse", submitResponseResult.ResponseType);
        Assert.Equal("Pending", submitResponseResult.Status);

        // Step 7: Verify response in database (Pending status)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var responseInDb = await db.FormalResponses.FirstOrDefaultAsync(r => r.Id == submitResponseResult.ResponseId);
        Assert.NotNull(responseInDb);
        Assert.Equal(ResponseStatus.Pending, responseInDb.Status);
        Assert.Equal(innovationId, responseInDb.InnovationId);
        Assert.IsType<ManufacturingResponse>(responseInDb);

        // Step 8: Verify owner sees the response (GET /innovations/{innovationId}/bids)
        var ownerBidsResponse = await GetBidsForInnovation(innovationId, ownerToken);
        Assert.Equal(HttpStatusCode.OK, ownerBidsResponse.StatusCode);
        var bidsData = await ownerBidsResponse.Content.ReadFromJsonAsync<BidsListResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(bidsData);
        Assert.Single(bidsData.Responses);
        Assert.Equal(submitResponseResult.ResponseId, bidsData.Responses[0].ResponseId);
    }

    /// <summary>
    /// ERROR PATH: Idea Generator attempts to submit a response (should return 403 Forbidden)
    /// </summary>
    [Fact]
    public async Task Journey2_IdeaGeneratorCannotSubmitResponse_Returns403()
    {
        // Create published innovation (owned by Idea Generator)
        var owner = await RegisterActor("IdeaGenerator", $"owner-{Guid.NewGuid()}@test.com");
        await ActivateAccount(owner.Email, owner.ActivationToken);
        var ownerLogin = await Login(owner.Email, "IdeaGenerator");
        var ownerToken = ownerLogin.AccessToken;

        var createRequest = new
        {
            title = "Innovation for Response Submission Test",
            productType = "Test Product",
            researchCategory = "Engineering",
            researchBackground = "This is a detailed research background with sufficient length to meet all validation requirements for successful submission to published status. We have conducted extensive research and prototyping.",
            hasIPR = true,
            hasRightToUse = true,
            productDescription = "Complete product description",
            technologyDescription = "Complete technology description",
            targetBeneficiaries = "Target beneficiaries",
            productAdvantages = "Product advantages",
            developmentPhase = "Prototype",
            developmentProcess = "Development process",
            targetMarket = "Target market",
            targetCustomerBase = "Customer base",
            targetCustomerType = "B2B",
            productKeywords = "keywords",
            advantageKeywords = "advantages",
            relevantMarketSize = 1000000,
            potentialMarketSize = 5000000,
            targetIndustryIds = new[] { "TECH-001" },
            partnersNeeded = new[] { "Manufacturing" }
        };

        var createResult = await CreateInnovation(ownerToken, createRequest);
        var innovationId = createResult.InnovationId;

        var submitResult = await SubmitInnovation(innovationId, ownerToken);
        Assert.Equal(HttpStatusCode.OK, submitResult.StatusCode);

        // Register Idea Generator and attempt to submit a Manufacturing response
        var ideaGenerator = await RegisterActor("IdeaGenerator", $"ideagen-{Guid.NewGuid()}@test.com");
        await ActivateAccount(ideaGenerator.Email, ideaGenerator.ActivationToken);
        var ideaGenLogin = await Login(ideaGenerator.Email, "IdeaGenerator");
        var ideaGenToken = ideaGenLogin.AccessToken;

        var result = await SubmitManufacturingResponse(innovationId, ideaGenToken, MakeManufacturingRequest());
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    /// <summary>
    /// ERROR PATH: Actor submits a duplicate response (should return 409 Conflict)
    /// </summary>
    [Fact]
    public async Task Journey2_DuplicateResponse_Returns409()
    {
        // Create published innovation
        var owner = await RegisterActor("IdeaGenerator", $"owner-{Guid.NewGuid()}@test.com");
        await ActivateAccount(owner.Email, owner.ActivationToken);
        var ownerLogin = await Login(owner.Email, "IdeaGenerator");
        var ownerToken = ownerLogin.AccessToken;

        var createRequest = new
        {
            title = "Innovation for Duplicate Response Test",
            productType = "Test Product",
            researchCategory = "Engineering",
            researchBackground = "This is a detailed research background with sufficient length to meet all validation requirements for successful submission to published status. We have conducted extensive research and prototyping.",
            hasIPR = true,
            hasRightToUse = true,
            productDescription = "Complete product description",
            technologyDescription = "Complete technology description",
            targetBeneficiaries = "Target beneficiaries",
            productAdvantages = "Product advantages",
            developmentPhase = "Prototype",
            developmentProcess = "Development process",
            targetMarket = "Target market",
            targetCustomerBase = "Customer base",
            targetCustomerType = "B2B",
            productKeywords = "keywords",
            advantageKeywords = "advantages",
            relevantMarketSize = 1000000,
            potentialMarketSize = 5000000,
            targetIndustryIds = new[] { "TECH-001" },
            partnersNeeded = new[] { "RD" }
        };

        var createResult = await CreateInnovation(ownerToken, createRequest);
        var innovationId = createResult.InnovationId;

        var submitResult = await SubmitInnovation(innovationId, ownerToken);
        Assert.Equal(HttpStatusCode.OK, submitResult.StatusCode);

        // Register RD actor and submit first response
        var rdActor = await RegisterActor("RD", $"rd-{Guid.NewGuid()}@test.com");
        await ActivateAccount(rdActor.Email, rdActor.ActivationToken);
        var rdLogin = await Login(rdActor.Email, "RD");
        var rdToken = rdLogin.AccessToken;

        var firstResult = await SubmitResearchDevelopmentResponse(innovationId, rdToken, MakeResearchDevelopmentRequest());
        Assert.Equal(HttpStatusCode.Created, firstResult.StatusCode);

        // Attempt second response (duplicate)
        var secondResult = await SubmitResearchDevelopmentResponse(innovationId, rdToken, MakeResearchDevelopmentRequest());
        Assert.Equal(HttpStatusCode.Conflict, secondResult.StatusCode);
    }

    #endregion

    #region Helper DTOs

    private record ActorRegistrationResult
    {
        public Guid ActorId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string ActivationToken { get; init; } = string.Empty;
    }

    private record LoginResult
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }

    private record InnovationCreationResult
    {
        public HttpStatusCode StatusCode { get; init; }
        public Guid InnovationId { get; init; }
    }

    private record InnovationSubmissionResult
    {
        public HttpStatusCode StatusCode { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    private record ResponseSubmissionResult
    {
        public HttpStatusCode StatusCode { get; init; }
        public Guid ResponseId { get; init; }
        public string ResponseType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    private record InnovationListResponse
    {
        public List<InnovationSummary> Items { get; init; } = new();
        public int TotalCount { get; init; }
    }

    private record InnovationSummary
    {
        public Guid InnovationId { get; init; }
        public string Title { get; init; } = string.Empty;
    }

    private record BidsListResponse
    {
        public Guid InnovationId { get; init; }
        public List<BidDetail> Responses { get; init; } = new();
    }

    private record BidDetail
    {
        public Guid ResponseId { get; init; }
        public string Location { get; init; } = string.Empty;
        public string ParticipationType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    #endregion
}
