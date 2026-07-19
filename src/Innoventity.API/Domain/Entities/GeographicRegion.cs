namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Geographic region in which a responding actor proposes to operate (Spec 005 §R4.2).
/// Persisted as a string discriminator value via HasConversion&lt;string&gt;().
/// </summary>
public enum GeographicRegion
{
    /// <summary>
    /// Asia region.
    /// </summary>
    Asia = 1,

    /// <summary>
    /// North and South America.
    /// </summary>
    Americas = 2,

    /// <summary>
    /// Europe region.
    /// </summary>
    Europe = 3,

    /// <summary>
    /// Africa region.
    /// </summary>
    Africa = 4,

    /// <summary>
    /// Oceania region.
    /// </summary>
    Oceania = 5
}
