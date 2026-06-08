using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for retrieving all bids submitted for an innovation
/// </summary>
public static class GetBids
{
    /// <summary>
    /// Retrieve all partnership proposals (bids) for an innovation
    /// </summary>
    /// <remarks>
    /// Business Rules:
    /// - R6.1: Only the innovation owner can view bids
    /// - R8.2: Other actors receive 403 Forbidden
    /// - Returns full proposal details including actor information
    /// - Bids ordered by submission date (newest first)
    /// </remarks>
    /// <param name="innovationId">The innovation to retrieve bids for</param>
    /// <param name="httpContext">HTTP context — actor resolved by ActorResolutionFilter</param>
    /// <param name="db">Database context (injected by ASP.NET Core)</param>
    /// <response code="200">Bids retrieved successfully</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - only innovation owner can view bids</response>
    /// <response code="404">Innovation not found</response>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid innovationId,
        HttpContext httpContext,
        AppDbContext db)
    {
        var actor = httpContext.GetCurrentActor();

        // Validate innovation exists
        var innovation = await db.Innovations.FindAsync(innovationId);
        if (innovation == null)
        {
            return Results.NotFound(new { error = "Innovation not found." });
        }

        // R6.1 & R8.2: Only innovation owner can view bids
        if (innovation.OwnerId != actor.Id)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Only the innovation owner can view submitted bids.");
        }

        // Query all bids for this innovation with actor details
        var bids = await db.Bids
            .Include(b => b.Actor)
            .Where(b => b.InnovationId == innovationId)
            .OrderByDescending(b => b.SubmittedAt)
            .Select(b => new BidDetailDto
            {
                BidId = b.Id,
                Actor = new ActorSummaryDto
                {
                    ActorId = b.ActorId,
                    FirstName = b.Actor!.FirstName,
                    LastName = b.Actor.LastName,
                    DisplayName = $"{b.Actor.FirstName} {b.Actor.LastName}",
                    ActorType = b.Actor.ActorType.ToString()
                },
                Location = b.Location,
                ParticipationType = b.ParticipationType,
                ParticipationProposal = b.ParticipationProposal,
                SubmittedAt = b.SubmittedAt,
                Status = b.Status.ToString()
            })
            .ToListAsync();

        // Return response
        var response = new GetBidsResponse
        {
            Bids = bids,
            TotalCount = bids.Count
        };

        return Results.Ok(response);
    }

    /// <summary>
    /// Response model for GET /innovations/{innovationId}/bids
    /// </summary>
    public class GetBidsResponse
    {
        /// <summary>
        /// List of bids submitted for the innovation
        /// </summary>
        public List<BidDetailDto> Bids { get; set; } = new();

        /// <summary>
        /// Total count of bids
        /// </summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Detailed bid information including actor details
    /// </summary>
    public class BidDetailDto
    {
        /// <summary>
        /// Unique identifier for the bid
        /// </summary>
        public Guid BidId { get; set; }

        /// <summary>
        /// Actor who submitted the bid
        /// </summary>
        public ActorSummaryDto Actor { get; set; } = null!;

        /// <summary>
        /// Geographic location of the bidding organization
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Type of partnership being proposed
        /// </summary>
        public string ParticipationType { get; set; } = string.Empty;

        /// <summary>
        /// Full partnership proposal text
        /// </summary>
        public string ParticipationProposal { get; set; } = string.Empty;

        /// <summary>
        /// When the bid was submitted
        /// </summary>
        public DateTimeOffset SubmittedAt { get; set; }

        /// <summary>
        /// Current status (Pending, Accepted, Rejected)
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Summary information about the actor who submitted a bid
    /// </summary>
    public class ActorSummaryDto
    {
        /// <summary>
        /// Unique identifier for the actor
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// Actor's first name
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Actor's last name
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Display name (typically "FirstName LastName")
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Type of actor (Manufacturing, RD, SalesMarketing, Investor)
        /// </summary>
        public string ActorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Register the GET /innovations/{innovationId}/bids endpoint
    /// </summary>
    public static void MapGetBids(this WebApplication app)
    {
        app.MapGet("/innovations/{innovationId}/bids", Handle)
            .WithName("GetBids")
            .WithTags("Bids")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<ActorResolutionFilter>()
            .Produces<GetBidsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
