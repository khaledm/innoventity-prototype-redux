namespace Innoventity.API.Domain.Entities;

/// <summary>
/// R&amp;D actor's formal response, carrying a development duration and a structured yearly
/// development cost projection (Spec 005 §R4.x). Persisted with discriminator
/// <c>ResponseType = "ResearchDevelopmentResponse"</c>; <see cref="YearlyDevelopmentCosts"/>
/// is stored as a JSON column.
/// </summary>
public class ResearchDevelopmentResponse : FormalResponse
{
    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public ResearchDevelopmentResponse() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing.
    /// </summary>
    public ResearchDevelopmentResponse(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Estimated product development duration in years, range 1–10 (Spec FR-003).
    /// </summary>
    public int ProductDevelopmentDuration { get; set; }

    /// <summary>
    /// Per-year development cost projection (JSON column). Years must be contiguous
    /// starting from 1, max 10 (Spec FR-010, FR-011).
    /// </summary>
    public IList<YearlyDevelopmentCost> YearlyDevelopmentCosts { get; set; } = [];
}
