namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Represents the activation and access status of an actor account.
/// Enforces R1.1 (Account Activation) requirements.
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// Account created but not yet activated via email link (R1.1)
    /// User cannot submit innovations or bids in this state
    /// </summary>
    PendingActivation = 1,

    /// <summary>
    /// Account activated and fully functional
    /// User can perform all role-specific actions (R8.5)
    /// </summary>
    Active = 2,

    /// <summary>
    /// Account suspended by administrator (R8.6)
    /// User cannot perform any actions until reactivated
    /// </summary>
    Suspended = 3
}
