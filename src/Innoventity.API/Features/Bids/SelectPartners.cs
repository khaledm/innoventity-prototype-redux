using System.Security.Claims;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for completing partner selection on a published innovation
/// </summary>
public static class SelectPartners
{
    /// <summary>
    /// Required actor types for Phase 1c partner selection (FR-013)
    /// </summary>
    private static readonly ActorType[] RequiredActorTypes =
    [
        ActorType.Manufacturing,
        ActorType.SalesMarketing,
        ActorType.RD
    ];

    /// <summary>
    /// Select collaboration partners for a published innovation
    /// </summary>
    /// <remarks>
    /// Business Rules:
    /// - FR-002/FR-011: Caller must be authenticated and must own the innovation
    /// - FR-007: If selection already completed, returns 403 (irreversible commitment)
    /// - FR-016: Innovation must be in Published status
    /// - FR-005/FR-015: At least one eligible Pending bid must exist per required actor type
    /// - FR-004/FR-014: Payload must contain exactly one bid per required actor type (Manufacturing, SalesMarketing, RD)
    /// - FR-003: All selected bids must belong to the target innovation
    /// - FR-006: On success, selected bids → Accepted; all other Pending bids → Rejected; innovation → PartnersSelected
    /// </remarks>
    /// <param name="innovationId">The innovation to complete partner selection for</param>
    /// <param name="request">Array of selected bid IDs (one per required actor type)</param>
    /// <param name="user">Authenticated user principal</param>
    /// <param name="db">Database context</param>
    /// <response code="200">Partner selection completed</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - not the innovation owner, or selection already completed</response>
    /// <response code="404">Innovation not found</response>
    /// <response code="409">Conflict - innovation not in Published state, or minimum readiness threshold not met</response>
    /// <response code="422">Unprocessable - invalid actor type coverage (missing, duplicate, or unsupported types)</response>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid innovationId,
        SelectPartnersRequest request,
        ClaimsPrincipal user,
        AppDbContext db)
    {
        // Step 1 & 2: Authenticate caller
        var actorIdClaim = user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);
        if (actorIdClaim == null || !Guid.TryParse(actorIdClaim.Value, out var actorId))
        {
            return Results.Unauthorized();
        }

        var actor = await db.Actors.FindAsync(actorId);
        if (actor == null)
        {
            return Results.Unauthorized();
        }

        // Step 3: Load innovation with all bids and their actors
        var innovation = await db.Innovations
            .Include(i => i.Bids!)
                .ThenInclude(b => b.Actor)
            .FirstOrDefaultAsync(i => i.Id == innovationId);

        if (innovation == null)
        {
            return Results.NotFound();
        }

        // Step 4: Ownership check (FR-002, FR-011)
        if (innovation.OwnerId != actorId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Only the innovation owner can select partners.");
        }

        // Step 5: Irreversibility check (FR-007, SelectPartner-4)
        if (innovation.PartnerSelectionCompletedOn != null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Selection Immutable",
                detail: "Partner selection is final and cannot be changed.");
        }

        // Step 6: Innovation must be Published (FR-016)
        if (innovation.Status != InnovationStatus.Published)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: $"Partner selection requires the innovation to be in Published status. Current status: {innovation.Status}.");
        }

        // Step 7: Readiness check — at least one Pending bid per required type before validation (FR-005, FR-009, FR-015)
        // This is a precondition on the innovation, checked before payload validation.
        var allBids = innovation.Bids ?? [];
        var pendingBids = allBids.Where(b => b.Status == BidStatus.Pending).ToList();
        var missingReadiness = RequiredActorTypes
            .Where(t => !pendingBids.Any(b => b.Actor?.ActorType == t))
            .ToList();

        if (missingReadiness.Count > 0)
        {
            var missing = string.Join(", ", missingReadiness);
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Insufficient Readiness",
                detail: $"Minimum readiness threshold not met. No eligible bid exists for required actor type(s): {missing}.");
        }

        // Steps 8–12: Payload structural validation (returns 422 via ValidationProblem)
        var validationErrors = new Dictionary<string, string[]>();
        var selectedIds = request.SelectedBidIds ?? [];

        // Step 8: Payload must contain exactly one ID per required actor type (FR-004)
        if (selectedIds.Length != RequiredActorTypes.Length)
        {
            validationErrors["SelectedBidIds"] = new[]
            {
                $"Exactly {RequiredActorTypes.Length} bid IDs must be provided — one per required actor type (Manufacturing, SalesMarketing, RD)."
            };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 9: No duplicate bid IDs in payload
        if (selectedIds.Distinct().Count() != selectedIds.Length)
        {
            validationErrors["SelectedBidIds"] = new[] { "Duplicate bid IDs are not allowed in the selection payload." };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 10: All bids must belong to this innovation (FR-004, SelectPartner-3 AC3)
        var allBidIds = allBids.Select(b => b.Id).ToHashSet();
        var foreignBidIds = selectedIds.Where(id => !allBidIds.Contains(id)).ToList();
        if (foreignBidIds.Count > 0)
        {
            validationErrors["SelectedBidIds"] = new[] { "One or more selected bids do not belong to this innovation." };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var selectedBids = allBids.Where(b => selectedIds.Contains(b.Id)).ToList();

        // Step 11: No unsupported actor types (FR-014)
        var requiredSet = RequiredActorTypes.ToHashSet();
        var unsupportedBids = selectedBids.Where(b => b.Actor != null && !requiredSet.Contains(b.Actor.ActorType)).ToList();
        if (unsupportedBids.Count > 0)
        {
            validationErrors["SelectedBidIds"] = new[]
            {
                "Selection payload includes bids from actor types that are not required for partner selection. Required types: Manufacturing, SalesMarketing, RD."
            };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 12: No duplicate actor types in selection (FR-004, SelectPartner-3 AC2)
        var selectedActorTypes = selectedBids
            .Where(b => b.Actor != null)
            .Select(b => b.Actor!.ActorType)
            .ToList();

        if (selectedActorTypes.Distinct().Count() != selectedActorTypes.Count)
        {
            validationErrors["SelectedBidIds"] = new[] { "Duplicate actor types detected. Exactly one bid per required actor type is allowed." };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 14: Atomic mutation (FR-006, FR-012)
        // Note: a missing-types check here would be unreachable — Steps 8, 11, and 12 together
        // guarantee all 3 required types are covered when this point is reached.
        var selectedIdSet = selectedIds.ToHashSet();

        foreach (var bid in allBids)
        {
            if (selectedIdSet.Contains(bid.Id))
            {
                bid.Status = BidStatus.Accepted;
                bid.AcceptedAt = DateTimeOffset.UtcNow;
            }
            else if (bid.Status == BidStatus.Pending)
            {
                bid.Status = BidStatus.Rejected;
            }
        }

        innovation.Status = InnovationStatus.PartnersSelected;
        innovation.PartnerSelectionCompletedOn = DateTimeOffset.UtcNow;
        innovation.SelectedByActorId = actorId;

        await db.SaveChangesAsync();

        var acceptedBids = selectedBids.Select(b => new AcceptedBidSummary
        {
            BidId = b.Id,
            ActorId = b.ActorId,
            ActorType = b.Actor!.ActorType.ToString()
        }).ToList();

        return Results.Ok(new SelectPartnersResponse
        {
            InnovationId = innovation.Id,
            Status = innovation.Status.ToString(),
            PartnerSelectionCompletedOn = innovation.PartnerSelectionCompletedOn!.Value,
            AcceptedBids = acceptedBids
        });
    }

    /// <summary>
    /// Request model for partner selection
    /// </summary>
    public record SelectPartnersRequest
    {
        /// <summary>
        /// Exactly one bid ID per required actor type: Manufacturing, SalesMarketing, RD (FR-004)
        /// </summary>
        public Guid[] SelectedBidIds { get; init; } = [];
    }

    /// <summary>
    /// Summary of a single accepted bid
    /// </summary>
    public record AcceptedBidSummary
    {
        public Guid BidId { get; init; }
        public Guid ActorId { get; init; }
        public string ActorType { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response model for completed partner selection
    /// </summary>
    public record SelectPartnersResponse
    {
        public Guid InnovationId { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset PartnerSelectionCompletedOn { get; init; }
        public List<AcceptedBidSummary> AcceptedBids { get; init; } = [];
    }

    /// <summary>
    /// Register the POST /innovations/{innovationId}/select-partners endpoint
    /// </summary>
    public static void MapSelectPartners(this WebApplication app)
    {
        app.MapPost("/innovations/{innovationId}/select-partners", Handle)
            .WithName("SelectPartners")
            .WithTags("Bids")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces<SelectPartnersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
