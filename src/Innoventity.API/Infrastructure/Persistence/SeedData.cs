using System.Security.Cryptography;
using Innoventity.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Infrastructure.Persistence;

/// <summary>
/// Seed data for testing and demo purposes
/// Implements spec.md §6 Test Data Requirements (Quantum Battery Prototype)
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Fixed GUIDs for deterministic testing (spec.md §6)
    /// </summary>
    private static readonly Guid TestActorId = new Guid("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestInnovationId = new Guid("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestIdeaToken = new Guid("33333333-3333-3333-3333-333333333333");

    /// <summary>
    /// Seed test data if not already present (idempotent)
    /// </summary>
    public static async Task SeedTestDataAsync(AppDbContext context)
    {
        // Check if test data already exists
        var testActorExists = await context.Actors.AnyAsync(a => a.Id == TestActorId);
        if (testActorExists)
        {
            return; // Data already seeded
        }

        // Seed Industries
        var electronicsIndustry = new Industry("ELEC-001")
        {
            Name = "Electronics"
        };

        var energyIndustry = new Industry("ENRG-001")
        {
            Name = "Renewable Energy"
        };

        context.Set<Industry>().AddRange(electronicsIndustry, energyIndustry);

        // Seed test actor (Dr. Sarah Chen)
        // Generate password salt (R8.5)
        var saltBytes = new byte[32];
        RandomNumberGenerator.Fill(saltBytes);
        var passwordSalt = Convert.ToBase64String(saltBytes);

        var testActor = new Actor(TestActorId)
        {
            FirstName = "Sarah",
            LastName = "Chen",
            Email = "test-generator@innoventity.dev",
            ActorType = ActorType.IdeaGenerator,
            ContactAddress = new Address
            {
                Address1 = "123 Innovation Drive",
                City = "Tech City",
                PostCode = "TC 12345",
                CountryCode = "US"
            },
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!@#", workFactor: 12),
            PasswordSalt = passwordSalt,
            AccountStatus = AccountStatus.Active,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        context.Actors.Add(testActor);

        // Seed Quantum Battery Prototype innovation
        var quantumBattery = new Innovation(TestInnovationId)
        {
            IdeaToken = TestIdeaToken,
            OwnerId = TestActorId,
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
            CreatedAt = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            SubmittedAt = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero),
            TargetIndustries = new List<Industry> { electronicsIndustry, energyIndustry }
        };

        context.Set<Innovation>().Add(quantumBattery);

        await context.SaveChangesAsync();
    }
}
