using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Common;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Represents a platform actor (user) with specific role and account status.
/// Enforces business rules R1.1 (Account Activation), R1.2 (Actor Type Immutability),
/// and R1.3 (Email Uniqueness per ActorType).
/// Inherits identity-based equality from EntityOfGuid (R9.1)
/// </summary>
public class Actor : EntityOfGuid
{
    // Id inherited from EntityOfGuid

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    public Actor() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing
    /// </summary>
    public Actor(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Email address - serves as username equivalent (R1.3)
    /// Must be unique per ActorType
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name of the actor (R1.4)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the actor (R1.4)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Structured contact address (R1.5)
    /// Stored as owned entity with columns ContactAddress_*
    /// Nullable - address is optional
    /// </summary>
    public Address? ContactAddress { get; set; }

    /// <summary>
    /// Contact phone number (optional)
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

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
    /// Password salt for enhanced security (R8.5)
    /// Base64-encoded 32-byte random salt (44 characters)
    /// </summary>
    [Required]
    [MaxLength(44)]
    public string PasswordSalt { get; set; } = string.Empty;

    // Lockout policy constants (R8.4)
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Number of consecutive failed login attempts (R8.4).
    /// Incremented on each wrong password; reset to 0 on successful login.
    /// </summary>
    public int FailedLoginAttempts { get; private set; } = 0;

    /// <summary>
    /// Account lockout expiry timestamp (R8.4).
    /// Null when not locked. Set to UtcNow + 15 minutes when FailedLoginAttempts reaches 5.
    /// Login is blocked while LockoutUntil > UtcNow.
    /// </summary>
    public DateTimeOffset? LockoutUntil { get; private set; }

    /// <summary>Returns the current number of consecutive failed login attempts.</summary>
    public int GetFailedLoginAttempts() => FailedLoginAttempts;

    /// <summary>Returns the maximum number of failed attempts before lockout (R8.4: 5).</summary>
    public int GetMaxAllowedFailedCount() => MaxFailedAttempts;

    /// <summary>
    /// Returns true when the account is actively locked out at the given point in time (R8.4).
    /// </summary>
    public bool IsAccountLocked(DateTimeOffset now) =>
        LockoutUntil.HasValue && LockoutUntil.Value > now;

    /// <summary>
    /// Records one failed login attempt. Sets LockoutUntil when the threshold is reached (R8.4).
    /// </summary>
    public void IncrementFailedLoginAttemptCount(DateTimeOffset now)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedAttempts)
            LockoutUntil = now.Add(LockoutDuration);
    }

    /// <summary>
    /// Resets lockout state after a successful login (R8.4).
    /// </summary>
    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockoutUntil = null;
    }

    /// <summary>
    /// Timestamp when actor account was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when actor account was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Computed property for display name (FirstName LastName)
    /// Not stored in database
    /// </summary>
    public string DisplayName => $"{FirstName} {LastName}";

    /// <summary>
    /// Computed property for sortable name (LastName, FirstName)
    /// Not stored in database
    /// </summary>
    public string SortableName => $"{LastName}, {FirstName}";
}
