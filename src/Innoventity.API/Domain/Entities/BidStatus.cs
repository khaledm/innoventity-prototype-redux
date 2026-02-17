namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Represents the lifecycle status of a bid submission
/// </summary>
public enum BidStatus
{
    /// <summary>
    /// Bid submitted and awaiting innovation owner review
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Bid accepted by innovation owner for partnership
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Bid rejected by innovation owner
    /// </summary>
    Rejected = 2
}
