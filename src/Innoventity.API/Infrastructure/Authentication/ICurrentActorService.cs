using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Infrastructure.Authentication;

public interface ICurrentActorService
{
    Task<Actor?> ResolveAsync(Guid actorId, CancellationToken ct = default);
}
