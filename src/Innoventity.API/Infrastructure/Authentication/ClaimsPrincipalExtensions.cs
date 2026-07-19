using System.Security.Claims;

namespace Innoventity.API.Infrastructure.Authentication;

public static class ClaimsPrincipalExtensions
{
    private const string SubjectClaimType = "sub";

    public static bool TryGetActorId(this ClaimsPrincipal principal, out Guid actorId)
    {
        actorId = Guid.Empty;
        var claim = principal.FindFirst(SubjectClaimType)
                 ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out actorId);
    }
}
