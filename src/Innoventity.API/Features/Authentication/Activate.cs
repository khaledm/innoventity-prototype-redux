using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Authentication;

public static class Activate
{
    public static void MapActivateEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/activate", async (
            ActivateRequest request,
            AppDbContext dbContext) =>
        {
            // Find actor by email and activation token
            var actor = await dbContext.Actors
                .FirstOrDefaultAsync(a => a.Email == request.Email);

            if (actor == null)
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Activation Failed",
                    status = 400,
                    detail = "Invalid email or activation token."
                });
            }

            // Check if already activated (R1.1)
            if (actor.AccountStatus == AccountStatus.Active)
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Activation Failed",
                    status = 400,
                    detail = "Account is already activated."
                });
            }

            // Validate activation token
            if (string.IsNullOrEmpty(actor.ActivationToken) || actor.ActivationToken != request.Token)
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Activation Failed",
                    status = 400,
                    detail = "Invalid email or activation token."
                });
            }

            // Activate account (R1.1)
            actor.AccountStatus = AccountStatus.Active;
            actor.ActivationToken = null; // Clear token after successful activation
            actor.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Account activated successfully. You can now log in.",
                email = actor.Email,
                accountStatus = actor.AccountStatus.ToString()
            });
        })
        .WithName("Activate")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Activate user account";
            operation.Description = "Activates a pending account using email and activation token. Changes account status from PendingActivation to Active.";
            return operation;
        });
    }

    public record ActivateRequest(
        [EmailAddress][Required] string Email,
        [Required] string Token
    );
}
