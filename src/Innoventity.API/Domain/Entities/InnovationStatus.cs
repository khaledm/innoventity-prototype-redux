namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Innovation workflow status (Spec §R2, §R3.1)
/// </summary>
public enum InnovationStatus
{
    Draft = 1,
    Published = 2,
    PartnersSelected = 3
}
