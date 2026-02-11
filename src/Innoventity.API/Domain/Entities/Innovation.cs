using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Common;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Innovation entity for Phase 0 - View Innovation only (Spec §6 Test Data Requirements)
/// Full innovation submission workflow deferred to Phase 1+
/// Inherits identity-based equality from EntityOfGuid (R9.1)
/// </summary>
public class Innovation : EntityOfGuid
{
    // Id inherited from EntityOfGuid

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    public Innovation() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing
    /// </summary>
    public Innovation(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Unique tracking token for innovation (Spec §R2.3)
    /// </summary>
    [Required]
    public Guid IdeaToken { get; set; }

    /// <summary>
    /// Owner actor ID (Spec §R2.2)
    /// </summary>
    [Required]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Innovation title (Spec §R2.1)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string Title { get; set; }

    /// <summary>
    /// Product type description (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string ProductType { get; set; }

    /// <summary>
    /// Research background description (Spec §R2.1)
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public required string ResearchBackground { get; set; }

    /// <summary>
    /// Research category classification (Spec §R3.3)
    /// </summary>
    [Required]
    public ResearchCategory ResearchCategory { get; set; }

    /// <summary>
    /// IPR status declaration (Spec §Journey 1 step 3)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string IprStatus { get; set; }

    /// <summary>
    /// Product description (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public required string ProductDescription { get; set; }

    /// <summary>
    /// Product advantages (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public required string ProductAdvantages { get; set; }

    /// <summary>
    /// Current development phase (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string DevelopmentPhase { get; set; }

    /// <summary>
    /// Development process description (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public required string DevelopmentProcess { get; set; }

    /// <summary>
    /// Target market description (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public required string TargetMarket { get; set; }

    /// <summary>
    /// Target customer base (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public required string TargetCustomerBase { get; set; }

    /// <summary>
    /// Target customer type (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string TargetCustomerType { get; set; }

    /// <summary>
    /// Product keywords for discoverability (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public required string ProductKeywords { get; set; }

    /// <summary>
    /// Advantage keywords (Spec §6)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public required string AdvantageKeywords { get; set; }

    /// <summary>
    /// Innovation status (Spec §R3.1)
    /// </summary>
    [Required]
    public InnovationStatus Status { get; set; }

    /// <summary>
    /// Creation timestamp (Spec §6)
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Submission timestamp when published (Spec §6)
    /// </summary>
    public DateTimeOffset? SubmittedAt { get; set; }

    /// <summary>
    /// Navigation property to owner actor
    /// </summary>
    public Actor? Owner { get; set; }

    /// <summary>
    /// Target industries for this innovation (many-to-many relationship)
    /// </summary>
    public ICollection<Industry> TargetIndustries { get; set; } = new List<Industry>();
}
