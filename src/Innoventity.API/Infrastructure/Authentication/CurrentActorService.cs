using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;

namespace Innoventity.API.Infrastructure.Authentication;

public sealed class CurrentActorService : ICurrentActorService
{
    private readonly AppDbContext _db;

    public CurrentActorService(AppDbContext db) => _db = db;

    public async Task<Actor?> ResolveAsync(Guid actorId, CancellationToken ct = default)
        => await _db.Actors.FindAsync([actorId], ct);
}
