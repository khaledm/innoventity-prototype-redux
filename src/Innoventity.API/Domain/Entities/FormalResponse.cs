using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Common;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Abstract base for an actor's formal response to a published innovation (Spec 005 §R4.1).
/// Replaces the former flat <c>Bid</c> entity. Concrete subtypes (Manufacturing, SalesMarketing,
/// ResearchDevelopment, Investor) are persisted in a single table using TPH with a
/// <c>ResponseType</c> discriminator. Inherits identity-based equality from EntityOfGuid (R9.1).
/// </summary>
public abstract class FormalResponse : EntityOfGuid
{
    // Id inherited from EntityOfGuid

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    protected FormalResponse() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing.
    /// </summary>
    protected FormalResponse(Guid id) : base(id)
    {
    }

    /// <summary>
    /// The innovation this response targets (Spec §R4.1).
    /// </summary>
    [Required]
    public Guid InnovationId { get; set; }

    /// <summary>
    /// Navigation to the targeted innovation.
    /// </summary>
    public Innovation? Innovation { get; set; }

    /// <summary>
    /// The actor who submitted this response (Spec §R4.1).
    /// </summary>
    [Required]
    public Guid ActorId { get; set; }

    /// <summary>
    /// Navigation to the responding actor.
    /// </summary>
    public Actor? Actor { get; set; }

    /// <summary>
    /// Geographic region the actor proposes to operate in (Spec §R4.2).
    /// </summary>
    [Required]
    public GeographicRegion Location { get; set; }

    /// <summary>
    /// Short label for the kind of participation offered (Spec §R4.2).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string ParticipationType { get; set; }

    /// <summary>
    /// Free-text participation proposal. Business rule: minimum 100 characters
    /// (enforced in the endpoint handler, Spec FR-008 / clarification).
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public required string ParticipationProposal { get; set; }

    /// <summary>
    /// Lifecycle status (Spec §R4.x). Defaults to Pending on submission.
    /// </summary>
    [Required]
    public ResponseStatus Status { get; set; } = ResponseStatus.Pending;

    /// <summary>
    /// Submission timestamp.
    /// </summary>
    [Required]
    public DateTimeOffset SubmittedAt { get; set; }

    /// <summary>
    /// Timestamp of the most recent update, if any.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp set when the owner accepts this response during partner selection (Spec FR-016).
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }
}
