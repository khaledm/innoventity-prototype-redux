using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
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
    /// - FR-005/FR-015: At least one eligible Pending response must exist per required actor type
    /// - FR-004/FR-014: Payload must contain exactly one response per required actor type (Manufacturing, SalesMarketing, RD)
    /// - FR-003: All selected responses must belong to the target innovation
    /// - FR-006: On success, selected responses → Accepted; all other Pending responses → Rejected; innovation → PartnersSelected
    ///
    /// Field rename only (Spec 005 Decision 3): validation pipeline is unchanged from Phase 1c —
    /// only the entity type (Bid → FormalResponse), enum (BidStatus → ResponseStatus), and
    /// request field (SelectedBidIds → SelectedResponseIds) were renamed.
    /// </remarks>
    /// <param name="innovationId">The innovation to complete partner selection for</param>
    /// <param name="request">Array of selected response IDs (one per required actor type)</param>
    /// <param name="httpContext">HTTP context — actor resolved by ActorResolutionFilter</param>
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
        HttpContext httpContext,
        AppDbContext db)
    {
        var actorId = httpContext.GetCurrentActor().Id;

        // Step 3: Load innovation with all formal responses and their actors
        var innovation = await db.Innovations
            .Include(i => i.FormalResponses)
                .ThenInclude(r => r.Actor)
            .FirstOrDefaultAsync(i => i.Id == innovationId);

        if (innovation == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Innovation '{innovationId}' was not found.");
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

        // Step 7: Readiness check — at least one Pending response per required type before validation (FR-005, FR-009, FR-015)
        // This is a precondition on the innovation, checked before payload validation.
        var allResponses = innovation.FormalResponses;
        var pendingResponses = allResponses.Where(r => r.Status == ResponseStatus.Pending).ToList();
        var missingReadiness = RequiredActorTypes
            .Where(t => !pendingResponses.Any(r => r.Actor?.ActorType == t))
            .ToList();

        if (missingReadiness.Count > 0)
        {
            var missing = string.Join(", ", missingReadiness);
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Insufficient Readiness",
                detail: $"Minimum readiness threshold not met. No eligible response exists for required actor type(s): {missing}.");
        }

        // Steps 8–12: Payload structural validation (returns 422 via ValidationProblem)
        var validationErrors = new Dictionary<string, string[]>();
        var selectedIds = request.SelectedResponseIds ?? [];

        // Step 8: Payload must contain exactly one ID per required actor type (FR-004)
        if (selectedIds.Length != RequiredActorTypes.Length)
        {
            validationErrors["SelectedResponseIds"] = new[]
            {
                $"Exactly {RequiredActorTypes.Length} response IDs must be provided — one per required actor type (Manufacturing, SalesMarketing, RD)."
            };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 9: No duplicate response IDs in payload
        if (selectedIds.Distinct().Count() != selectedIds.Length)
        {
            validationErrors["SelectedResponseIds"] = new[] { "Duplicate response IDs are not allowed in the selection payload." };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 10: All responses must belong to this innovation (FR-004, SelectPartner-3 AC3)
        var allResponseIds = allResponses.Select(r => r.Id).ToHashSet();
        var foreignResponseIds = selectedIds.Where(id => !allResponseIds.Contains(id)).ToList();
        if (foreignResponseIds.Count > 0)
        {
            validationErrors["SelectedResponseIds"] = new[] { "One or more selected responses do not belong to this innovation." };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var selectedResponses = allResponses.Where(r => selectedIds.Contains(r.Id)).ToList();

        // Step 11: No unsupported actor types (FR-014)
        var requiredSet = RequiredActorTypes.ToHashSet();
        var unsupportedResponses = selectedResponses.Where(r => r.Actor != null && !requiredSet.Contains(r.Actor.ActorType)).ToList();
        if (unsupportedResponses.Count > 0)
        {
            validationErrors["SelectedResponseIds"] = new[]
            {
                "Selection payload includes responses from actor types that are not required for partner selection. Required types: Manufacturing, SalesMarketing, RD."
            };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 12: No duplicate actor types in selection (FR-004, SelectPartner-3 AC2)
        var selectedActorTypes = selectedResponses
            .Where(r => r.Actor != null)
            .Select(r => r.Actor!.ActorType)
            .ToList();

        if (selectedActorTypes.Distinct().Count() != selectedActorTypes.Count)
        {
            validationErrors["SelectedResponseIds"] = new[] { "Duplicate actor types detected. Exactly one response per required actor type is allowed." };
            return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // Step 14: Atomic mutation (FR-006, FR-012)
        // Note: a missing-types check here would be unreachable — Steps 8, 11, and 12 together
        // guarantee all 3 required types are covered when this point is reached.
        var selectedIdSet = selectedIds.ToHashSet();

        foreach (var response in allResponses)
        {
            if (selectedIdSet.Contains(response.Id))
            {
                response.Status = ResponseStatus.Accepted;
                response.AcceptedAt = DateTimeOffset.UtcNow;
            }
            else if (response.Status == ResponseStatus.Pending)
            {
                response.Status = ResponseStatus.Rejected;
            }
        }

        innovation.Status = InnovationStatus.PartnersSelected;
        innovation.PartnerSelectionCompletedOn = DateTimeOffset.UtcNow;
        innovation.SelectedByActorId = actorId;

        await db.SaveChangesAsync();

        var acceptedResponses = selectedResponses.Select(r => new AcceptedBidSummary
        {
            BidId = r.Id,
            ActorId = r.ActorId,
            ActorType = r.Actor!.ActorType.ToString()
        }).ToList();

        return Results.Ok(new SelectPartnersResponse
        {
            InnovationId = innovation.Id,
            Status = innovation.Status.ToString(),
            PartnerSelectionCompletedOn = innovation.PartnerSelectionCompletedOn!.Value,
            AcceptedBids = acceptedResponses
        });
    }

    /// <summary>
    /// Request model for partner selection
    /// </summary>
    public record SelectPartnersRequest
    {
        /// <summary>
        /// Exactly one response ID per required actor type: Manufacturing, SalesMarketing, RD (FR-004)
        /// </summary>
        public Guid[] SelectedResponseIds { get; init; } = [];
    }

    /// <summary>
    /// Summary of a single accepted response
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
            .AddEndpointFilter<ActorResolutionFilter>()
            .Produces<SelectPartnersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
