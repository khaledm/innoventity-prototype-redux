namespace Innoventity.API.Domain.Entities;

/// <summary>
/// One year of an R&amp;D actor's development cost projection (Spec 005 §R4.x).
/// Value object persisted inside the <c>YearlyDevelopmentCosts</c> JSON column via
/// <c>OwnsMany(...).ToJson()</c> — EF stores a per-entry key (for example <c>Id</c>) in the JSON payload for change tracking.
/// Every projected metric is paired with a rationale (Spec FR-007).
/// </summary>
public class YearlyDevelopmentCost
{
    /// <summary>
    /// Projection year, contiguous starting from 1, range 1–10 (Spec FR-010, FR-011).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Projected infrastructure cost (decimal ≥ 0).
    /// </summary>
    public decimal InfrastructureCost { get; set; }

    /// <summary>
    /// Justification for the infrastructure cost (20–500 chars, Spec FR-008).
    /// </summary>
    public required string InfrastructureCostRationale { get; set; }

    /// <summary>
    /// Projected people cost (decimal ≥ 0).
    /// </summary>
    public decimal PeopleCost { get; set; }

    /// <summary>
    /// Justification for the people cost (20–500 chars).
    /// </summary>
    public required string PeopleCostRationale { get; set; }
}
