using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Infrastructure.Authentication;

/// <summary>
/// Endpoint filter (GoF Chain of Responsibility) that validates the JWT-identified actor
/// exists in the database and injects the resolved Actor into HttpContext.Items before the
/// handler runs. Short-circuits with 401 if the claim is unparseable or the actor is absent.
/// </summary>
public sealed class ActorResolutionFilter : IEndpointFilter
{
    internal const string ActorItemKey = "CurrentActor";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        if (!ctx.HttpContext.User.TryGetActorId(out var actorId))
            return ActorUnresolvable();

        var service = ctx.HttpContext.RequestServices.GetRequiredService<ICurrentActorService>();
        var actor = await service.ResolveAsync(actorId, ctx.HttpContext.RequestAborted);

        if (actor == null)
            return ActorUnresolvable();

        ctx.HttpContext.Items[ActorItemKey] = actor;
        return await next(ctx);
    }

    private static IResult ActorUnresolvable() =>
        Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: "Actor identity could not be resolved.");
}

/// <summary>
/// Typed accessor so handlers can retrieve the pre-resolved Actor without magic strings.
/// </summary>
public static class HttpContextExtensions
{
    public static Actor GetCurrentActor(this HttpContext ctx) =>
        (Actor)ctx.Items[ActorResolutionFilter.ActorItemKey]!;
}
