using Innoventity.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Actors (users) registered in the platform
    /// </summary>
    public DbSet<Actor> Actors => Set<Actor>();

    /// <summary>
    /// Innovations submitted by IdeaGenerators (Phase 0: read-only, future phases: full CRUD)
    /// </summary>
    public DbSet<Innovation> Innovations => Set<Innovation>();

    /// <summary>
    /// Industries for innovation targeting and actor affiliation
    /// </summary>
    public DbSet<Industry> Industries => Set<Industry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Actor entity
        modelBuilder.Entity<Actor>(entity =>
        {
            // R1.3: Email must be unique per ActorType
            entity.HasIndex(a => new { a.Email, a.ActorType })
                  .IsUnique()
                  .HasDatabaseName("IX_Actor_Email_ActorType");

            // Configure string lengths and required fields
            entity.Property(a => a.Email)
                  .IsRequired()
                  .HasMaxLength(255);

            // R1.4: FirstName/LastName (replacing FullName)
            entity.Property(a => a.FirstName)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(a => a.LastName)
                  .IsRequired()
                  .HasMaxLength(50);

            // Password security (R8.4, R8.5)
            entity.Property(a => a.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(60); // BCrypt hash length

            entity.Property(a => a.PasswordSalt)
                  .IsRequired()
                  .HasMaxLength(44); // Base64 encoded 32-byte salt

            // Phone (optional, separate from address)
            entity.Property(a => a.Phone)
                  .HasMaxLength(20);

            // R1.5: Configure Address as owned entity
            entity.OwnsOne(a => a.ContactAddress, address =>
            {
                address.Property(ad => ad.Address1)
                       .HasColumnName("ContactAddress_Address1")
                       .IsRequired()
                       .HasMaxLength(100);

                address.Property(ad => ad.Address2)
                       .HasColumnName("ContactAddress_Address2")
                       .HasMaxLength(100);

                address.Property(ad => ad.City)
                       .HasColumnName("ContactAddress_City")
                       .IsRequired()
                       .HasMaxLength(50);

                address.Property(ad => ad.PostCode)
                       .HasColumnName("ContactAddress_PostCode")
                       .IsRequired()
                       .HasMaxLength(20);

                address.Property(ad => ad.CountryCode)
                       .HasColumnName("ContactAddress_CountryCode")
                       .IsRequired()
                       .HasMaxLength(2)
                       .IsFixedLength();
            });

            entity.Property(a => a.ActivationToken)
                  .HasMaxLength(64);

            entity.Property(a => a.ActorType)
                  .IsRequired()
                  .HasConversion<string>();

            entity.Property(a => a.AccountStatus)
                  .IsRequired()
                  .HasConversion<string>();

            // Set default values for timestamps
            entity.Property(a => a.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(a => a.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Computed properties are not stored (DisplayName, SortableName)
            entity.Ignore(a => a.DisplayName);
            entity.Ignore(a => a.SortableName);
        });

        // Configure Innovation entity
        modelBuilder.Entity<Innovation>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(i => i.ProductType)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(i => i.ResearchBackground)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(i => i.ResearchCategory)
                  .IsRequired()
                  .HasConversion<string>();

            entity.Property(i => i.IprStatus)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(i => i.ProductDescription)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(i => i.ProductAdvantages)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(i => i.DevelopmentPhase)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(i => i.DevelopmentProcess)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(i => i.TargetMarket)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(i => i.TargetCustomerBase)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(i => i.TargetCustomerType)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(i => i.ProductKeywords)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(i => i.AdvantageKeywords)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(i => i.Status)
                  .IsRequired()
                  .HasConversion<string>();

            entity.Property(i => i.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Configure relationship with Actor (Owner)
            entity.HasOne(i => i.Owner)
                  .WithMany()
                  .HasForeignKey(i => i.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Configure many-to-many relationship with Industry
            entity.HasMany(i => i.TargetIndustries)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "InnovationIndustry",
                      j => j.HasOne<Industry>()
                            .WithMany()
                            .HasForeignKey("IndustryId")
                            .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Innovation>()
                            .WithMany()
                            .HasForeignKey("InnovationId")
                            .OnDelete(DeleteBehavior.Cascade));
        });

        // Configure Industry entity
        modelBuilder.Entity<Industry>(entity =>
        {
            entity.HasKey(i => i.Id);  // Renamed from IndustryId (R9.1)

            entity.Property(i => i.Id)  // Renamed from IndustryId (R9.1)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(i => i.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            // Seed industry master list (Phase 0 - flat structure)
            // Based on legacy SchemaBuilder/GetCommonLookupSql ICB taxonomy
            // Future: Expand to hierarchical SuperSector → Sector → Subsector structure
            entity.HasData(
                // Core industries (Phase 0 minimum requirement)
                new Industry("HLTH-001") { Name = "Health Care" },
                new Industry("TECH-001") { Name = "Technology" },
                new Industry("ENRG-001") { Name = "Oil & Gas" },  // Includes Renewable Energy subsector
                new Industry("AUTO-001") { Name = "Consumer Goods" },  // Includes Automobiles subsector

                // Additional industries (production completeness)
                new Industry("INDU-001") { Name = "Industrials" },
                new Industry("FIN-001") { Name = "Financials" },
                new Industry("TCOM-001") { Name = "Telecommunications" },
                new Industry("CSVC-001") { Name = "Consumer Services" },
                new Industry("UTIL-001") { Name = "Utilities" },
                new Industry("MTRL-001") { Name = "Basic Materials" }
            );
        });
    }
}
