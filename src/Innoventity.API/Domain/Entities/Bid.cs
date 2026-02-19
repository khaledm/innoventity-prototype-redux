using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Common;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Represents a partnership proposal submitted by an actor for an innovation.
/// Enforces business rules R4.1 (Bid Eligibility) and R4.2 (Proposal Requirements).
/// Inherits identity-based equality from EntityOfGuid (R9.1)
/// </summary>
public class Bid : EntityOfGuid
{
    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    public Bid() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing
    /// </summary>
    public Bid(Guid id) : base(id)
    {
    }

    /// <summary>
    /// The innovation this bid is associated with (R4.1)
    /// </summary>
    [Required]
    public Guid InnovationId { get; set; }

    /// <summary>
    /// Navigation property to Innovation
    /// </summary>
    public Innovation? Innovation { get; set; }

    /// <summary>
    /// The actor submitting the bid (R4.1 - cannot bid on own innovation)
    /// </summary>
    [Required]
    public Guid ActorId { get; set; }

    /// <summary>
    /// Navigation property to Actor
    /// </summary>
    public Actor? Actor { get; set; }

    /// <summary>
    /// Geographic location of the bidding organization (R4.2)
    /// Example: "Munich, Germany", "San Francisco, CA, USA"
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Type of partnership being proposed (R4.2)
    /// Example: "Manufacturing Partner", "R&amp;D Collaboration", "Distribution Partner"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ParticipationType { get; set; } = string.Empty;

    /// <summary>
    /// Detailed partnership proposal (R4.2 - minimum 200 characters)
    /// Must explain capabilities, experience, and value proposition
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public string ParticipationProposal { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the bid (Pending, Accepted, Rejected)
    /// All new bids start with Pending status
    /// </summary>
    [Required]
    public BidStatus Status { get; set; } = BidStatus.Pending;

    /// <summary>
    /// Timestamp when the bid was submitted
    /// </summary>
    [Required]
    public DateTimeOffset SubmittedAt { get; set; }

    /// <summary>
    /// Timestamp when the bid was last updated
    /// Null if never updated after submission
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp when the bid was accepted by the innovation owner
    /// Null if still pending or rejected
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }
}
