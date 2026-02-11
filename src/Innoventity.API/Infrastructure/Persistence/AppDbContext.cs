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

            entity.Property(a => a.FullName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(a => a.ContactAddress)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(a => a.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(60); // BCrypt hash length

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
            entity.HasKey(i => i.IndustryId);

            entity.Property(i => i.IndustryId)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(i => i.Name)
                  .IsRequired()
                  .HasMaxLength(200);
        });
    }
}
