using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for submitting an Investor formal response — a lightweight feedback response with
/// no financial projections (Spec 005 US4).
/// </summary>
public static class SubmitInvestorResponse
{
    /// <summary>
    /// Submit an Investor partnership response for an innovation
    /// </summary>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid innovationId,
        SubmitInvestorResponseRequest request,
        HttpContext httpContext,
        AppDbContext db)
    {
        var actor = httpContext.GetCurrentActor();

        if (actor.ActorType != ActorType.Investor)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Only Investor actors can submit an investor response.");
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

        if (string.IsNullOrWhiteSpace(request.Feedback) || request.Feedback.Length < 50 || request.Feedback.Length > 2000)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Feedback"] = ["Feedback must be between 50 and 2000 characters."]
            }, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var response = new InvestorResponse(Guid.NewGuid())
        {
            InnovationId = innovationId,
            ActorId = actor.Id,
            Location = location,
            ParticipationType = request.ParticipationType,
            ParticipationProposal = request.ParticipationProposal,
            Status = ResponseStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
            Feedback = request.Feedback
        };

        db.FormalResponses.Add(response);
        await db.SaveChangesAsync();

        return Results.Created($"/innovations/{innovationId}/bids/investor/{response.Id}", new SubmitManufacturingResponse.SubmitResponseResult
        {
            ResponseId = response.Id,
            ResponseType = nameof(InvestorResponse),
            InnovationId = response.InnovationId,
            ActorId = response.ActorId,
            Status = response.Status.ToString(),
            SubmittedAt = response.SubmittedAt
        });
    }

    /// <summary>
    /// Request model for submitting an Investor response.
    /// </summary>
    public record SubmitInvestorResponseRequest
    {
        public string Location { get; init; } = string.Empty;
        public string ParticipationType { get; init; } = string.Empty;
        public string ParticipationProposal { get; init; } = string.Empty;
        public string Feedback { get; init; } = string.Empty;
    }

    /// <summary>
    /// Register the POST /innovations/{innovationId}/bids/investor endpoint
    /// </summary>
    public static void MapSubmitInvestorResponse(this WebApplication app)
    {
        app.MapPost("/innovations/{innovationId}/bids/investor", Handle)
            .WithName("SubmitInvestorResponse")
            .WithTags("Bids")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<ActorResolutionFilter>()
            .Produces<SubmitManufacturingResponse.SubmitResponseResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
