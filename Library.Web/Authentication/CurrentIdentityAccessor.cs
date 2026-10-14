using System.Security.Claims;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Subjects;
using AethericForge.Runtime.Models.Identity.Primitives;
using Microsoft.AspNetCore.Components.Authorization;

namespace Library.Web.Authentication;

public interface ICurrentIdentityAccessor
{
    Task<IIdentitySubject> GetIdentityAsync();
}

public sealed class CurrentIdentityAccessor(
    AuthenticationStateProvider authenticationStateProvider)
    : ICurrentIdentityAccessor
{
    public async Task<IIdentitySubject> GetIdentityAsync()
    {
        var authenticationState = await authenticationStateProvider
            .GetAuthenticationStateAsync();
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated is not true)
        {
            throw new InvalidOperationException(
                "An authenticated identity is required.");
        }

        var subjectId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new InvalidOperationException(
                "The authenticated identity has no subject identifier.");
        }

        var schemeClaim = principal.FindFirst("forge:identity-scheme")?.Value;
        if (!Enum.TryParse<IdentityScheme>(schemeClaim, out var scheme))
        {
            throw new InvalidOperationException(
                "The authenticated identity has no valid Forge identity scheme.");
        }

        return new IdentitySubject(subjectId, scheme, principal.Identity.Name);
    }
}
