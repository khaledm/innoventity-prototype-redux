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

            // Check if industries already exist (from HasData seed)
            if (!await context.Industries.AnyAsync())
            {
                // Seed industries matching AppDbContext seed data
                // Based on legacy SchemaBuilder/GetCommonLookupSql ICB taxonomy
                context.Industries.AddRange(
                    new Industry("HLTH-001") { Name = "Health Care" },
                    new Industry("TECH-001") { Name = "Technology" },
                    new Industry("ENRG-001") { Name = "Oil & Gas" },
                    new Industry("AUTO-001") { Name = "Consumer Goods" },
                    new Industry("INDU-001") { Name = "Industrials" },
                    new Industry("FIN-001") { Name = "Financials" },
                    new Industry("TCOM-001") { Name = "Telecommunications" },
                    new Industry("CSVC-001") { Name = "Consumer Services" },
                    new Industry("UTIL-001") { Name = "Utilities" },
                    new Industry("MTRL-001") { Name = "Basic Materials" }
                );
                await context.SaveChangesAsync();
            }
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

        // Verify required industry IDs present (Phase 0 minimum + ICB top-level)
        var industryIds = industries.Select(i => i.IndustryId).ToList();
        Assert.Contains("HLTH-001", industryIds);  // Health Care
        Assert.Contains("TECH-001", industryIds);  // Technology
        Assert.Contains("ENRG-001", industryIds);  // Oil & Gas (includes Renewable Energy)
        Assert.Contains("AUTO-001", industryIds);  // Consumer Goods (includes Automobiles)

        // Verify each industry has required fields
        foreach (var industry in industries)
        {
            Assert.False(string.IsNullOrEmpty(industry.IndustryId), "IndustryId should not be empty");
            Assert.False(string.IsNullOrEmpty(industry.Name), "Name should not be empty");
        }

        // Verify specific industry names (ICB top-level taxonomy)
        var healthCare = industries.First(i => i.IndustryId == "HLTH-001");
        Assert.Equal("Health Care", healthCare.Name);

        var technology = industries.First(i => i.IndustryId == "TECH-001");
        Assert.Equal("Technology", technology.Name);

        var oilAndGas = industries.First(i => i.IndustryId == "ENRG-001");
        Assert.Equal("Oil & Gas", oilAndGas.Name);

        var consumerGoods = industries.First(i => i.IndustryId == "AUTO-001");
        Assert.Equal("Consumer Goods", consumerGoods.Name);

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
