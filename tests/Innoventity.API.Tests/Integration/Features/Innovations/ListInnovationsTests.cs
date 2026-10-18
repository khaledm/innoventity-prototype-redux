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

namespace Innoventity.API.Tests.Integration.Features.Innovations;

/// <summary>
/// Integration tests for GET /innovations endpoint (T008)
/// Tests innovation discovery with filtering and pagination
/// </summary>
public class ListInnovationsTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _ideaGeneratorId = new Guid("11111111-1111-1111-1111-111111111111");
    private readonly Guid _manufacturingActorId = new Guid("22222222-2222-2222-2222-222222222222");
    private readonly Guid _electronicsInnovation1Id = new Guid("33333333-3333-3333-3333-333333333333");
    private readonly Guid _electronicsInnovation2Id = new Guid("44444444-4444-4444-4444-444444444444");
    private readonly Guid _healthcareInnovationId = new Guid("55555555-5555-5555-5555-555555555555");
    private readonly Guid _draftInnovationId = new Guid("66666666-6666-6666-6666-666666666666");

    public ListInnovationsTests()
    {
        _factory = CreateFactory();
        _client = _factory.CreateClient();
        SeedTestData();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // Generate unique database name once for this factory instance (T002 fix)
        var databaseName = $"TestDb_ListInnovations_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add JWT configuration matching appsettings.Development.json (T003 fix)
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
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database with unique name for isolation (captured in closure - T002)
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Infrastructure.Authentication.PasswordHasher>();

        // Seed Idea Generator actor
        var ideaGenerator = new Actor(_ideaGeneratorId)
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "test-generator@innoventity.dev",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("Test123!@#"),
            PasswordSalt = "somesalt",
            ActivationToken = null,
            ContactAddress = new Address
            {
                Address1 = "123 Test St",
                City = "Test City",
                PostCode = "12345",
                CountryCode = "US"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Actors.Add(ideaGenerator);

        // Seed Manufacturing actor
        var manufacturingActor = new Actor(_manufacturingActorId)
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "test-manufacturing@innoventity.dev",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("Test123!@#"),
            PasswordSalt = "somesalt",
            ActivationToken = null,
            ContactAddress = new Address
            {
                Address1 = "456 Factory Rd",
                City = "Manufacturing City",
                PostCode = "54321",
                CountryCode = "US"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Actors.Add(manufacturingActor);

        // Seed industries
        var electronics = new Industry("ELEC-001") { Name = "Electronics" };
        var renewableEnergy = new Industry("ENRG-001") { Name = "Renewable Energy" };
        var healthcare = new Industry("HLTH-001") { Name = "Healthcare" };
        context.Industries.AddRange(electronics, renewableEnergy, healthcare);

        // Seed published Electronics innovation 1
        var electronicsInnovation1 = new Innovation(_electronicsInnovation1Id)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            IdeaSummary = new IdeaSummary
            {
                Title = "Quantum Battery Prototype",
                ProductType = "Energy Storage Device",
                ResearchBackground = "Lithium-air battery leveraging quantum tunneling",
                ResearchCategory = ResearchCategory.Engineering,
                IprStatus = "Patent Pending"
            },
            Product = new Product
            {
                ProductDescription = "Next-generation battery technology",
                TechnologyDescription = "Quantum-enhanced battery materials",
                TargetBeneficiaries = "EV manufacturers and renewable energy storage providers",
                ProductAdvantages = "High energy density",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "R&D phase",
                ProductKeywords = "battery, quantum, energy",
                AdvantageKeywords = "efficient, high-capacity"
            },
            Market = new Market
            {
                TargetMarket = "EV manufacturers",
                TargetCustomerBase = "Automotive OEMs",
                TargetCustomerType = "B2B"
            },
            CollaborationRequirement = new CollaborationRequirement
            {
                PartnersNeeded = "RD,Manufacturing"
            },
            Status = InnovationStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-4)
        };
        electronicsInnovation1.TargetIndustries.Add(electronics);
        electronicsInnovation1.TargetIndustries.Add(renewableEnergy);
        context.Innovations.Add(electronicsInnovation1);

        // Seed published Electronics innovation 2
        var electronicsInnovation2 = new Innovation(_electronicsInnovation2Id)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            IdeaSummary = new IdeaSummary
            {
                Title = "Smart Circuit Optimizer",
                ProductType = "Electronic Component",
                ResearchBackground = "AI-powered circuit design optimization for reduced power consumption",
                ResearchCategory = ResearchCategory.Engineering,
                IprStatus = "Patent Pending"
            },
            Product = new Product
            {
                ProductDescription = "Circuit optimization software",
                TechnologyDescription = "AI-powered circuit design optimization",
                TargetBeneficiaries = "Electronics manufacturers and circuit designers",
                ProductAdvantages = "Reduces power by 30%",
                DevelopmentPhase = "Beta",
                DevelopmentProcess = "Software development",
                ProductKeywords = "circuit, AI, optimization",
                AdvantageKeywords = "efficient, smart"
            },
            Market = new Market
            {
                TargetMarket = "Electronics manufacturers",
                TargetCustomerBase = "Circuit designers",
                TargetCustomerType = "B2B"
            },
            CollaborationRequirement = new CollaborationRequirement
            {
                PartnersNeeded = "SalesMarketing"
            },
            Status = InnovationStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        electronicsInnovation2.TargetIndustries.Add(electronics);
        context.Innovations.Add(electronicsInnovation2);

        // Seed published Healthcare innovation (NaturalScience category)
        var healthcareInnovation = new Innovation(_healthcareInnovationId)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            IdeaSummary = new IdeaSummary
            {
                Title = "Rapid Diagnostic Test Kit",
                ProductType = "Medical Device",
                ResearchBackground = "Novel biomarker detection for early disease diagnosis",
                ResearchCategory = ResearchCategory.NaturalScience,
                IprStatus = "Patent Pending"
            },
            Product = new Product
            {
                ProductDescription = "Point-of-care diagnostic device",
                TechnologyDescription = "Novel biomarker detection technology",
                TargetBeneficiaries = "Healthcare providers, hospitals, and patients",
                ProductAdvantages = "Results in 5 minutes",
                DevelopmentPhase = "Clinical trials",
                DevelopmentProcess = "FDA approval process",
                ProductKeywords = "diagnostic, rapid, biomarker",
                AdvantageKeywords = "fast, accurate"
            },
            Market = new Market
            {
                TargetMarket = "Healthcare providers",
                TargetCustomerBase = "Hospitals and clinics",
                TargetCustomerType = "B2B"
            },
            CollaborationRequirement = new CollaborationRequirement
            {
                PartnersNeeded = "Investor"
            },
            Status = InnovationStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        healthcareInnovation.TargetIndustries.Add(healthcare);
        context.Innovations.Add(healthcareInnovation);

        // Seed draft innovation (should NOT appear in results)
        var draftInnovation = new Innovation(_draftInnovationId)
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = _ideaGeneratorId,
            IdeaSummary = new IdeaSummary
            {
                Title = "Draft Innovation - Should Not Appear",
                ProductType = "Draft Product",
                ResearchBackground = "This is still a draft",
                ResearchCategory = ResearchCategory.Management,
                IprStatus = "None"
            },
            Product = new Product
            {
                ProductDescription = "Draft description",
                TechnologyDescription = "Draft technology",
                TargetBeneficiaries = "Draft beneficiaries",
                ProductAdvantages = "Draft advantages",
                DevelopmentPhase = "Concept",
                DevelopmentProcess = "Planning",
                ProductKeywords = "draft",
                AdvantageKeywords = "none"
            },
            Market = new Market
            {
                TargetMarket = "TBD",
                TargetCustomerBase = "TBD",
                TargetCustomerType = "B2B"
            },
            CollaborationRequirement = new CollaborationRequirement(),
            Status = InnovationStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            SubmittedAt = null
        };
        context.Innovations.Add(draftInnovation);

        context.SaveChanges();
    }

    private async Task<string> GetAccessToken(string email, string actorType, string password = "Test123!@#")
    {
        var loginRequest = new
        {
            email,
            actorType,
            password
        };

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with status {response.StatusCode}: {errorContent}");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        return loginResponse.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Failed to get access token");
    }

    /// <summary>
    /// Test 1: No filter returns all published innovations (not drafts)
    /// Spec §US4 Acceptance Scenario 2: "GET /innovations (no filters), Then system returns all published innovations (not drafts)"
    /// </summary>
    [Fact]
    public async Task ListInnovations_NoFilter_ReturnsAllPublished()
    {
        // Arrange
        var token = await GetAccessToken("test-manufacturing@innoventity.dev", "Manufacturing");

        // Act
        var response = await _client.GetWithAuthAsync("/innovations", token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        var items = root.GetProperty("items");
        Assert.Equal(3, items.GetArrayLength()); // 3 published innovations (draft excluded)

        // Verify no drafts in results
        foreach (var item in items.EnumerateArray())
        {
            Assert.Equal("Published", item.GetProperty("status").GetString());
        }
    }

    /// <summary>
    /// Test 2: Filter by industryId returns only matching innovations
    /// Spec §US4 Acceptance Scenario 1: "GET /innovations?industryId=ELEC-001, Then system returns 2 Electronics innovations only"
    /// </summary>
    [Fact]
    public async Task ListInnovations_FilterByIndustry_ReturnsMatched()
    {
        // Arrange
        var token = await GetAccessToken("test-manufacturing@innoventity.dev", "Manufacturing");

        // Act
        var response = await _client.GetWithAuthAsync("/innovations?industryId=ELEC-001", token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        var items = root.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength()); // 2 Electronics innovations

        // Verify all items have Electronics industry
        foreach (var item in items.EnumerateArray())
        {
            var targetIndustries = item.GetProperty("targetIndustries");
            bool hasElectronics = false;
            foreach (var industry in targetIndustries.EnumerateArray())
            {
                if (industry.GetString() == "Electronics")
                {
                    hasElectronics = true;
                    break;
                }
            }
            Assert.True(hasElectronics, "Expected all innovations to have Electronics industry");
        }
    }

    /// <summary>
    /// Test 3: Filter by researchCategory returns only matching innovations
    /// Spec §US4 Acceptance Scenario 3: "GET /innovations?researchCategory=Engineering, Then system returns only Engineering category innovations"
    /// </summary>
    [Fact]
    public async Task ListInnovations_FilterByResearchCategory_ReturnsMatched()
    {
        // Arrange
        var token = await GetAccessToken("test-manufacturing@innoventity.dev", "Manufacturing");

        // Act
        var response = await _client.GetWithAuthAsync("/innovations?researchCategory=Engineering", token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        var items = root.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength()); // 2 Engineering innovations

        // Verify all items are Engineering category
        foreach (var item in items.EnumerateArray())
        {
            Assert.Equal("Engineering", item.GetProperty("ideaSummary").GetProperty("researchCategory").GetString());
        }
    }

    /// <summary>
    /// Test 4: Pagination works correctly
    /// T008 Acceptance Criteria: "Pagination works correctly (page 1 vs page 2 returns different items)"
    /// </summary>
    [Fact]
    public async Task ListInnovations_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var token = await GetAccessToken("test-manufacturing@innoventity.dev", "Manufacturing");

        // Act - Get page 1 with pageSize=2
        var responsePage1 = await _client.GetWithAuthAsync("/innovations?page=1&pageSize=2", token);
        var contentPage1 = await responsePage1.Content.ReadAsStringAsync();
        var jsonDocPage1 = JsonDocument.Parse(contentPage1);
        var rootPage1 = jsonDocPage1.RootElement;

        // Act - Get page 2 with pageSize=2
        var responsePage2 = await _client.GetWithAuthAsync("/innovations?page=2&pageSize=2", token);
        var contentPage2 = await responsePage2.Content.ReadAsStringAsync();
        var jsonDocPage2 = JsonDocument.Parse(contentPage2);
        var rootPage2 = jsonDocPage2.RootElement;

        // Assert
        Assert.Equal(HttpStatusCode.OK, responsePage1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responsePage2.StatusCode);

        var itemsPage1 = rootPage1.GetProperty("items");
        var itemsPage2 = rootPage2.GetProperty("items");

        Assert.Equal(2, itemsPage1.GetArrayLength()); // Page 1 has 2 items
        Assert.Equal(1, itemsPage2.GetArrayLength()); // Page 2 has 1 item (total 3)

        // Verify different items on different pages
        var id1 = itemsPage1[0].GetProperty("innovationId").GetString();
        var id2Page2 = itemsPage2[0].GetProperty("innovationId").GetString();
        Assert.NotEqual(id1, id2Page2);

        // Verify pagination metadata
        Assert.Equal(3, rootPage1.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, rootPage1.GetProperty("page").GetInt32());
        Assert.Equal(2, rootPage1.GetProperty("pageSize").GetInt32());
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
