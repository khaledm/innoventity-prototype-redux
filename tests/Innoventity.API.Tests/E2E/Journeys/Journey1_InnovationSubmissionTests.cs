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

namespace Innoventity.API.Tests.E2E.Journeys;

/// <summary>
/// Journey 1: Complete Innovation Submission & Publication Flow
/// Tests the full lifecycle: Register → Activate → Login → Create Draft → Update → Submit → Verify Published
/// This validates the end-to-end user journey through multiple API endpoints with real infrastructure.
/// </summary>
public class Journey1_InnovationSubmissionTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName;

    public Journey1_InnovationSubmissionTests()
    {
        _databaseName = $"TestDb_Journey1_{Guid.NewGuid()}";
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedReferenceData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = _databaseName; // Capture in closure

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

    private void SeedReferenceData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed industries (required for innovation submission)
        if (!context.Set<Industry>().Any())
        {
            context.Set<Industry>().AddRange(
                new Industry("HLTH-001") { Name = "Health Care" },
                new Industry("FINA-001") { Name = "Financials" },
                new Industry("TECH-001") { Name = "Technology" },
                new Industry("CONG-001") { Name = "Consumer Goods" },
                new Industry("INDU-001") { Name = "Industrials" },
                new Industry("TELE-001") { Name = "Telecommunications" },
                new Industry("UTIL-001") { Name = "Utilities" },
                new Industry("CONS-001") { Name = "Consumer Services" },
                new Industry("OILG-001") { Name = "Oil & Gas" },
                new Industry("MATL-001") { Name = "Basic Materials" }
            );
            context.SaveChanges();
        }
    }

    /// <summary>
    /// Helper: Register a new actor
    /// </summary>
    private async Task<ActorRegistrationResult> RegisterActor(string actorType, string email, string password = "TestPassword123!")
    {
        var registerRequest = new
        {
            firstName = "Test",
            lastName = "User",
            email = email,
            actorType = actorType,
            password = password,
            contactAddress = new
            {
                address1 = "123 Test Street",
                city = "London",
                postCode = "SW1A 1AA",
                countryCode = "GB"
            }
        };

        var response = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new ActorRegistrationResult
        {
            StatusCode = response.StatusCode,
            ActorId = data.GetProperty("actorId").GetString() ?? throw new InvalidOperationException(),
            ActivationToken = data.GetProperty("activationToken").GetString() ?? throw new InvalidOperationException(),
            Email = email
        };
    }

    /// <summary>
    /// Helper: Activate an actor's account
    /// </summary>
    private async Task<HttpStatusCode> ActivateAccount(string email, string activationToken)
    {
        var activateRequest = new
        {
            email = email,
            token = activationToken
        };

        var response = await _client.PostAsJsonAsync("/auth/activate", activateRequest);
        return response.StatusCode;
    }

    /// <summary>
    /// Helper: Login and obtain JWT access token
    /// </summary>
    private async Task<LoginResult> Login(string email, string actorType, string password = "TestPassword123!")
    {
        var loginRequest = new
        {
            email = email,
            actorType = actorType,
            password = password
        };

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new LoginResult
        {
            StatusCode = response.StatusCode,
            AccessToken = data.GetProperty("accessToken").GetString() ?? throw new InvalidOperationException(),
            ActorType = data.GetProperty("actor").GetProperty("actorType").GetString() ?? throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Helper: Create a draft innovation
    /// </summary>
    private async Task<InnovationCreationResult> CreateInnovation(string token, object innovationRequest)
    {
        var response = await _client.PostWithAuthAsync("/innovations",
            JsonContent.Create(innovationRequest), token);

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            return new InnovationCreationResult
            {
                StatusCode = response.StatusCode,
                InnovationId = Guid.Parse(data.GetProperty("innovationId").GetString() ?? throw new InvalidOperationException()),
                Status = data.GetProperty("status").GetString() ?? throw new InvalidOperationException()
            };
        }

        return new InnovationCreationResult
        {
            StatusCode = response.StatusCode,
            InnovationId = Guid.Empty,
            Status = "Failed"
        };
    }

    /// <summary>
    /// Helper: Update a draft innovation
    /// </summary>
    private async Task<HttpStatusCode> UpdateInnovation(Guid innovationId, string token, object updateRequest)
    {
        var response = await _client.PutWithAuthAsync($"/innovations/{innovationId}",
            JsonContent.Create(updateRequest), token);
        return response.StatusCode;
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
                Status = data.GetProperty("status").GetString() ?? throw new InvalidOperationException(),
                SubmittedAt = data.GetProperty("submittedAt").GetString()
            };
        }

        return new InnovationSubmissionResult
        {
            StatusCode = response.StatusCode,
            Status = "Failed",
            SubmittedAt = null
        };
    }

    /// <summary>
    /// Helper: Get innovation details
    /// </summary>
    private async Task<HttpResponseMessage> GetInnovation(Guid innovationId, string token)
    {
        return await _client.GetWithAuthAsync($"/innovations/{innovationId}", token);
    }

    /// <summary>
    /// Helper: List innovations with filters
    /// </summary>
    private async Task<HttpResponseMessage> ListInnovations(string token, string? industryId = null, string? researchCategory = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(industryId))
            queryParams.Add($"industryId={industryId}");
        if (!string.IsNullOrEmpty(researchCategory))
            queryParams.Add($"researchCategory={researchCategory}");

        var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _client.GetWithAuthAsync($"/innovations{query}", token);
    }

    /// <summary>
    /// PRIMARY JOURNEY TEST: Idea Generator submits and publishes an innovation successfully
    /// Orchestrates 8 API calls: Register → Activate → Login → Create → Update → Submit → Verify → Discover
    /// </summary>
    [Fact]
    public async Task Journey1_IdeaGeneratorSubmitsInnovation_PublishedSuccessfully()
    {
        // Step 1-3: Register, Activate, Login as Idea Generator
        var actor = await RegisterActor("IdeaGenerator", $"innovator-{Guid.NewGuid()}@test.com");
        Assert.Equal(HttpStatusCode.Created, actor.StatusCode);

        var activateStatus = await ActivateAccount(actor.Email, actor.ActivationToken);
        Assert.Equal(HttpStatusCode.OK, activateStatus);

        var login = await Login(actor.Email, "IdeaGenerator");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = login.AccessToken;

        // Step 4: Create Draft Innovation
        var createRequest = new
        {
            title = "AI-Powered Medical Diagnostic Platform",
            productType = "Healthcare Software",
            researchBackground = "Advanced machine learning algorithms for early disease detection through medical imaging analysis, developed over 3 years of research collaboration with leading medical institutions.",
            researchCategory = "Engineering",
            hasIPR = true,
            hasRightToUse = true,
            productDescription = "Cloud-based diagnostic platform using deep learning to analyze medical images (X-rays, MRIs, CT scans) with 95% accuracy.",
            technologyDescription = "Convolutional neural networks trained on 1M+ medical images, utilizing transfer learning and ensemble methods for robust predictions.",
            targetBeneficiaries = "Hospitals, diagnostic centers, rural healthcare clinics, telemedicine providers.",
            productAdvantages = "95% diagnostic accuracy, 10x faster than manual review, reduces radiologist workload, enables remote diagnostics.",
            developmentPhase = "Beta Testing",
            developmentProcess = "MVP deployed in 5 pilot hospitals, collecting feedback for production release.",
            targetMarket = "Healthcare providers in Europe and North America",
            targetCustomerBase = "Public and private hospitals, diagnostic imaging centers",
            targetCustomerType = "B2B",
            productKeywords = "AI, medical imaging, diagnostics, machine learning, healthcare",
            advantageKeywords = "accuracy, speed, automation, remote access",
            relevantMarketSize = 15000000000,
            potentialMarketSize = 50000000000,
            targetIndustryIds = new[] { "HLTH-001" },
            partnersNeeded = new[] { "RD", "SalesMarketing" }
        };

        var createResult = await CreateInnovation(token, createRequest);
        Assert.Equal(HttpStatusCode.Created, createResult.StatusCode);
        Assert.Equal("Draft", createResult.Status);
        var innovationId = createResult.InnovationId;

        // Step 5: Update Innovation (optional refinement)
        var updateRequest = new
        {
            productAdvantages = "98% diagnostic accuracy (improved from 95%), 10x faster than manual review, reduces radiologist workload by 70%, enables remote diagnostics in underserved areas."
        };

        var updateStatus = await UpdateInnovation(innovationId, token, updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateStatus);

        // Step 6: Submit for Publication
        var submitResult = await SubmitInnovation(innovationId, token);
        Assert.Equal(HttpStatusCode.OK, submitResult.StatusCode);
        Assert.Equal("Published", submitResult.Status);
        Assert.NotNull(submitResult.SubmittedAt);

        // Step 7: Verify status change in database
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var innovation = await context.Set<Innovation>().FindAsync(innovationId);
            Assert.NotNull(innovation);
            Assert.Equal(InnovationStatus.Published, innovation.Status);
            Assert.NotNull(innovation.SubmittedAt);
        }

        // Step 8: Verify discoverable by other actors (Manufacturing)
        var manufacturingActor = await RegisterActor("Manufacturing", $"manufacturer-{Guid.NewGuid()}@test.com");
        await ActivateAccount(manufacturingActor.Email, manufacturingActor.ActivationToken);
        var manufacturingLogin = await Login(manufacturingActor.Email, "Manufacturing");

        var discoveries = await ListInnovations(manufacturingLogin.AccessToken, industryId: "HLTH-001");
        Assert.Equal(HttpStatusCode.OK, discoveries.StatusCode);

        var discoveriesData = await discoveries.Content.ReadFromJsonAsync<JsonElement>();
        var items = discoveriesData.GetProperty("items").EnumerateArray();
        Assert.Contains(items, item =>
            Guid.Parse(item.GetProperty("innovationId").GetString()!) == innovationId);
    }

    /// <summary>
    /// ERROR PATH: Submitting incomplete innovation returns 400 with validation errors
    /// </summary>
    [Fact]
    public async Task Journey1_SubmitIncompleteInnovation_Returns400WithValidationErrors()
    {
        // Register, Activate, Login
        var actor = await RegisterActor("IdeaGenerator", $"incomplete-{Guid.NewGuid()}@test.com");
        await ActivateAccount(actor.Email, actor.ActivationToken);
        var login = await Login(actor.Email, "IdeaGenerator");
        var token = login.AccessToken;

        // Create incomplete draft (missing required fields for submission)
        var incompleteRequest = new
        {
            title = "Incomplete Innovation",
            productType = "Test Product",
            researchBackground = "Short", // Too short (needs 50+ chars)
            researchCategory = "Engineering",
            hasIPR = true,
            hasRightToUse = true
        };

        var createResult = await CreateInnovation(token, incompleteRequest);
        Assert.Equal(HttpStatusCode.Created, createResult.StatusCode);
        var innovationId = createResult.InnovationId;

        // Attempt to submit incomplete innovation
        var submitResult = await SubmitInnovation(innovationId, token);
        Assert.Equal(HttpStatusCode.BadRequest, submitResult.StatusCode);
    }

    /// <summary>
    /// ERROR PATH: Non-owner cannot submit another user's innovation
    /// </summary>
    [Fact]
    public async Task Journey1_NonOwnerCannotSubmitInnovation_Returns403()
    {
        // Actor 1: Creates innovation
        var owner = await RegisterActor("IdeaGenerator", $"owner-{Guid.NewGuid()}@test.com");
        await ActivateAccount(owner.Email, owner.ActivationToken);
        var ownerLogin = await Login(owner.Email, "IdeaGenerator");

        var createRequest = new
        {
            title = "Owner's Innovation",
            productType = "Test Product",
            researchBackground = "This is a detailed research background with sufficient length to meet validation requirements for the innovation submission process.",
            researchCategory = "Engineering",
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

        var createResult = await CreateInnovation(ownerLogin.AccessToken, createRequest);
        Assert.Equal(HttpStatusCode.Created, createResult.StatusCode);
        var innovationId = createResult.InnovationId;

        // Actor 2: Attempts to submit Actor 1's innovation
        var nonOwner = await RegisterActor("IdeaGenerator", $"nonowner-{Guid.NewGuid()}@test.com");
        await ActivateAccount(nonOwner.Email, nonOwner.ActivationToken);
        var nonOwnerLogin = await Login(nonOwner.Email, "IdeaGenerator");

        var submitResult = await SubmitInnovation(innovationId, nonOwnerLogin.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, submitResult.StatusCode);
    }

    /// <summary>
    /// ERROR PATH: Cannot re-submit already published innovation (immutability)
    /// </summary>
    [Fact]
    public async Task Journey1_SubmitAlreadyPublished_Returns409()
    {
        // Register, Activate, Login
        var actor = await RegisterActor("IdeaGenerator", $"resubmit-{Guid.NewGuid()}@test.com");
        await ActivateAccount(actor.Email, actor.ActivationToken);
        var login = await Login(actor.Email, "IdeaGenerator");
        var token = login.AccessToken;

        // Create complete innovation
        var createRequest = new
        {
            title = "Already Published Innovation",
            productType = "Test Product",
            researchCategory = "Engineering",
            researchBackground = "This is a detailed research background with sufficient length to meet all validation requirements for successful submission.",
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

        var createResult = await CreateInnovation(token, createRequest);
        Assert.Equal(HttpStatusCode.Created, createResult.StatusCode);
        var innovationId = createResult.InnovationId;

        // First submission - should succeed
        var firstSubmit = await SubmitInnovation(innovationId, token);
        Assert.Equal(HttpStatusCode.OK, firstSubmit.StatusCode);
        Assert.Equal("Published", firstSubmit.Status);

        // Second submission attempt - should fail with 409 Conflict
        var secondSubmit = await SubmitInnovation(innovationId, token);
        Assert.Equal(HttpStatusCode.Conflict, secondSubmit.StatusCode);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    // Helper DTOs
    private record ActorRegistrationResult
    {
        public HttpStatusCode StatusCode { get; init; }
        public string ActorId { get; init; } = string.Empty;
        public string ActivationToken { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }

    private record LoginResult
    {
        public HttpStatusCode StatusCode { get; init; }
        public string AccessToken { get; init; } = string.Empty;
        public string ActorType { get; init; } = string.Empty;
    }

    private record InnovationCreationResult
    {
        public HttpStatusCode StatusCode { get; init; }
        public Guid InnovationId { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    private record InnovationSubmissionResult
    {
        public HttpStatusCode StatusCode { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? SubmittedAt { get; init; }
    }
}
