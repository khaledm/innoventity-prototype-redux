using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;

namespace Innoventity.API.Features.Authentication;

public static class RefreshToken
{
    public record RefreshTokenRequest(
        [Required] string RefreshToken
    );

    public record RefreshTokenResponse(
        string AccessToken,
        int ExpiresIn,
        string TokenType
    );

    public static void MapRefreshTokenEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh-token", async (
            [FromBody] RefreshTokenRequest request,
            AppDbContext dbContext,
            JwtTokenService jwtTokenService,
            IConfiguration configuration) =>
        {
            try
            {
                // Validate refresh token
                var tokenHandler = new JwtSecurityTokenHandler();
                var signingKey = configuration["Jwt:SigningKey"]
                    ?? throw new InvalidOperationException("JWT SigningKey not configured");

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(request.RefreshToken, validationParameters, out var validatedToken);

                // Verify token type is refresh
                var tokenTypeClaim = principal.FindFirst("tokenType")?.Value;
                if (tokenTypeClaim != "refresh")
                {
                    return Results.Problem(
                        detail: "The refresh token is invalid or has expired",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Invalid Refresh Token"
                    );
                }

                // Extract actor ID from token
                var actorIdClaim = principal.FindFirst("actorId")?.Value;
                if (string.IsNullOrEmpty(actorIdClaim) || !Guid.TryParse(actorIdClaim, out var actorId))
                {
                    return Results.Problem(
                        detail: "The refresh token is invalid or has expired",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Invalid Refresh Token"
                    );
                }

                // Fetch actor from database
                var actor = await dbContext.Actors.FindAsync(actorId);
                if (actor == null || actor.AccountStatus != Domain.Entities.AccountStatus.Active)
                {
                    return Results.Problem(
                        detail: "The refresh token is invalid or has expired",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Invalid Refresh Token"
                    );
                }

                // Generate new access token
                var newAccessToken = jwtTokenService.GenerateAccessToken(
                    actor.Id,
                    actor.ActorType.ToString(),
                    actor.Email
                );

                var response = new RefreshTokenResponse(
                    AccessToken: newAccessToken,
                    ExpiresIn: 3600, // 1 hour in seconds
                    TokenType: "Bearer"
                );

                return Results.Ok(response);
            }
            catch (SecurityTokenExpiredException)
            {
                return Results.Problem(
                    detail: "The refresh token is invalid or has expired",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid Refresh Token"
                );
            }
            catch (Exception)
            {
                return Results.Problem(
                    detail: "The refresh token is invalid or has expired",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid Refresh Token"
                );
            }
        })
        .WithName("RefreshToken")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Refresh access token";
            operation.Description = "Obtains new access token using valid refresh token. Refresh tokens expire after 7 days of inactivity.";
            return operation;
        });
    }
}
