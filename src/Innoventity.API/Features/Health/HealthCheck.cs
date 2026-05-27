using Innoventity.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Health;

public static class HealthCheck
{
    public static void MapHealthEndpoint(this WebApplication app)
    {
        app.MapGet("/health", async (AppDbContext dbContext, IConfiguration configuration, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(HealthCheck));
            var health = new
            {
                status = "Healthy",
                checks = new Dictionary<string, string>()
            };

            // Check database connectivity
            bool canConnect = false;
            try
            {
                canConnect = await dbContext.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database connectivity check failed");
                canConnect = false;
            }

            if (!canConnect)
            {
                health.checks["database"] = "Unhealthy";
                return Results.Json(new { status = "Unhealthy", checks = health.checks }, statusCode: 503);
            }
            health.checks["database"] = "Healthy";

            // Check JWT configuration
            var jwtSigningKey = configuration["Jwt:SigningKey"];
            if (string.IsNullOrEmpty(jwtSigningKey))
            {
                logger.LogError($"JWT configuration check failed. nameof(Jwt:SigningKey) is not set in configuration.");
                health.checks["jwt_config"] = "Unhealthy";
                return Results.Json(new { status = "Unhealthy", checks = health.checks }, statusCode: 503);
            }
            health.checks["jwt_config"] = "Healthy";

            return Results.Ok(health);
        })
        .WithName("HealthCheck")
        .WithTags("Health")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Check API health status";
            operation.Description = "Returns health status of the API including database connectivity and JWT configuration";
            return operation;
        });
    }
}
