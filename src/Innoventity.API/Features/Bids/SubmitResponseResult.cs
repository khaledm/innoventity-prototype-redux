namespace Innoventity.API.Features.Bids;

/// <summary>
/// Response model shared by all four typed submission endpoints (Spec 005 api-contracts.md).
/// </summary>
public record SubmitResponseResult
{
    public Guid ResponseId { get; init; }
    public string ResponseType { get; init; } = string.Empty;
    public Guid InnovationId { get; init; }
    public Guid ActorId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; init; }
}