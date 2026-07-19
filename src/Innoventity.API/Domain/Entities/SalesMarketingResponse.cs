namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Sales &amp; Marketing actor's formal response, carrying a structured yearly revenue projection
/// (Spec 005 §R4.x). Persisted with discriminator <c>ResponseType = "SalesMarketingResponse"</c>;
/// <see cref="YearlySales"/> is stored as a JSON column.
/// </summary>
public class SalesMarketingResponse : FormalResponse
{
    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public SalesMarketingResponse() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing.
    /// </summary>
    public SalesMarketingResponse(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Per-year sales projection (JSON column). Years must be contiguous starting from 1,
    /// max 10 (Spec FR-010, FR-011).
    /// </summary>
    public IList<YearlySale> YearlySales { get; set; } = [];
}
