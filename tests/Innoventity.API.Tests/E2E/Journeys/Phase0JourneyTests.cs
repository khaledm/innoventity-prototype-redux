using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Tests.TestFixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Innoventity.API.Tests.E2E.Journeys;

/// <summary>
/// Phase 0 E2E Journey: Register → Activate → Login → View Innovation
/// Tests the complete user flow through API endpoints (full frontend E2E with Playwright deferred to Phase 7/T075)
/// </summary>
public class Phase0JourneyTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _testInnovationId = new Guid("22222222-2222-2222-2222-222222222222");

    public Phase0JourneyTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestInnovation();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // FIX (T002): Capture unique database name in closure BEFORE factory creation
        // This ensures all DbContext instances share the same in-memory database
        var databaseName = $"TestDb_Phase0Journey_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add JWT configuration for test environment
                    // Use values that MATCH appsettings.json to ensure token generation and validation agree
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
                        // FIX (T002): Use captured database name instead of inline Guid.NewGuid()
                        // This ensures constructor, test method, and WebApplicationFactory all share the same database
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    private void SeedTestInnovation()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed industries
        var electronicsIndustry = new Industry("ELEC-001") { Name = "Electronics" };
        var energyIndustry = new Industry("ENRG-001") { Name = "Renewable Energy" };
        context.Set<Industry>().AddRange(electronicsIndustry, energyIndustry);

        // Note: Actor will be created during registration step
        // Innovation will be seeded after actor creation

        context.SaveChanges();
    }

    [Fact]
    public async Task Phase0Journey_RegisterActivateLoginViewInnovation_Success()
    {
        var testEmail = $"journey-test-{Guid.NewGuid()}@innoventity.dev";

        // Step 1: Register
        var registerRequest = new
        {
            firstName = "Journey",
            lastName = "TestUser",
            email = testEmail,
            actorType = "IdeaGenerator",
            password = "Journey123!@#",
            contactAddress = new
            {
                address1 = "123 Journey Lane",
                city = "London",
                postCode = "SW1A 1AA",
                countryCode = "GB"
            }
        };

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerData = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var activationToken = registerData.GetProperty("activationToken").GetString();
        var actorId = Guid.Parse(registerData.GetProperty("actorId").GetString() ?? throw new InvalidOperationException("ActorId missing"));
        Assert.NotNull(activationToken);

        // Seed innovation owned by this actor
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure industries exist (they may not have been seeded in this test's database instance)
            var electronics = await context.Set<Industry>().FindAsync("ELEC-001");
            if (electronics == null)
            {
                electronics = new Industry("ELEC-001") { Name = "Electronics" };
                context.Set<Industry>().Add(electronics);
            }

            var energy = await context.Set<Industry>().FindAsync("ENRG-001");
            if (energy == null)
            {
                energy = new Industry("ENRG-001") { Name = "Renewable Energy" };
                context.Set<Industry>().Add(energy);
            }

            await context.SaveChangesAsync(); // Save industries first

            var testInnovation = new Innovation(_testInnovationId)
            {
                IdeaToken = new Guid("33333333-3333-3333-3333-333333333333"),
                OwnerId = actorId,
                IdeaSummary = new IdeaSummary
                {
                    Title = "Quantum Battery Prototype",
                    ProductType = "Energy Storage Device",
                    ResearchBackground = "Lithium-air battery leveraging quantum tunneling for 10x energy density improvement.",
                    ResearchCategory = ResearchCategory.Engineering,
                    IprStatus = "Patent Pending"
                },
                Product = new Product
                {
                    ProductDescription = "Next-generation battery technology for electric vehicles enabling 1000-mile range.",
                    TechnologyDescription = "Quantum tunneling mechanism enables unprecedented energy density through advanced material science",
                    TargetBeneficiaries = "Electric vehicle manufacturers, renewable energy storage providers, grid operators",
                    ProductAdvantages = "10x energy density, 50% faster charging time, 20-year operational lifespan",
                    DevelopmentPhase = "Prototype",
                    DevelopmentProcess = "Laboratory validation complete, seeking partners for commercial scale production",
                    ProductKeywords = "battery, energy storage, electric vehicle, quantum",
                    AdvantageKeywords = "energy density, fast charging, long lifespan"
                },
                Market = new Market
                {
                    TargetMarket = "Electric vehicle manufacturers, renewable energy storage systems",
                    TargetCustomerBase = "Automotive OEMs, grid-scale energy storage providers",
                    TargetCustomerType = "B2B"
                },
                CollaborationRequirement = new CollaborationRequirement
                {
                    PartnersNeeded = "RD,Manufacturing"
                },
                Status = InnovationStatus.Published,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                TargetIndustries = new List<Industry> { electronics, energy }
            };
            context.Set<Innovation>().Add(testInnovation);
            await context.SaveChangesAsync();
        }

        // Step 2: Activate
        var activateRequest = new
        {
            email = testEmail,
            token = activationToken
        };

        var activateResponse = await _client.PostAsJsonAsync("/auth/activate", activateRequest);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        // Step 3: Login
        var loginRequest = new
        {
            email = testEmail,
            actorType = "IdeaGenerator",
            password = "Journey123!@#"
        };

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginData.GetProperty("accessToken").GetString();
        Assert.NotNull(accessToken);

        // Step 4: View Innovation (use per-request token attachment - T003 fix)
        var innovationResponse = await _client.GetWithAuthAsync($"/innovations/{_testInnovationId}", accessToken);

        Assert.Equal(HttpStatusCode.OK, innovationResponse.StatusCode);
        var innovation = await innovationResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ideaSummary = innovation.GetProperty("ideaSummary");
        Assert.Equal("Quantum Battery Prototype", ideaSummary.GetProperty("title").GetString());
        Assert.Equal("Engineering", ideaSummary.GetProperty("researchCategory").GetString());
        Assert.Equal("Published", innovation.GetProperty("status").GetString());

        // Verify complete journey success
        Assert.True(true, "Phase 0 Journey completed successfully: Register → Activate → Login → View Innovation");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
