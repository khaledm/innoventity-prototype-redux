using System.ComponentModel.DataAnnotations;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Investor actor's lightweight feedback response (Spec 005 §R4.x). Carries no financial
/// projection — only structured feedback. Persisted with discriminator
/// <c>ResponseType = "InvestorResponse"</c>.
/// </summary>
public class InvestorResponse : FormalResponse
{
    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public InvestorResponse() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing.
    /// </summary>
    public InvestorResponse(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Investor feedback. Business rule: 50–2000 characters (enforced in the endpoint
    /// handler, Spec FR-004).
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public required string Feedback { get; set; }
}
