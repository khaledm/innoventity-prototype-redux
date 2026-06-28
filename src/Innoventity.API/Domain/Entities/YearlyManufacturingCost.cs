namespace Innoventity.API.Domain.Entities;

/// <summary>
/// One year of a manufacturing actor's cost projection (Spec 005 §R4.x).
/// Value object persisted inside the <c>YearlyManufacturingCosts</c> JSON column via
/// <c>OwnsMany(...).ToJson()</c> — it has no EF identity key.
/// Every projected metric is paired with a rationale (Spec FR-007).
/// </summary>
public class YearlyManufacturingCost
{
    /// <summary>
    /// Projection year, contiguous starting from 1, range 1–10 (Spec FR-010, FR-011).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Projected production volume (units), integer ≥ 0.
    /// </summary>
    public int ProductionVolume { get; set; }

    /// <summary>
    /// Justification for the production volume (20–500 chars, Spec FR-008).
    /// </summary>
    public required string ProductionVolumeRationale { get; set; }

    /// <summary>
    /// Projected per-unit production cost (decimal ≥ 0).
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Justification for the unit cost (20–500 chars).
    /// </summary>
    public required string UnitCostRationale { get; set; }

    /// <summary>
    /// Projected average global distribution expense per unit (decimal ≥ 0).
    /// </summary>
    public decimal AverageGlobalDistributionExpense { get; set; }

    /// <summary>
    /// Justification for the average global distribution expense (20–500 chars).
    /// </summary>
    public required string AvgDistributionExpenseRationale { get; set; }
}
