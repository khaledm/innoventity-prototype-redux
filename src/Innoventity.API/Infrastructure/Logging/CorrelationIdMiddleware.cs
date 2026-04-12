namespace Innoventity.API.Infrastructure.Logging;

/// <summary>
/// Middleware that propagates or generates an X-Correlation-ID header for every request.
/// The correlation ID is added to all structured log entries via an ILogger scope,
/// and echoed back to the caller in the response header.
/// </summary>
/// <remarks>
/// Satisfies FR7.6 structured-logging requirement: every log entry for a given request
/// includes the same correlation ID, enabling end-to-end tracing in Application Insights.
/// If the caller supplies an <c>X-Correlation-ID</c> request header its value is reused;
/// otherwise a new GUID is generated.
/// </remarks>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string HttpContextItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Expose on HttpContext for downstream handlers (e.g. exception handler)
        context.Items[HttpContextItemKey] = correlationId;

        // Echo back so callers can correlate their own logs
        context.Response.Headers[HeaderName] = correlationId;

        // Open a logging scope so every ILogger call within this request includes the ID
        using (logger.BeginScope(new Dictionary<string, object>
               {
                   [HttpContextItemKey] = correlationId
               }))
        {
            await next(context);
        }
    }
}

/// <summary>
/// Extension methods for registering <see cref="CorrelationIdMiddleware" />.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>Adds <see cref="CorrelationIdMiddleware" /> to the middleware pipeline.</summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
