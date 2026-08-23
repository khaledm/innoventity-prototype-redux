using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Innovations;

/// <summary>
/// GET /innovations - List published innovations with filtering (Phase 0.6: US4 - Innovation Discovery)
/// Spec: §US4, §R3.2 (discovery filtering), §R3.3 (research category)
/// </summary>
public static class ListInnovations
{
    /// <summary>
    /// Register the GET /innovations endpoint
    /// </summary>
    public static void MapListInnovations(this WebApplication app)
    {
        app.MapGet("/innovations", async (
            AppDbContext context,
            [FromQuery] string? industryId,
            [FromQuery] string? researchCategory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            // Start with base query: only Published innovations (not Drafts)
            // Spec §US4 Scenario 2: "returns all published innovations (not drafts)"
            var query = context.Innovations
                .Include(i => i.Owner)
                .Include(i => i.TargetIndustries)
                .Where(i => i.Status == InnovationStatus.Published)
                .AsQueryable();

            // Filter by industry if provided
            // Spec §US4 Scenario 1: "industryId=ELEC-001, returns 2 Electronics innovations only"
            if (!string.IsNullOrWhiteSpace(industryId))
            {
                query = query.Where(i => i.TargetIndustries.Any(ti => ti.Id == industryId));
            }

            // Filter by research category if provided
            // Spec §US4 Scenario 3: "researchCategory=Engineering, returns only Engineering category innovations"
            if (!string.IsNullOrWhiteSpace(researchCategory))
            {
                if (Enum.TryParse<ResearchCategory>(researchCategory, ignoreCase: true, out var category))
                {
                    query = query.Where(i => i.IdeaSummary.ResearchCategory == category);
                }
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            // Spec §US4 API Contract: page and pageSize parameters
            var skip = (page - 1) * pageSize;
            var innovations = await query
                .OrderByDescending(i => i.SubmittedAt) // Most recent first
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // Map to response DTOs
            var items = innovations.Select(i => new
            {
                innovationId = i.Id,
                ideaSummary = new
                {
                    title = i.IdeaSummary.Title,
                    productType = i.IdeaSummary.ProductType,
                    researchCategory = i.IdeaSummary.ResearchCategory.ToString()
                },
                status = i.Status.ToString(),
                submittedAt = i.SubmittedAt,
                owner = new
                {
                    actorId = i.OwnerId,
                    firstName = i.Owner?.FirstName,
                    lastName = i.Owner?.LastName,
                    displayName = $"{i.Owner?.FirstName} {i.Owner?.LastName}".Trim()
                },
                targetIndustries = i.TargetIndustries.Select(ti => ti.Name).ToList(),
                partnersNeeded = (i.CollaborationRequirement.PartnersNeeded ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
            }).ToList();

            // Return paginated results with metadata
            // Spec §US4 API Contract response format
            return Results.Ok(new
            {
                items,
                totalCount,
                page,
                pageSize
            });
        })
        .RequireAuthorization() // Spec §US4 Scenario 4: "Unauthenticated request returns 401 Unauthorized"
        .WithName("ListInnovations")
        .WithTags("Innovations")
        .WithOpenApi()
        .Produces<ListInnovationsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

/// <summary>
/// Response DTO for listing innovations (Spec §US4 API Contract)
/// </summary>
public record ListInnovationsResponse
{
    /// <summary>
    /// List of innovations matching filter criteria
    /// </summary>
    public required List<InnovationListItem> Items { get; init; }

    /// <summary>
    /// Total count of innovations matching filter (before pagination)
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Page size
    /// </summary>
    public required int PageSize { get; init; }
}

/// <summary>
/// Innovation list item DTO (Spec §US4 API Contract)
/// </summary>
public record InnovationListItem
{
    /// <summary>
    /// Innovation ID
    /// </summary>
    public required Guid InnovationId { get; init; }

    /// <summary>
    /// Innovation title
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
    /// Innovation status (should be "Published" for discovery)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Submission timestamp
    /// </summary>
    public required DateTimeOffset? SubmittedAt { get; init; }

    /// <summary>
    /// Innovation owner information
    /// </summary>
    public required OwnerInfo Owner { get; init; }

    /// <summary>
    /// Target industries
    /// </summary>
    public required List<string> TargetIndustries { get; init; }

    /// <summary>
    /// Partners needed (actor types)
    /// </summary>
    public required List<string> PartnersNeeded { get; init; }
}

/// <summary>
/// Owner information DTO
/// </summary>
public record OwnerInfo
{
    /// <summary>
    /// Actor ID
    /// </summary>
    public required Guid ActorId { get; init; }

    /// <summary>
    /// First name
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Last name
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Display name
    /// </summary>
    public required string DisplayName { get; init; }
}
