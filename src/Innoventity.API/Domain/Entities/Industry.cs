using System.ComponentModel.DataAnnotations;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Industry classification for innovation targeting and actor affiliation (Spec §R3.2)
/// </summary>
public class Industry
{
    [Key]
    [MaxLength(50)]
    public required string IndustryId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }
}
