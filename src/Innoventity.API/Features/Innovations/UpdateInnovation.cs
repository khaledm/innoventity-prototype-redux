using System.Security.Claims;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Innovations;

/// <summary>
/// PUT /innovations/{id} - Update innovation draft (Phase 0.6: US2 - Innovation Draft Management)
/// Spec: §US2 Acceptance Scenarios 2-3, §R2.2 (ownership validation)
/// </summary>
public static class UpdateInnovation
{
    /// <summary>
    /// Register the PUT /innovations/{id} endpoint
    /// </summary>
    public static void MapUpdateInnovation(this WebApplication app)
    {
        app.MapPut("/innovations/{id:guid}", async (
            Guid id,
            [FromBody] UpdateInnovationRequest request,
            ClaimsPrincipal user,
            AppDbContext context) =>
        {
            // Extract actor ID from JWT claims
            var actorIdClaim = user.FindFirst("actorId")?.Value;
            if (actorIdClaim == null || !Guid.TryParse(actorIdClaim, out var actorId))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Invalid actor ID in authentication token.");
            }

            // Query innovation from database
            var innovation = await context.Innovations
                .Include(i => i.TargetIndustries)
                .FirstOrDefaultAsync(i => i.Id == id);

            // Return 404 if innovation doesn't exist (T006 Requirement)
            if (innovation == null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: $"Innovation with ID {id} not found.");
            }

            // Ownership validation: Check OwnerId matches authenticated actor (Spec §US2 Scenario 3)
            if (innovation.OwnerId != actorId)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only the innovation owner can update this innovation.");
            }

            // Status validation: Only Draft innovations can be edited (T006 Requirement)
            if (innovation.Status != InnovationStatus.Draft)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict",
                    detail: "Only draft innovations can be edited. Published innovations cannot be modified.");
            }

            // Partial update logic: Update only provided fields (null = no change)
            if (request.Title != null)
                innovation.Title = request.Title;

            if (request.ProductType != null)
                innovation.ProductType = request.ProductType;

            if (request.ResearchCategory != null)
            {
                if (!Enum.TryParse<ResearchCategory>(request.ResearchCategory, ignoreCase: true, out var researchCategory))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["researchCategory"] = new[] { "Must be one of: Management, Engineering, NaturalScience" }
                    });
                }
                innovation.ResearchCategory = researchCategory;
            }

            if (request.ResearchBackground != null)
                innovation.ResearchBackground = request.ResearchBackground;

            if (request.HasIPR.HasValue)
                innovation.IprStatus = request.HasIPR.Value ? "Patent Pending" : "None";

            if (request.ProductDescription != null)
                innovation.ProductDescription = request.ProductDescription;

            if (request.ProductAdvantages != null)
                innovation.ProductAdvantages = request.ProductAdvantages;

            if (request.DevelopmentPhase != null)
                innovation.DevelopmentPhase = request.DevelopmentPhase;

            if (request.DevelopmentProcess != null)
                innovation.DevelopmentProcess = request.DevelopmentProcess;

            if (request.TargetMarket != null)
                innovation.TargetMarket = request.TargetMarket;

            if (request.TargetCustomerBase != null)
                innovation.TargetCustomerBase = request.TargetCustomerBase;

            if (request.TargetCustomerType != null)
                innovation.TargetCustomerType = request.TargetCustomerType;

            if (request.ProductKeywords != null)
                innovation.ProductKeywords = request.ProductKeywords;

            if (request.AdvantageKeywords != null)
                innovation.AdvantageKeywords = request.AdvantageKeywords;

            // Update target industries if provided
            if (request.TargetIndustryIds != null)
            {
                // Clear existing and add new ones
                innovation.TargetIndustries.Clear();

                if (request.TargetIndustryIds.Any())
                {
                    var industries = await context.Industries
                        .Where(i => request.TargetIndustryIds.Contains(i.Id))
                        .ToListAsync();

                    foreach (var industry in industries)
                    {
                        innovation.TargetIndustries.Add(industry);
                    }
                }
            }

            // Note: Innovation entity in Phase 0.6 doesn't have UpdatedAt property
            // It will be added in future phases if needed
            await context.SaveChangesAsync();

            // Return 200 OK with updated innovation details (Spec §US2 API Contract)
            return Results.Ok(new
            {
                innovationId = innovation.Id,
                title = innovation.Title,
                productType = innovation.ProductType,
                researchCategory = innovation.ResearchCategory.ToString(),
                status = innovation.Status.ToString(),
                modifiedAt = DateTimeOffset.UtcNow, // Use current timestamp for response
                ownerId = innovation.OwnerId
            });
        })
        .RequireAuthorization() // Requires authentication
        .WithName("UpdateInnovation")
        .WithTags("Innovations")
        .WithOpenApi()
        .Produces<UpdateInnovationResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// Request DTO for updating innovation draft (Spec §US2 API Contract)
/// </summary>
/// <remarks>
/// All fields are optional - partial update pattern. Null values indicate "no change".
/// Only Draft status innovations can be updated (T006 requirement).
/// </remarks>
public record UpdateInnovationRequest
{
    /// <summary>
    /// Innovation title (optional - null = no change)
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Product type description (optional - null = no change)
    /// </summary>
    public string? ProductType { get; init; }

    /// <summary>
    /// Research category: Management, Engineering, or NaturalScience (optional - null = no change)
    /// </summary>
    public string? ResearchCategory { get; init; }

    /// <summary>
    /// Research background description (optional - null = no change)
    /// </summary>
    public string? ResearchBackground { get; init; }

    /// <summary>
    /// IPR declaration - has intellectual property rights (optional - null = no change)
    /// </summary>
    public bool? HasIPR { get; init; }

    /// <summary>
    /// Product description (optional - null = no change)
    /// </summary>
    public string? ProductDescription { get; init; }

    /// <summary>
    /// Product advantages (optional - null = no change)
    /// </summary>
    public string? ProductAdvantages { get; init; }

    /// <summary>
    /// Development phase (optional - null = no change)
    /// </summary>
    public string? DevelopmentPhase { get; init; }

    /// <summary>
    /// Development process (optional - null = no change)
    /// </summary>
    public string? DevelopmentProcess { get; init; }

    /// <summary>
    /// Target market description (optional - null = no change)
    /// </summary>
    public string? TargetMarket { get; init; }

    /// <summary>
    /// Target customer base (optional - null = no change)
    /// </summary>
    public string? TargetCustomerBase { get; init; }

    /// <summary>
    /// Target customer type: B2B, B2C, B2G (optional - null = no change)
    /// </summary>
    public string? TargetCustomerType { get; init; }

    /// <summary>
    /// Product keywords for discoverability (optional - null = no change)
    /// </summary>
    public string? ProductKeywords { get; init; }

    /// <summary>
    /// Advantage keywords (optional - null = no change)
    /// </summary>
    public string? AdvantageKeywords { get; init; }

    /// <summary>
    /// Target industry IDs (optional - null = no change, empty list = clear all)
    /// </summary>
    public List<string>? TargetIndustryIds { get; init; }

    /// <summary>
    /// Partners needed: RD, Manufacturing, SalesMarketing, Investor (optional - null = no change)
    /// </summary>
    public List<string>? PartnersNeeded { get; init; }
}

/// <summary>
/// Response DTO for updated innovation (Spec §US2 API Contract)
/// </summary>
public record UpdateInnovationResponse
{
    /// <summary>
    /// Innovation ID
    /// </summary>
    public required Guid InnovationId { get; init; }

    /// <summary>
    /// Updated title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Product type
    /// </summary>
    public required string ProductType { get; init; }

    /// <summary>
    /// Research category
    /// </summary>
    public required string ResearchCategory { get; init; }

    /// <summary>
    /// Innovation status (should be Draft after update)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Modification timestamp (T006 requirement)
    /// </summary>
    public required DateTimeOffset ModifiedAt { get; init; }

    /// <summary>
    /// Owner actor ID
    /// </summary>
    public required Guid OwnerId { get; init; }
}
