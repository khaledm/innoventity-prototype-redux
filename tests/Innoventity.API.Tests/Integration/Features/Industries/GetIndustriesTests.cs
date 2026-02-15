using System.Net;
using System.Net.Http.Json;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Innoventity.API.Tests.Integration.Features.Industries;

/// <summary>
/// Integration tests for GET /industries endpoint (T009)
/// Spec §US4 Innovation Discovery - Industry Master List
/// Tests reference data retrieval (no authentication required)
/// </summary>
public class GetIndustriesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetIndustriesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Test 1: GET /industries returns all industries
    /// Spec §T009 Acceptance Criteria:
    /// - Returns ≥4 industries
    /// - Response includes industryId and name for each industry
    /// - No authentication required (public reference data)
    /// - Specific IDs present: ELEC-001, ENRG-001, AUTO-001, HLTH-001
    /// </summary>
    [Fact]
    public async Task GetIndustries_ReturnsAllIndustries()
    {
        // Arrange: Create test-specific database to avoid conflicts with other tests
        var testDbName = $"TestDb_GetIndustries_{Guid.NewGuid()}";
        
        var testFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext configuration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add test-specific in-memory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={testDbName};Trusted_Connection=True;MultipleActiveResultSets=true");
                });
            });
        });

        var testClient = testFactory.CreateClient();

        // Seed industries in test database
        using (var scope = testFactory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();

            // Seed required industries (T009 specification)
            context.Industries.AddRange(
                new Industry("ELEC-001") { Name = "Electronics" },
                new Industry("ENRG-001") { Name = "Renewable Energy" },
                new Industry("AUTO-001") { Name = "Automotive" },
                new Industry("HLTH-001") { Name = "Healthcare" },
                new Industry("FIN-001") { Name = "Finance" },
                new Industry("AGRI-001") { Name = "Agriculture" },
                new Industry("MFG-001") { Name = "Manufacturing" },
                new Industry("IT-001") { Name = "IT Services" }
            );
            await context.SaveChangesAsync();
        }

        // Act: Request industries without authentication
        var response = await testClient.GetAsync("/industries");

        // Assert: Response is 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Deserialize response
        var industries = await response.Content.ReadFromJsonAsync<List<IndustryResponse>>();
        Assert.NotNull(industries);

        // Verify ≥4 industries returned
        Assert.True(industries.Count >= 4, $"Expected at least 4 industries, got {industries.Count}");

        // Verify required industry IDs present
        var industryIds = industries.Select(i => i.IndustryId).ToList();
        Assert.Contains("ELEC-001", industryIds);
        Assert.Contains("ENRG-001", industryIds);
        Assert.Contains("AUTO-001", industryIds);
        Assert.Contains("HLTH-001", industryIds);

        // Verify each industry has required fields
        foreach (var industry in industries)
        {
            Assert.False(string.IsNullOrEmpty(industry.IndustryId), "IndustryId should not be empty");
            Assert.False(string.IsNullOrEmpty(industry.Name), "Name should not be empty");
        }

        // Verify specific industry names
        var electronics = industries.First(i => i.IndustryId == "ELEC-001");
        Assert.Equal("Electronics", electronics.Name);

        var renewableEnergy = industries.First(i => i.IndustryId == "ENRG-001");
        Assert.Equal("Renewable Energy", renewableEnergy.Name);

        var automotive = industries.First(i => i.IndustryId == "AUTO-001");
        Assert.Equal("Automotive", automotive.Name);

        var healthcare = industries.First(i => i.IndustryId == "HLTH-001");
        Assert.Equal("Healthcare", healthcare.Name);

        // Cleanup: Delete test database
        using (var scope = testFactory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// Response DTO matching GET /industries endpoint contract
    /// </summary>
    private record IndustryResponse(
        string IndustryId,
        string Name
    );
}
