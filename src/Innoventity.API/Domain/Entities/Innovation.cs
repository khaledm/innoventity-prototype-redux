using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Common;

namespace Innoventity.API.Domain.Entities;

public class Innovation : EntityOfGuid
{
    public Innovation() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing
    /// </summary>
    public Innovation(Guid id) : base(id)
    {
    }

    public IdeaSummary IdeaSummary { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public Market Market { get; set; } = null!;

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

    /// <summary>
    /// Formal responses submitted for this innovation (one-to-many inverse navigation).
    /// Replaces the former <c>Bids</c> collection (Spec 005).
    /// </summary>
    public ICollection<FormalResponse> FormalResponses { get; set; } = [];

    /// <summary>
    /// Partners needed: RD, Manufacturing, SalesMarketing, Investor (Spec §US3 R2.1)
    /// Stored as comma-separated string
    /// </summary>
    public CollaborationRequirement CollaborationRequirement { get; set; } = null!;

    /// <summary>
    /// Timestamp when partner selection was completed (Spec 004 FR-007)
    /// Non-null means selection is final and immutable.
    /// </summary>
    public DateTimeOffset? PartnerSelectionCompletedOn { get; set; }

    /// <summary>
    /// Actor who completed partner selection (Spec 004 NFR-003 audit trail)
    /// </summary>
    public Guid? SelectedByActorId { get; set; }

    public bool IsIdeaSummaryComplete() => IdeaSummary.IsComplete();

    public bool IsProductDetailsComplete() => Product.IsComplete();

    public bool IsMarketDetailsSectionComplete() =>
        Market.IsComplete() && TargetIndustries.Count > 0;

    public bool IsReadyForSubmission() =>
        IsIdeaSummaryComplete() &&
        IsProductDetailsComplete() &&
        IsMarketDetailsSectionComplete() &&
        !string.IsNullOrWhiteSpace(CollaborationRequirement.PartnersNeeded);
}
