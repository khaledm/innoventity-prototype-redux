using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Innovations;

/// <summary>
/// GET /innovations/{id} - View innovation details (Phase 0: read-only, authenticated-only)
/// Spec: §R3.1, §R3.2, §R3.3, §R3.4
/// </summary>
public static class GetInnovation
{
    /// <summary>
    /// Register the GET /innovations/{id} endpoint
    /// </summary>
    public static void MapGetInnovation(this WebApplication app)
    {
        app.MapGet("/innovations/{id}", async (
            [FromRoute] Guid id,
            AppDbContext context) =>
        {
            // T056: Validation for non-existent innovation ID
            var innovation = await context.Innovations
                .Include(i => i.Owner)
                .Include(i => i.TargetIndustries)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (innovation == null)
            {
                return Results.NotFound();
            }

            // Return innovation with all details
            return Results.Ok(new
            {
                id = innovation.Id,
                ideaToken = innovation.IdeaToken,
                ownerId = innovation.OwnerId,
                owner = innovation.Owner != null ? new
                {
                    id = innovation.Owner.Id,
                    firstName = innovation.Owner.FirstName,
                    lastName = innovation.Owner.LastName,
                    displayName = innovation.Owner.DisplayName,
                    email = innovation.Owner.Email,
                    actorType = innovation.Owner.ActorType.ToString()
                } : null,
                title = innovation.Title,
                productType = innovation.ProductType,
                researchBackground = innovation.ResearchBackground,
                researchCategory = innovation.ResearchCategory.ToString(),
                iprStatus = innovation.IprStatus,
                productDescription = innovation.ProductDescription,
                productAdvantages = innovation.ProductAdvantages,
                developmentPhase = innovation.DevelopmentPhase,
                developmentProcess = innovation.DevelopmentProcess,
                targetMarket = innovation.TargetMarket,
                targetCustomerBase = innovation.TargetCustomerBase,
                targetCustomerType = innovation.TargetCustomerType,
                productKeywords = innovation.ProductKeywords,
                advantageKeywords = innovation.AdvantageKeywords,
                status = innovation.Status.ToString(),
                createdAt = innovation.CreatedAt,
                submittedAt = innovation.SubmittedAt,
                targetIndustries = innovation.TargetIndustries.Select(i => new
                {
                    industryId = i.Id,  // Renamed from IndustryId (R9.1)
                    name = i.Name
                }).ToList()
            });
        })
        .RequireAuthorization() // T055: Add [Authorize] attribute
        .WithName("GetInnovation")
        .WithTags("Innovations")
        .WithOpenApi();
    }
}
