using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Common;

namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Industry classification for innovation targeting and actor affiliation (Spec §R3.2)
/// Inherits identity-based equality from EntityBase&lt;string&gt; (R9.1)
/// </summary>
public class Industry : EntityBase<string>
{
    // Id inherited from EntityBase<string> (renamed from IndustryId)

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    public Industry() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for seed data and testing
    /// </summary>
    public Industry(string id) : base(id)
    {
    }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }
}
