using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Authentication;

public static class Register
{
    public static void MapRegisterEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            AppDbContext dbContext,
            PasswordHasher passwordHasher) =>
        {
            // Validate password complexity (R8.4)
            var passwordValidation = ValidatePasswordComplexity(request.Password);
            if (!passwordValidation.IsValid)
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation Failed",
                    status = 400,
                    detail = passwordValidation.ErrorMessage
                });
            }

            // Parse ActorType
            if (!Enum.TryParse<ActorType>(request.ActorType, ignoreCase: true, out var actorType))
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation Failed",
                    status = 400,
                    detail = "Invalid actor type. Must be one of: IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor"
                });
            }

            // Check for duplicate email+ActorType (R1.3)
            var existingActor = await dbContext.Actors
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.ActorType == actorType);

            if (existingActor != null)
            {
                return Results.Conflict(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    title = "Duplicate Registration",
                    status = 409,
                    detail = "This email address is already registered. Please use a different email or log in to your existing account."
                });
            }

            // Create actor with pending activation (R1.1)
            var actor = new Actor
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                ContactAddress = request.ContactAddress,
                ActorType = actorType,
                AccountStatus = AccountStatus.PendingActivation,
                PasswordHash = passwordHasher.HashPassword(request.Password),
                ActivationToken = GenerateActivationToken(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Actors.Add(actor);
            await dbContext.SaveChangesAsync();

            // In production, send activation email here with actor.ActivationToken
            // For MVP/testing, we include the token in response (remove in production)

            return Results.Created($"/actors/{actor.Id}", new
            {
                actorId = actor.Id.ToString(),
                email = actor.Email,
                actorType = actor.ActorType.ToString(),
                accountStatus = actor.AccountStatus.ToString(),
                activationToken = actor.ActivationToken, // TODO: Remove in production - send via email service instead
                message = "Registration successful. Please check your email for the activation link."
            });
        })
        .WithName("Register")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Register a new actor account";
            operation.Description = "Creates a new user account with PendingActivation status. Requires email, full name, contact address, actor type, and password.";
            return operation;
        });
    }

    /// <summary>
    /// Validates password meets R8.4 complexity requirements:
    /// - Minimum 8 characters
    /// - Maximum 128 characters
    /// - At least 1 uppercase letter
    /// - At least 1 lowercase letter
    /// - At least 1 digit
    /// - At least 1 special character
    /// </summary>
    private static (bool IsValid, string? ErrorMessage) ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return (false, "Password is required.");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters and include uppercase, lowercase, digit, and special character.");
        }

        if (password.Length > 128)
        {
            return (false, "Password must not exceed 128 characters.");
        }

        var hasUppercase = Regex.IsMatch(password, @"[A-Z]");
        var hasLowercase = Regex.IsMatch(password, @"[a-z]");
        var hasDigit = Regex.IsMatch(password, @"[0-9]");
        var hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*()\-_=+\[\]{}|;:,.<>?]");

        if (!hasUppercase || !hasLowercase || !hasDigit || !hasSpecialChar)
        {
            return (false, "Password must be at least 8 characters and include uppercase, lowercase, digit, and special character.");
        }

        return (true, null);
    }

    /// <summary>
    /// Generates a cryptographically secure activation token (T026)
    /// </summary>
    private static string GenerateActivationToken()
    {
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    public record RegisterRequest(
        [EmailAddress] string Email,
        [Required] string FullName,
        [Required] string ContactAddress,
        [Required] string ActorType,
        [Required] string Password
    );
}
