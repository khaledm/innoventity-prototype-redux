namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Lifecycle status of a formal response (Spec 005). Replaces the former <c>BidStatus</c>.
/// Persisted as a string via HasConversion&lt;string&gt;().
/// </summary>
public enum ResponseStatus
{
    /// <summary>
    /// Response submitted and awaiting innovation owner review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Response accepted by the innovation owner for partnership.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Response rejected by the innovation owner.
    /// </summary>
    Rejected = 2
}
