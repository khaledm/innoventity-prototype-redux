using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Bids;

/// <summary>
/// Endpoint for retrieving all formal responses submitted for an innovation (Spec 005 §R6.1)
/// </summary>
public static class GetBids
{
    /// <summary>
    /// Retrieve all formal responses for an innovation, with 3-tier visibility
    /// </summary>
    /// <remarks>
    /// Business Rules (Spec 005):
    /// - Any authenticated actor may call this endpoint
    /// - Innovation owner sees full data (proposal + financial projections + rationale) for every response
    /// - The actor who submitted a response sees their own full data; all other responses show public fields only
    /// - All other authenticated actors see public summary fields only for every response
    /// - Optional `type` filter: manufacturing, sales, rd, investor
    /// </remarks>
    /// <param name="innovationId">The innovation to retrieve responses for</param>
    /// <param name="type">Optional response-type filter</param>
    /// <param name="httpContext">HTTP context — actor resolved by ActorResolutionFilter</param>
    /// <param name="db">Database context (injected by ASP.NET Core)</param>
    /// <response code="200">Responses retrieved successfully</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="404">Innovation not found</response>
    [Authorize]
    public static async Task<IResult> Handle(
        Guid innovationId,
        string? type,
        HttpContext httpContext,
        AppDbContext db)
    {
        var actor = httpContext.GetCurrentActor();

        var innovation = await db.Innovations.FindAsync(innovationId);
        if (innovation == null)
        {
            return Results.NotFound(new { error = "Innovation not found." });
        }

        var isOwner = innovation.OwnerId == actor.Id;

        var query = db.FormalResponses.Where(r => r.InnovationId == innovationId);

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = type.Trim().ToLowerInvariant() switch
            {
                "manufacturing" => query.OfType<ManufacturingResponse>(),
                "sales" => query.OfType<SalesMarketingResponse>(),
                "rd" => query.OfType<ResearchDevelopmentResponse>(),
                "investor" => query.OfType<InvestorResponse>(),
                _ => query.Where(r => false) // unrecognized filter value — no responses can match
            };
        }

        var responses = await query
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();

        var dtos = responses
            .Select(r => BuildDto(r, fullVisibility: isOwner || r.ActorId == actor.Id))
            .ToList();

        return Results.Ok(new GetBidsResponse
        {
            InnovationId = innovationId,
            Responses = dtos
        });
    }

    /// <summary>
    /// Build the JSON-serializable projection for a single response, shaping the payload
    /// by the caller's visibility tier (Spec 005 3-tier visibility).
    /// </summary>
    private static object BuildDto(FormalResponse r, bool fullVisibility)
    {
        if (!fullVisibility)
        {
            return new
            {
                responseId = r.Id,
                responseType = GetResponseTypeName(r),
                actorId = r.ActorId,
                location = r.Location.ToString(),
                participationType = r.ParticipationType,
                status = r.Status.ToString(),
                submittedAt = r.SubmittedAt
            };
        }

        return r switch
        {
            ManufacturingResponse m => new
            {
                responseId = m.Id,
                responseType = GetResponseTypeName(m),
                actorId = m.ActorId,
                location = m.Location.ToString(),
                participationType = m.ParticipationType,
                participationProposal = m.ParticipationProposal,
                status = m.Status.ToString(),
                submittedAt = m.SubmittedAt,
                yearlyManufacturingCosts = m.YearlyManufacturingCosts
            },
            SalesMarketingResponse s => new
            {
                responseId = s.Id,
                responseType = GetResponseTypeName(s),
                actorId = s.ActorId,
                location = s.Location.ToString(),
                participationType = s.ParticipationType,
                participationProposal = s.ParticipationProposal,
                status = s.Status.ToString(),
                submittedAt = s.SubmittedAt,
                yearlySales = s.YearlySales
            },
            ResearchDevelopmentResponse d => new
            {
                responseId = d.Id,
                responseType = GetResponseTypeName(d),
                actorId = d.ActorId,
                location = d.Location.ToString(),
                participationType = d.ParticipationType,
                participationProposal = d.ParticipationProposal,
                status = d.Status.ToString(),
                submittedAt = d.SubmittedAt,
                productDevelopmentDuration = d.ProductDevelopmentDuration,
                yearlyDevelopmentCosts = d.YearlyDevelopmentCosts
            },
            InvestorResponse inv => new
            {
                responseId = inv.Id,
                responseType = GetResponseTypeName(inv),
                actorId = inv.ActorId,
                location = inv.Location.ToString(),
                participationType = inv.ParticipationType,
                participationProposal = inv.ParticipationProposal,
                status = inv.Status.ToString(),
                submittedAt = inv.SubmittedAt,
                feedback = inv.Feedback
            },
            _ => new
            {
                responseId = r.Id,
                responseType = GetResponseTypeName(r),
                actorId = r.ActorId,
                location = r.Location.ToString(),
                participationType = r.ParticipationType,
                participationProposal = r.ParticipationProposal,
                status = r.Status.ToString(),
                submittedAt = r.SubmittedAt
            }
        };
    }

    private static string GetResponseTypeName(FormalResponse response) => response switch
    {
        ManufacturingResponse => nameof(ManufacturingResponse),
        SalesMarketingResponse => nameof(SalesMarketingResponse),
        ResearchDevelopmentResponse => nameof(ResearchDevelopmentResponse),
        InvestorResponse => nameof(InvestorResponse),
        _ => response.GetType().Name
    };

    /// <summary>
    /// Response model for GET /innovations/{innovationId}/bids
    /// </summary>
    public class GetBidsResponse
    {
        /// <summary>The innovation the responses belong to</summary>
        public Guid InnovationId { get; set; }

        /// <summary>
        /// Formal responses submitted for the innovation, shaped per-entry by the
        /// caller's visibility tier and the response's discriminator type.
        /// </summary>
        public List<object> Responses { get; set; } = [];
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
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
