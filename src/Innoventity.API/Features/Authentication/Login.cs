using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;

namespace Innoventity.API.Features.Authentication;

public static class Login
{
    public record LoginRequest(
        [Required] [EmailAddress] string Email,
        [Required] string ActorType,
        [Required] string Password
    );

    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string TokenType,
        ActorInfo Actor
    );

    public record ActorInfo(
        Guid ActorId,
        string Email,
        string FullName,
        string ActorType
    );

    public static void MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            [FromBody] LoginRequest request,
            AppDbContext dbContext,
            PasswordHasher passwordHasher,
            JwtTokenService jwtTokenService) =>
        {
            // Parse actor type
            if (!Enum.TryParse<ActorType>(request.ActorType, ignoreCase: true, out var actorType))
            {
                return Results.Problem(
                    detail: "Invalid actor type",
                    statusCode: StatusCodes.Status400BadRequest
                );
            }

            // Find actor by email and actor type
            var actor = await dbContext.Actors
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.ActorType == actorType);

            if (actor == null)
            {
                return Results.Problem(
                    detail: "Invalid email, actor type, or password",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed"
                );
            }

            // Verify password (T039)
            if (!passwordHasher.VerifyPassword(request.Password, actor.PasswordHash))
            {
                return Results.Problem(
                    detail: "Invalid email, actor type, or password",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed"
                );
            }

            // Check account status (T040) - only Active accounts can login
            if (actor.AccountStatus != AccountStatus.Active)
            {
                return Results.Problem(
                    detail: "Please activate your account using the link sent to your email",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Account Not Activated"
                );
            }

            // Generate tokens (T042 - JWT claims already implemented in JwtTokenService)
            var accessToken = jwtTokenService.GenerateAccessToken(actor.Id, actor.ActorType.ToString(), actor.Email);
            var refreshToken = jwtTokenService.GenerateRefreshToken(actor.Id);

            var response = new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresIn: 3600, // 1 hour in seconds
                TokenType: "Bearer",
                Actor: new ActorInfo(
                    ActorId: actor.Id,
                    Email: actor.Email,
                    FullName: actor.FullName,
                    ActorType: actor.ActorType.ToString()
                )
            );

            return Results.Ok(response);
        })
        .WithName("Login")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Authenticate and obtain JWT tokens";
            operation.Description = "Authenticates actor using email, actor type, and password. Returns access token (1 hour) and refresh token (7 days).";
            return operation;
        });
    }
}
