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
    }
}
