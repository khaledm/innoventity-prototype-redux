namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Manufacturing actor's formal response, carrying a structured yearly cost projection
/// (Spec 005 §R4.x). Persisted with discriminator <c>ResponseType = "ManufacturingResponse"</c>;
/// <see cref="YearlyManufacturingCosts"/> is stored as a JSON column.
/// </summary>
public class ManufacturingResponse : FormalResponse
{
    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public ManufacturingResponse() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing.
    /// </summary>
    public ManufacturingResponse(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Per-year manufacturing cost projection (JSON column). Years must be contiguous
    /// starting from 1, max 10 (Spec FR-010, FR-011).
    /// </summary>
    public IList<YearlyManufacturingCost> YearlyManufacturingCosts { get; set; } = [];
}
