using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for submitting partnership proposals (bids) on innovations
/// </summary>
public static class SubmitBid
{
    /// <summary>
    /// Submit a partnership proposal for an innovation
    /// </summary>
    /// <remarks>
    /// Business Rules:
    /// - R4.1: Only R&amp;D, Manufacturing, SalesMarketing, Investor actors can submit bids
    /// - R4.1: Idea Generator actors cannot submit bids
    /// - R4.1: Actor cannot bid on their own innovations
    /// - R4.1: Innovation must be in Published status (accepting bids)
    /// - R4.1: Actor cannot submit duplicate bids for same innovation
    /// - R4.2: Location required (non-empty string)
    /// - R4.2: ParticipationType required
    /// - R4.2: ParticipationProposal required, minimum 200 characters
    /// </remarks>
    /// <param name="innovationId">The innovation to bid on</param>
    /// <param name="request">Bid submission details</param>
    /// <param name="httpContext">HTTP context — actor resolved by ActorResolutionFilter</param>
    /// <param name="db">Database context (injected by ASP.NET Core)</param>
    /// <response code="201">Bid successfully submitted</response>
    /// <response code="400">Validation errors (proposal too short, missing required fields)</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - Idea Generators cannot submit bids</response>
    /// <response code="404">Innovation not found or not published</response>
    /// <response code="409">Conflict - duplicate bid or own innovation</response>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid innovationId,
        SubmitBidRequest request,
        HttpContext httpContext,
        AppDbContext db)
    {
        var actor = httpContext.GetCurrentActor();

        // R4.1: Idea Generators cannot submit bids
        if (actor.ActorType == ActorType.IdeaGenerator)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Idea Generators cannot submit bids. Only R&D, Manufacturing, Sales & Marketing, and Investor actors can submit partnership proposals.");
        }

        // Validate innovation exists and is published
        var innovation = await db.Innovations.FindAsync(innovationId);
        if (innovation == null)
        {
            return Results.NotFound(new { error = "Innovation not found." });
        }

        // R4.1: Innovation must be Published (accepting bids)
        if (innovation.Status != InnovationStatus.Published)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "This innovation is not accepting bids. Only published innovations can receive partnership proposals.");
        }

        // R4.1: Actor cannot bid on their own innovation
        if (innovation.OwnerId == actor.Id)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: "You cannot submit a bid for your own innovation.");
        }

        // R4.1: Check for duplicate bid
        var existingBid = await db.Bids
            .FirstOrDefaultAsync(b => b.ActorId == actor.Id && b.InnovationId == innovationId);

        if (existingBid != null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate Bid",
                detail: "You have already submitted a bid for this innovation. You can edit your existing bid instead.");
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

        // Create bid entity
        var bid = new Bid(Guid.NewGuid())
        {
            InnovationId = innovationId,
            ActorId = actor.Id,
            Location = request.Location,
            ParticipationType = request.ParticipationType,
            ParticipationProposal = request.ParticipationProposal,
            Status = BidStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        db.Bids.Add(bid);
        await db.SaveChangesAsync();

        // Return response
        var response = new SubmitBidResponse
        {
            BidId = bid.Id,
            InnovationId = bid.InnovationId,
            ActorId = bid.ActorId,
            Location = bid.Location,
            ParticipationType = bid.ParticipationType,
            SubmittedAt = bid.SubmittedAt,
            Status = bid.Status.ToString()
        };

        return Results.Created($"/bids/{bid.Id}", response);
    }

    /// <summary>
    /// Request model for submitting a bid
    /// </summary>
    public record SubmitBidRequest
    {
        /// <summary>
        /// Geographic location of the bidding organization (R4.2)
        /// Example: "Munich, Germany", "San Francisco, CA, USA"
        /// </summary>
        public string Location { get; init; } = string.Empty;

        /// <summary>
        /// Type of partnership being proposed (R4.2)
        /// Example: "Manufacturing Partner", "R&amp;D Collaboration", "Distribution Partner"
        /// </summary>
        public string ParticipationType { get; init; } = string.Empty;

        /// <summary>
        /// Detailed partnership proposal (R4.2 - minimum 200 characters)
        /// Must explain capabilities, experience, and value proposition
        /// </summary>
        public string ParticipationProposal { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response model for bid submission
    /// </summary>
    public record SubmitBidResponse
    {
        /// <summary>Unique identifier of the newly created bid</summary>
        public Guid BidId { get; init; }

        /// <summary>Innovation this bid was submitted for</summary>
        public Guid InnovationId { get; init; }

        /// <summary>Actor who submitted the bid</summary>
        public Guid ActorId { get; init; }

        /// <summary>Geographic location of the bidding organization</summary>
        public string Location { get; init; } = string.Empty;

        /// <summary>Type of partnership being proposed</summary>
        public string ParticipationType { get; init; } = string.Empty;

        /// <summary>Timestamp when the bid was submitted</summary>
        public DateTimeOffset SubmittedAt { get; init; }

        /// <summary>Current bid status (always "Pending" for new submissions)</summary>
        public string Status { get; init; } = string.Empty;
    }

    /// <summary>
    /// Register the POST /innovations/{innovationId}/bids endpoint
    /// </summary>
    public static void MapSubmitBid(this WebApplication app)
    {
        app.MapPost("/innovations/{innovationId}/bids", Handle)
            .WithName("SubmitBid")
            .WithTags("Bids")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<ActorResolutionFilter>()
            .Produces<SubmitBidResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
