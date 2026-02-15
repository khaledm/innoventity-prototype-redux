using Innoventity.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Industries;

/// <summary>
/// GET /industries endpoint - Returns industry master list (T009)
/// Spec §US4 Innovation Discovery - Industry Master List
/// No authentication required (public reference data)
/// </summary>
public static class GetIndustries
{
    /// <summary>
    /// Register the GET /industries endpoint
    /// </summary>
    public static void MapGetIndustries(this WebApplication app)
    {
        app.MapGet("/industries", async (AppDbContext context) =>
        {
            // Query all industries from database
            // No authentication required - this is public reference data
            var industries = await context.Industries
                .OrderBy(i => i.Name) // Alphabetical order for UI display
                .Select(i => new IndustryResponse(i.Id, i.Name))
                .ToListAsync();

            return Results.Ok(industries);
        })
        .WithName("GetIndustries")
        .WithTags("Industries")
        .Produces<List<IndustryResponse>>(StatusCodes.Status200OK);
    }
}

/// <summary>
/// Response for GET /industries endpoint
/// </summary>
/// <param name="IndustryId">Industry identifier (e.g., ELEC-001)</param>
/// <param name="Name">Industry name (e.g., Electronics)</param>
public record IndustryResponse(
    string IndustryId,
    string Name
);
