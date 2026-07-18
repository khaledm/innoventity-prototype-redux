using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for submitting a Manufacturing formal response with structured yearly cost
/// projections (Spec 005 US1).
/// </summary>
public static class SubmitManufacturingResponse
{
    /// <summary>
    /// Submit a Manufacturing partnership response for an innovation
    /// </summary>
    /// <remarks>
    /// Guard chain (Spec 005 plan.md Phase 3):
    /// actor resolved by ActorResolutionFilter → reject non-Manufacturing (403) →
    /// look up innovation → reject non-Published (404) → reject if actor owns innovation (403) →
    /// reject duplicate (409) → validate participationProposal ≥ 100 chars (400) →
    /// validate year contiguity + max 10 (422) → validate every rationale field is 20–500 chars
    /// with the field name in the error (422) → persist → 201.
    /// </remarks>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid innovationId,
        SubmitManufacturingResponseRequest request,
        HttpContext httpContext,
        AppDbContext db)
    {
        var actor = httpContext.GetCurrentActor();

        if (actor.ActorType != ActorType.Manufacturing)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Only Manufacturing actors can submit a manufacturing response.");
        }

        var innovation = await db.Innovations.FindAsync(innovationId);
        if (innovation == null || innovation.Status != InnovationStatus.Published)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "This innovation is not accepting responses. Only published innovations can receive partnership proposals.");
        }

        if (innovation.OwnerId == actor.Id)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "You cannot submit a response for your own innovation.");
        }

        var duplicateExists = await db.FormalResponses
            .AnyAsync(r => r.ActorId == actor.Id && r.InnovationId == innovationId);
        if (duplicateExists)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate Response",
                detail: "You have already submitted a response for this innovation.");
        }

        if (string.IsNullOrWhiteSpace(request.ParticipationProposal) || request.ParticipationProposal.Length < 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ParticipationProposal"] = ["Participation proposal must be at least 100 characters."]
            });
        }

        if (!Enum.TryParse<GeographicRegion>(request.Location, ignoreCase: true, out var location))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Location"] = ["Location must be one of: Asia, Americas, Europe, Africa, Oceania."]
            });
        }

        var projectionErrors = ValidateProjection(request.YearlyManufacturingCosts);
        if (projectionErrors.Count > 0)
        {
            return Results.ValidationProblem(projectionErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var response = new ManufacturingResponse(Guid.NewGuid())
        {
            InnovationId = innovationId,
            ActorId = actor.Id,
            Location = location,
            ParticipationType = request.ParticipationType,
            ParticipationProposal = request.ParticipationProposal,
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
            YearlyManufacturingCosts = request.YearlyManufacturingCosts.Select(y => new YearlyManufacturingCost
            {
                Year = y.Year,
                ProductionVolume = y.ProductionVolume,
                ProductionVolumeRationale = y.ProductionVolumeRationale,
                UnitCost = y.UnitCost,
                UnitCostRationale = y.UnitCostRationale,
                AverageGlobalDistributionExpense = y.AverageGlobalDistributionExpense,
                AvgDistributionExpenseRationale = y.AvgDistributionExpenseRationale
            }).ToList()
        };

        db.FormalResponses.Add(response);
        await db.SaveChangesAsync();

        return Results.Created($"/innovations/{innovationId}/bids/manufacturing/{response.Id}", new SubmitResponseResult
        {
            ResponseId = response.Id,
            ResponseType = nameof(ManufacturingResponse),
            InnovationId = response.InnovationId,
            ActorId = response.ActorId,
            Status = response.Status.ToString(),
            SubmittedAt = response.SubmittedAt
        });
    }

    /// <summary>
    /// Validates non-empty list, year contiguity starting at 1, max year 10, and that every
    /// rationale field is 20–500 chars (a missing rationale binds to an empty string, which
    /// naturally fails the minimum-length check — covering FR-009 partial-projection rejection).
    /// </summary>
    internal static Dictionary<string, string[]> ValidateProjection(List<YearlyManufacturingCostRequest> entries)
    {
        var errors = new Dictionary<string, string[]>();

        if (entries.Count == 0)
        {
            errors["YearlyManufacturingCosts"] = ["At least one yearly projection entry is required."];
            return errors;
        }

        var years = entries.Select(e => e.Year).OrderBy(y => y).ToList();
        var expectedYears = Enumerable.Range(1, years.Count).ToList();
        if (!years.SequenceEqual(expectedYears))
        {
            errors["YearlyManufacturingCosts"] = ["Projection years must be contiguous starting from year 1, with no gaps or duplicates."];
        }

        if (years.Max() > 10)
        {
            errors["YearlyManufacturingCosts.Year"] = ["Projection years cannot exceed 10."];
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            ValidateRationale(errors, $"YearlyManufacturingCosts[{i}].ProductionVolumeRationale", entry.ProductionVolumeRationale);
            ValidateRationale(errors, $"YearlyManufacturingCosts[{i}].UnitCostRationale", entry.UnitCostRationale);
            ValidateRationale(errors, $"YearlyManufacturingCosts[{i}].AvgDistributionExpenseRationale", entry.AvgDistributionExpenseRationale);
        }

        return errors;
    }

    private static void ValidateRationale(Dictionary<string, string[]> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 20 || value.Length > 500)
        {
            errors[fieldName] = [$"{fieldName} must be between 20 and 500 characters."];
        }
    }

    /// <summary>
    /// Request model for submitting a Manufacturing response.
    /// </summary>
    public record SubmitManufacturingResponseRequest
    {
        public string Location { get; init; } = string.Empty;
        public string ParticipationType { get; init; } = string.Empty;
        public string ParticipationProposal { get; init; } = string.Empty;
        public List<YearlyManufacturingCostRequest> YearlyManufacturingCosts { get; init; } = [];
    }

    /// <summary>
    /// One year of a requested manufacturing cost projection.
    /// </summary>
    public record YearlyManufacturingCostRequest
    {
        public int Year { get; init; }
        public int ProductionVolume { get; init; }
        public string ProductionVolumeRationale { get; init; } = string.Empty;
        public decimal UnitCost { get; init; }
        public string UnitCostRationale { get; init; } = string.Empty;
        public decimal AverageGlobalDistributionExpense { get; init; }
        public string AvgDistributionExpenseRationale { get; init; } = string.Empty;
    }

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

    /// <summary>
    /// Register the POST /innovations/{innovationId}/bids/manufacturing endpoint
    /// </summary>
    public static void MapSubmitManufacturingResponse(this WebApplication app)
    {
        app.MapPost("/innovations/{innovationId}/bids/manufacturing", Handle)
            .WithName("SubmitManufacturingResponse")
            .WithTags("Bids")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<ActorResolutionFilter>()
            .Produces<SubmitResponseResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
