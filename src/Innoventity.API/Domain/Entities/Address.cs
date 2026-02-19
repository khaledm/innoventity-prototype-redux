using System.ComponentModel.DataAnnotations;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Address value object - owned by Actor entity (R1.5)
/// No separate Id - embedded in Actor table as ContactAddress_*
/// This is a value object following DDD principles - no identity, compared by value
/// </summary>
public class Address
{
    /// <summary>
    /// First line of address (street address)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Address1 { get; set; } = string.Empty;

    /// <summary>
    /// Second line of address (optional - apartment, suite, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? Address2 { get; set; }

    /// <summary>
    /// City or town name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PostCode { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "US", "GB", "DE")
    /// </summary>
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;
}
