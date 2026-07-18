using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for submitting a Sales &amp; Marketing formal response with structured yearly
/// revenue projections (Spec 005 US2).
/// </summary>
public static class SubmitSalesMarketingResponse
{
    /// <summary>
    /// Submit a Sales &amp; Marketing partnership response for an innovation
    /// </summary>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid innovationId,
        SubmitSalesMarketingResponseRequest request,
        HttpContext httpContext,
        AppDbContext db)
    {
        var actor = httpContext.GetCurrentActor();

        if (actor.ActorType != ActorType.SalesMarketing)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Only Sales & Marketing actors can submit a sales response.");
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

        if (string.IsNullOrWhiteSpace(request.ParticipationType) || request.ParticipationType.Length > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ParticipationType"] = ["Participation type is required and must be 100 characters or fewer."]
            });
        }

        if (!Enum.TryParse<GeographicRegion>(request.Location, ignoreCase: true, out var location))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Location"] = ["Location must be one of: Asia, Americas, Europe, Africa, Oceania."]
            });
        }

        var yearlySales = request.YearlySales ?? [];
        var projectionErrors = ValidateProjection(yearlySales);
        if (projectionErrors.Count > 0)
        {
            return Results.ValidationProblem(projectionErrors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var response = new SalesMarketingResponse(Guid.NewGuid())
        {
            InnovationId = innovationId,
            ActorId = actor.Id,
            Location = location,
            ParticipationType = request.ParticipationType,
            ParticipationProposal = request.ParticipationProposal,
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
            YearlySales = yearlySales.Select(y => new YearlySale
            {
                Year = y.Year,
                UnitsSold = y.UnitsSold,
                UnitsSoldRationale = y.UnitsSoldRationale,
                UnitPrice = y.UnitPrice,
                UnitPriceRationale = y.UnitPriceRationale,
                SalesMarketingExpense = y.SalesMarketingExpense,
                SalesMarketingExpenseRationale = y.SalesMarketingExpenseRationale
            }).ToList()
        };

        db.FormalResponses.Add(response);
        await db.SaveChangesAsync();

        return Results.Created($"/innovations/{innovationId}/bids/sales/{response.Id}", new SubmitResponseResult
        {
            ResponseId = response.Id,
            ResponseType = nameof(SalesMarketingResponse),
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
    internal static Dictionary<string, string[]> ValidateProjection(List<YearlySaleRequest>? entries)
    {
        var errors = new Dictionary<string, string[]>();
        entries ??= [];

        if (entries.Count == 0)
        {
            errors["YearlySales"] = ["At least one yearly projection entry is required."];
            return errors;
        }

        var years = entries.Select(e => e.Year).OrderBy(y => y).ToList();
        var distinctYears = years.Distinct().OrderBy(y => y).ToList();
        var invalidYears = distinctYears.Where(y => y < 1).ToList();
        var duplicateYears = years
            .GroupBy(y => y)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(y => y)
            .ToList();

        if (invalidYears.Count > 0)
        {
            errors["YearlySales"] = [$"Projection years must be contiguous starting from year 1 — invalid year(s): {string.Join(", ", invalidYears)}."];
        }
        else if (duplicateYears.Count > 0)
        {
            errors["YearlySales"] = [$"Projection years must be contiguous starting from year 1, with no duplicates — duplicate year(s): {string.Join(", ", duplicateYears)}."];
        }
        else
        {
            var expectedYears = Enumerable.Range(1, distinctYears.Count).ToList();
            if (!distinctYears.SequenceEqual(expectedYears))
            {
                var missingYears = expectedYears.Except(distinctYears).OrderBy(y => y).ToList();
                errors["YearlySales"] = missingYears.Count > 0
                    ? [$"Projection years must be contiguous starting from year 1 — missing year(s): {string.Join(", ", missingYears)}."]
                    : ["Projection years must be contiguous starting from year 1, with no gaps or duplicates."];
            }
        }

        if (years.Max() > 10)
        {
            errors["YearlySales.Year"] = ["Projection years cannot exceed 10."];
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            ValidateNonNegative(errors, $"YearlySales[{i}].UnitsSold", entry.UnitsSold);
            ValidateNonNegative(errors, $"YearlySales[{i}].UnitPrice", entry.UnitPrice);
            ValidateNonNegative(errors, $"YearlySales[{i}].SalesMarketingExpense", entry.SalesMarketingExpense);
            ValidateRationale(errors, $"YearlySales[{i}].UnitsSoldRationale", entry.UnitsSoldRationale);
            ValidateRationale(errors, $"YearlySales[{i}].UnitPriceRationale", entry.UnitPriceRationale);
            ValidateRationale(errors, $"YearlySales[{i}].SalesMarketingExpenseRationale", entry.SalesMarketingExpenseRationale);
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

    private static void ValidateNonNegative(Dictionary<string, string[]> errors, string fieldName, decimal value)
    {
        if (value < 0)
        {
            errors[fieldName] = [$"{fieldName} must be greater than or equal to 0."];
        }
    }

    private static void ValidateNonNegative(Dictionary<string, string[]> errors, string fieldName, int value)
    {
        if (value < 0)
        {
            errors[fieldName] = [$"{fieldName} must be greater than or equal to 0."];
        }
    }

    /// <summary>
    /// Request model for submitting a Sales &amp; Marketing response.
    /// </summary>
    public record SubmitSalesMarketingResponseRequest
    {
        public string Location { get; init; } = string.Empty;
        public string ParticipationType { get; init; } = string.Empty;
        public string ParticipationProposal { get; init; } = string.Empty;
        public List<YearlySaleRequest> YearlySales { get; init; } = [];
    }

    /// <summary>
    /// One year of a requested sales projection.
    /// </summary>
    public record YearlySaleRequest
    {
        public int Year { get; init; }
        public int UnitsSold { get; init; }
        public string UnitsSoldRationale { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        public string UnitPriceRationale { get; init; } = string.Empty;
        public decimal SalesMarketingExpense { get; init; }
        public string SalesMarketingExpenseRationale { get; init; } = string.Empty;
    }

    /// <summary>
    /// Register the POST /innovations/{innovationId}/bids/sales endpoint
    /// </summary>
    public static void MapSubmitSalesMarketingResponse(this WebApplication app)
    {
        app.MapPost("/innovations/{innovationId}/bids/sales", Handle)
            .WithName("SubmitSalesMarketingResponse")
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
