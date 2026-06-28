namespace Innoventity.API.Domain.Entities;

/// <summary>
/// One year of a sales &amp; marketing actor's revenue projection (Spec 005 §R4.x).
/// Value object persisted inside the <c>YearlySales</c> JSON column via
/// <c>OwnsMany(...).ToJson()</c> — it has no EF identity key.
/// Every projected metric is paired with a rationale (Spec FR-007).
/// </summary>
public class YearlySale
{
    /// <summary>
    /// Projection year, contiguous starting from 1, range 1–10 (Spec FR-010, FR-011).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Projected units sold, integer ≥ 0.
    /// </summary>
    public int UnitsSold { get; set; }

    /// <summary>
    /// Justification for the units sold (20–500 chars, Spec FR-008).
    /// </summary>
    public required string UnitsSoldRationale { get; set; }

    /// <summary>
    /// Projected per-unit sale price (decimal ≥ 0).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Justification for the unit price (20–500 chars).
    /// </summary>
    public required string UnitPriceRationale { get; set; }

    /// <summary>
    /// Projected sales &amp; marketing expense (decimal ≥ 0).
    /// </summary>
    public decimal SalesMarketingExpense { get; set; }

    /// <summary>
    /// Justification for the sales &amp; marketing expense (20–500 chars).
    /// </summary>
    public required string SalesMarketingExpenseRationale { get; set; }
}
