using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
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
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SigningKey"] = "test-signing-key-minimum-32-characters-required-for-hs256",
                        ["Jwt:Issuer"] = "test-issuer",
                        ["Jwt:Audience"] = "test-audience",
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
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });
    }

    private void SeedTestInnovation()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed industries
        var electronicsIndustry = new Industry { IndustryId = "ELEC-001", Name = "Electronics" };
        var energyIndustry = new Industry { IndustryId = "ENRG-001", Name = "Renewable Energy" };
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
            FullName = "Journey Test User",
            Email = testEmail,
            ActorType = "IdeaGenerator",
            Password = "Journey123!@#",
            ContactAddress = "123 Journey Lane"
        };

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var registerData = await registerResponse.Content.ReadFromJsonAsync<dynamic>();
        var activationToken = registerData?.activationToken?.ToString();
        var actorId = Guid.Parse(registerData?.actorId?.ToString() ?? throw new InvalidOperationException("ActorId missing"));
        Assert.NotNull(activationToken);

        // Seed innovation owned by this actor
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var electronics = await context.Set<Industry>().FindAsync("ELEC-001");
            var energy = await context.Set<Industry>().FindAsync("ENRG-001");

            var testInnovation = new Innovation
            {
                Id = _testInnovationId,
                IdeaToken = new Guid("33333333-3333-3333-3333-333333333333"),
                OwnerId = actorId,
                Title = "Quantum Battery Prototype",
                ProductType = "Energy Storage Device",
                ResearchBackground = "Lithium-air battery leveraging quantum tunneling for 10x energy density improvement.",
                ResearchCategory = ResearchCategory.Engineering,
                IprStatus = "Patent Pending",
                ProductDescription = "Next-generation battery technology for electric vehicles enabling 1000-mile range.",
                ProductAdvantages = "10x energy density, 50% faster charging time, 20-year operational lifespan",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Laboratory validation complete, seeking partners for commercial scale production",
                TargetMarket = "Electric vehicle manufacturers, renewable energy storage systems",
                TargetCustomerBase = "Automotive OEMs, grid-scale energy storage providers",
                TargetCustomerType = "B2B",
                ProductKeywords = "battery, energy storage, electric vehicle, quantum",
                AdvantageKeywords = "energy density, fast charging, long lifespan",
                Status = InnovationStatus.Published,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                TargetIndustries = new List<Industry> { electronics!, energy! }
            };
            context.Set<Innovation>().Add(testInnovation);
            await context.SaveChangesAsync();
        }

        // Step 2: Activate
        var activateRequest = new
        {
            ActivationToken = activationToken
        };

        var activateResponse = await _client.PostAsJsonAsync("/auth/activate", activateRequest);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        // Step 3: Login
        var loginRequest = new
        {
            Email = testEmail,
            ActorType = "IdeaGenerator",
            Password = "Journey123!@#"
        };

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginData = await loginResponse.Content.ReadFromJsonAsync<dynamic>();
        var accessToken = loginData?.accessToken?.ToString();
        Assert.NotNull(accessToken);

        // Step 4: View Innovation
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var innovationResponse = await _client.GetAsync($"/innovations/{_testInnovationId}");

        Assert.Equal(HttpStatusCode.OK, innovationResponse.StatusCode);
        var innovation = await innovationResponse.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(innovation);
        Assert.Equal("Quantum Battery Prototype", innovation?.title?.ToString());
        Assert.Equal("Engineering", innovation?.researchCategory?.ToString());
        Assert.Equal("Published", innovation?.status?.ToString());

        // Verify complete journey success
        Assert.True(true, "Phase 0 Journey completed successfully: Register → Activate → Login → View Innovation");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
