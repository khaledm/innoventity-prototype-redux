using System.ComponentModel.DataAnnotations;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Represents a platform actor (user) with specific role and account status.
/// Enforces business rules R1.1 (Account Activation), R1.2 (Actor Type Immutability),
/// and R1.3 (Email Uniqueness per ActorType).
/// </summary>
public class Actor
{
    /// <summary>
    /// Unique identifier for the actor
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Email address - serves as username equivalent (R1.3)
    /// Must be unique per ActorType
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the actor or organization
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Contact address for the actor
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ContactAddress { get; set; } = string.Empty;

    /// <summary>
    /// Actor role in the platform ecosystem (R1.2 - immutable after registration)
    /// </summary>
    [Required]
    public ActorType ActorType { get; set; }

    /// <summary>
    /// Current activation/access status (R1.1)
    /// All new users start with PendingActivation
    /// </summary>
    [Required]
    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingActivation;

    /// <summary>
    /// Temporary activation token for email verification (R1.1)
    /// Nullable - set to null after successful activation
    /// </summary>
    [MaxLength(64)]
    public string? ActivationToken { get; set; }

    /// <summary>
    /// BCrypt password hash (R8.4 - work factor 12)
    /// </summary>
    [Required]
    [MaxLength(60)]  // BCrypt hash is always 60 characters
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when actor account was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when actor account was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
