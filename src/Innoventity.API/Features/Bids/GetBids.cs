using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

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
            var normalizedType = type.Trim().ToLowerInvariant();
            IQueryable<FormalResponse>? filteredQuery;

            switch (normalizedType)
            {
                case "manufacturing":
                    filteredQuery = query.OfType<ManufacturingResponse>();
                    break;
                case "sales":
                    filteredQuery = query.OfType<SalesMarketingResponse>();
                    break;
                case "rd":
                    filteredQuery = query.OfType<ResearchDevelopmentResponse>();
                    break;
                case "investor":
                    filteredQuery = query.OfType<InvestorResponse>();
                    break;
                default:
                    filteredQuery = null;
                    break;
            }

            if (filteredQuery is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["type"] =
                    [
                        "Invalid type. Accepted values: manufacturing, sales, rd, investor."
                    ]
                });
            }

            query = filteredQuery;
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
    private static GetBidsResponseItem BuildDto(FormalResponse r, bool fullVisibility)
    {
        var dto = new GetBidsResponseItem
        {
            ResponseId = r.Id,
            ResponseType = GetResponseTypeName(r),
            ActorId = r.ActorId,
            Location = r.Location.ToString(),
            ParticipationType = r.ParticipationType,
            Status = r.Status.ToString(),
            SubmittedAt = r.SubmittedAt
        };

        if (!fullVisibility)
        {
            return dto;
        }

        dto.ParticipationProposal = r.ParticipationProposal;

        switch (r)
        {
            case ManufacturingResponse m:
                dto.YearlyManufacturingCosts = m.YearlyManufacturingCosts;
                break;
            case SalesMarketingResponse s:
                dto.YearlySales = s.YearlySales;
                break;
            case ResearchDevelopmentResponse d:
                dto.ProductDevelopmentDuration = d.ProductDevelopmentDuration;
                dto.YearlyDevelopmentCosts = d.YearlyDevelopmentCosts;
                break;
            case InvestorResponse inv:
                dto.Feedback = inv.Feedback;
                break;
        }

        return dto;
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
        public List<GetBidsResponseItem> Responses { get; set; } = [];
    }

    /// <summary>
    /// Contract item for a single formal response returned by GET /innovations/{innovationId}/bids.
    /// </summary>
    public class GetBidsResponseItem
    {
        public Guid ResponseId { get; set; }
        public string ResponseType { get; set; } = string.Empty;
        public Guid ActorId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ParticipationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset SubmittedAt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ParticipationProposal { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ProductDevelopmentDuration { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Feedback { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<YearlyManufacturingCost>? YearlyManufacturingCosts { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<YearlySale>? YearlySales { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<YearlyDevelopmentCost>? YearlyDevelopmentCosts { get; set; }
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
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
