using System.Security.Claims;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for updating partnership proposals (bids) before acceptance
/// </summary>
public static class UpdateBid
{
    /// <summary>
    /// Update an unaccepted partnership proposal
    /// </summary>
    /// <remarks>
    /// Business Rules:
    /// - R8.2: Only the bid author (ActorId) can update their own bid
    /// - Accepted bids are immutable (409 Conflict)
    /// - Rejected bids are immutable (409 Conflict)
    /// - Only Pending bids can be updated
    /// - R4.2: ParticipationProposal must be at least 200 characters if provided
    /// </remarks>
    /// <param name="bidId">The bid to update</param>
    /// <param name="request">Updated bid details</param>
    /// <response code="200">Bid successfully updated</response>
    /// <response code="400">Validation errors (proposal too short, invalid data)</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - only bid author can update</response>
    /// <response code="404">Bid not found</response>
    /// <response code="409">Conflict - bid has been accepted or rejected (immutable)</response>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid bidId,
        UpdateBidRequest request,
        ClaimsPrincipal user,
        AppDbContext db)
    {
        // Extract actor ID from JWT claims
        var actorIdClaim = user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);
        if (actorIdClaim == null || !Guid.TryParse(actorIdClaim.Value, out var actorId))
        {
            return Results.Unauthorized();
        }

        // Query bid with actor details
        var bid = await db.Bids
            .Include(b => b.Actor)
            .FirstOrDefaultAsync(b => b.Id == bidId);

        if (bid == null)
        {
            return Results.NotFound(new { error = "Bid not found." });
        }

        // R8.2: Only bid author can update their own bid
        if (bid.ActorId != actorId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "You can only update your own bids.");
        }

        // Status validation: Only Pending bids can be updated
        if (bid.Status == BidStatus.Accepted)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Bid Immutable",
                detail: "This bid has been accepted and cannot be modified. Accepted bids are immutable to preserve partnership agreement integrity.");
        }

        if (bid.Status == BidStatus.Rejected)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Bid Immutable",
                detail: "This bid has been rejected and cannot be modified.");
        }

        // Validate request fields (R4.2)
        var validationErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            validationErrors["Location"] = new[] { "Location is required." };
        }

        if (string.IsNullOrWhiteSpace(request.ParticipationType))
        {
            validationErrors["ParticipationType"] = new[] { "Participation type is required." };
        }

        if (string.IsNullOrWhiteSpace(request.ParticipationProposal))
        {
            validationErrors["ParticipationProposal"] = new[] { "Participation proposal is required." };
        }
        else if (request.ParticipationProposal.Length < 200)
        {
            validationErrors["ParticipationProposal"] = new[] { "Participation proposal must be at least 200 characters." };
        }

        if (validationErrors.Any())
        {
            return Results.ValidationProblem(validationErrors);
        }

        // Update bid fields
        bid.Location = request.Location;
        bid.ParticipationType = request.ParticipationType;
        bid.ParticipationProposal = request.ParticipationProposal;
        bid.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        // Return updated bid response
        var response = new UpdateBidResponse
        {
            BidId = bid.Id,
            Location = bid.Location,
            ParticipationType = bid.ParticipationType,
            ParticipationProposal = bid.ParticipationProposal,
            UpdatedAt = bid.UpdatedAt ?? DateTimeOffset.UtcNow,
            Status = bid.Status.ToString()
        };

        return Results.Ok(response);
    }

    /// <summary>
    /// Register the PUT /bids/{bidId} endpoint
    /// </summary>
    public static void MapUpdateBid(this WebApplication app)
    {
        app.MapPut("/bids/{bidId:guid}", Handle)
            .RequireAuthorization()
            .WithTags("Bids")
            .WithName("UpdateBid")
            .WithOpenApi()
            .Produces<UpdateBidResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// Request to update an existing bid
/// </summary>
public record UpdateBidRequest
{
    /// <summary>
    /// Geographic location of the bidding organization
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Type of partnership being proposed
    /// </summary>
    public required string ParticipationType { get; init; }

    /// <summary>
    /// Detailed partnership proposal (minimum 200 characters)
    /// </summary>
    public required string ParticipationProposal { get; init; }
}

/// <summary>
/// Response after successfully updating a bid
/// </summary>
public record UpdateBidResponse
{
    public required Guid BidId { get; init; }
    public required string Location { get; init; }
    public required string ParticipationType { get; init; }
    public required string ParticipationProposal { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string Status { get; init; }
}
