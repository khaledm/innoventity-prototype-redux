namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Defines the types of actors in the platform ecosystem.
/// Each actor type has distinct permissions and workflows per R1.2 (Actor Type Immutability).
/// </summary>
public enum ActorType
{
    /// <summary>
    /// Innovation creator role - can submit innovations and select partners
    /// </summary>
    IdeaGenerator = 1,

    /// <summary>
    /// Research &amp; Development partner - provides technical development expertise
    /// </summary>
    RD = 2,

    /// <summary>
    /// Manufacturing partner - provides production capabilities and distribution
    /// </summary>
    Manufacturing = 3,

    /// <summary>
    /// Sales and Marketing partner - provides market access and commercialization
    /// </summary>
    SalesMarketing = 4,

    /// <summary>
    /// Investment partner - provides financial resources
    /// </summary>
    Investor = 5
}
