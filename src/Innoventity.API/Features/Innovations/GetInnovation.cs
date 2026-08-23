using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Innoventity.API.Domain.Entities;

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
                ideaSummary = new
                {
                    title = innovation.IdeaSummary.Title,
                    productType = innovation.IdeaSummary.ProductType,
                    researchBackground = innovation.IdeaSummary.ResearchBackground,
                    researchCategory = innovation.IdeaSummary.ResearchCategory.ToString(),
                    iprStatus = innovation.IdeaSummary.IprStatus
                },
                product = new
                {
                    productDescription = innovation.Product.ProductDescription,
                    technologyDescription = innovation.Product.TechnologyDescription,
                    productAdvantages = innovation.Product.ProductAdvantages,
                    developmentPhase = innovation.Product.DevelopmentPhase,
                    developmentProcess = innovation.Product.DevelopmentProcess,
                    targetBeneficiaries = innovation.Product.TargetBeneficiaries,
                    productKeywords = innovation.Product.ProductKeywords,
                    advantageKeywords = innovation.Product.AdvantageKeywords
                },
                market = new
                {
                    targetMarket = innovation.Market.TargetMarket,
                    targetCustomerBase = innovation.Market.TargetCustomerBase,
                    targetCustomerType = innovation.Market.TargetCustomerType,
                    relevantMarketSize = innovation.Market.RelevantMarketSize,
                    potentialMarketSize = innovation.Market.PotentialMarketSize
                },
                collaborationRequirement = new
                {
                    partnersNeeded = innovation.CollaborationRequirement.PartnersNeeded
                },
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
