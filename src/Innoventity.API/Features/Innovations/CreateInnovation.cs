using System.Security.Claims;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Innovations;

/// <summary>
/// POST /innovations - Create innovation draft (Phase 0.6: US2 - Innovation Draft Management)
/// Spec: §US2, §R2.1 (completeness optional for drafts), §R2.2 (ownership)
/// </summary>
public static class CreateInnovation
{
    /// <summary>
    /// Register the POST /innovations endpoint
    /// </summary>
    public static void MapCreateInnovation(this WebApplication app)
    {
        app.MapPost("/innovations", async (
            [FromBody] CreateInnovationRequest request,
            ClaimsPrincipal user,
            AppDbContext context) =>
        {
            // Authorization check: Actor must be IdeaGenerator (Spec §US2)
            var actorType = user.FindFirst("actorType")?.Value;
            if (actorType != "IdeaGenerator")
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only Idea Generator actors can create innovations.");
            }

            // Extract actor ID from JWT claims
            var actorIdClaim = user.FindFirst("actorId")?.Value;
            if (actorIdClaim == null || !Guid.TryParse(actorIdClaim, out var actorId))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Invalid actor ID in authentication token.");
            }

            // Verify actor exists in database
            var actor = await context.Actors.FindAsync(actorId);
            if (actor == null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Actor not found.");
            }

            // Parse ResearchCategory enum
            if (!Enum.TryParse<ResearchCategory>(request.ResearchCategory, ignoreCase: true, out var researchCategory))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["researchCategory"] = new[] { "Must be one of: Management, Engineering, NaturalScience" }
                });
            }

            // Create innovation entity with Draft status (Spec §US2)
            var innovation = new Innovation
            {
                IdeaToken = Guid.NewGuid(), // Generate unique tracking token (Spec §R2.3)
                OwnerId = actorId, // Ownership assigned to authenticated actor (Spec §R2.2)
                IdeaSummary = new IdeaSummary
                {
                    Title = request.Title,
                    ProductType = request.ProductType,
                    ResearchBackground = request.ResearchBackground,
                    ResearchCategory = researchCategory,
                    IprStatus = request.HasIPR ? "Patent Pending" : "None"
                },
                Product = new Product
                {
                    ProductDescription = request.ProductDescription ?? string.Empty,
                    TechnologyDescription = request.TechnologyDescription ?? string.Empty,
                    ProductAdvantages = request.ProductAdvantages ?? string.Empty,
                    DevelopmentPhase = request.DevelopmentPhase ?? string.Empty,
                    DevelopmentProcess = request.DevelopmentProcess ?? string.Empty,
                    TargetBeneficiaries = request.TargetBeneficiaries ?? string.Empty,
                    ProductKeywords = request.ProductKeywords ?? string.Empty,
                    AdvantageKeywords = request.AdvantageKeywords ?? string.Empty
                },
                Market = new Market
                {
                    TargetMarket = request.TargetMarket ?? string.Empty,
                    TargetCustomerBase = request.TargetCustomerBase ?? string.Empty,
                    TargetCustomerType = request.TargetCustomerType ?? "B2B",
                    RelevantMarketSize = request.RelevantMarketSize,
                    PotentialMarketSize = request.PotentialMarketSize
                },
                CollaborationRequirement = new CollaborationRequirement
                {
                    PartnersNeeded = request.PartnersNeeded != null && request.PartnersNeeded.Any()
                        ? string.Join(",", request.PartnersNeeded)
                        : null
                },
                Status = InnovationStatus.Draft, // Initial status is Draft (Spec §US2)
                CreatedAt = DateTimeOffset.UtcNow,
                SubmittedAt = null // Not published yet
            };

            // Add target industries if provided
            if (request.TargetIndustryIds != null && request.TargetIndustryIds.Any())
            {
                var industries = await context.Industries
                    .Where(i => request.TargetIndustryIds.Contains(i.Id))
                    .ToListAsync();

                foreach (var industry in industries)
                {
                    innovation.TargetIndustries.Add(industry);
                }
            }

            context.Innovations.Add(innovation);
            await context.SaveChangesAsync();

            // Return 201 Created with innovation details (Spec §US2 API Contract)
            return Results.Created(
                $"/innovations/{innovation.Id}",
                new
                {
                    innovationId = innovation.Id,
                    ideaToken = innovation.IdeaToken,
                    status = innovation.Status.ToString(),
                    createdAt = innovation.CreatedAt,
                    ownerId = innovation.OwnerId
                });
        })
        .RequireAuthorization() // Requires authentication (Spec §US2 scenario 4)
        .WithName("CreateInnovation")
        .WithTags("Innovations")
        .WithOpenApi()
        .Produces<CreateInnovationResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}

/// <summary>
/// Request DTO for creating innovation draft (Spec §US2 API Contract)
/// </summary>
/// <remarks>
/// Completeness validation (§R2.1) is optional for drafts. Required fields enforce baseline data quality.
/// Full completeness validation enforced during publication (§US3).
/// </remarks>
public record CreateInnovationRequest
{
    /// <summary>
    /// Innovation title (required - Spec §R2.1)
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Product type description (required - Spec §US2)
    /// </summary>
    public required string ProductType { get; init; }

    /// <summary>
    /// Research category: Management, Engineering, or NaturalScience (required - Spec §R3.3)
    /// </summary>
    public required string ResearchCategory { get; init; }

    /// <summary>
    /// Research background description (required - Spec §R2.1)
    /// </summary>
    public required string ResearchBackground { get; init; }

    /// <summary>
    /// IPR declaration - has intellectual property rights (required - Spec §Journey 1 step 3)
    /// </summary>
    public required bool HasIPR { get; init; }

    /// <summary>
    /// Right to use declaration (required - Spec §Journey 1 step 3)
    /// </summary>
    public required bool HasRightToUse { get; init; }

    /// <summary>
    /// Product description (optional for drafts - Spec §US2 remarks)
    /// </summary>
    public string? ProductDescription { get; init; }

    /// <summary>
    /// Technology description (optional for drafts)
    /// </summary>
    public string? TechnologyDescription { get; init; }

    /// <summary>
    /// Product advantages (optional for drafts)
    /// </summary>
    public string? ProductAdvantages { get; init; }

    /// <summary>
    /// Development phase (optional for drafts)
    /// </summary>
    public string? DevelopmentPhase { get; init; }

    /// <summary>
    /// Development process (optional for drafts)
    /// </summary>
    public string? DevelopmentProcess { get; init; }

    /// <summary>
    /// Target beneficiaries (optional for drafts)
    /// </summary>
    public string? TargetBeneficiaries { get; init; }

    /// <summary>
    /// Relevant market size in USD (optional for drafts)
    /// </summary>
    public decimal? RelevantMarketSize { get; init; }

    /// <summary>
    /// Potential market size in USD (optional for drafts)
    /// </summary>
    public decimal? PotentialMarketSize { get; init; }

    /// <summary>
    /// Target market description (optional for drafts)
    /// </summary>
    public string? TargetMarket { get; init; }

    /// <summary>
    /// Target customer base (optional for drafts)
    /// </summary>
    public string? TargetCustomerBase { get; init; }

    /// <summary>
    /// Target customer type: B2B, B2C, B2G (optional for drafts)
    /// </summary>
    public string? TargetCustomerType { get; init; }

    /// <summary>
    /// Product keywords for discoverability (optional for drafts)
    /// </summary>
    public string? ProductKeywords { get; init; }

    /// <summary>
    /// Advantage keywords (optional for drafts)
    /// </summary>
    public string? AdvantageKeywords { get; init; }

    /// <summary>
    /// Target industry IDs (optional for drafts - Spec §US2 API Contract)
    /// </summary>
    public List<string>? TargetIndustryIds { get; init; }

    /// <summary>
    /// Partners needed: RD, Manufacturing, SalesMarketing, Investor (optional for drafts)
    /// </summary>
    public List<string>? PartnersNeeded { get; init; }
}

/// <summary>
/// Response DTO for created innovation (Spec §US2 API Contract)
/// </summary>
public record CreateInnovationResponse
{
    /// <summary>
    /// Innovation ID
    /// </summary>
    public required Guid InnovationId { get; init; }

    /// <summary>
    /// Unique tracking token for owner reference
    /// </summary>
    public required Guid IdeaToken { get; init; }

    /// <summary>
    /// Innovation status (Draft for new creations)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Owner actor ID
    /// </summary>
    public required Guid OwnerId { get; init; }
}
