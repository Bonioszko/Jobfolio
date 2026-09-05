using System.Security.Claims;
using App.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.Api.Authentication;

internal static class GoogleClaimsPrincipalFactory
{
    public static ClaimsPrincipal? Create(
        ClaimsPrincipal googlePrincipal,
        GoogleAuthenticationSettings settings)
    {
        var subject = googlePrincipal.FindFirstValue("sub");
        var email = googlePrincipal.FindFirstValue("email");
        var emailVerified = googlePrincipal.FindFirstValue("email_verified");

        if (string.IsNullOrWhiteSpace(subject) ||
            !string.Equals(emailVerified, bool.TrueString, StringComparison.OrdinalIgnoreCase) ||
            !settings.TryGetWorkspaceId(email, out var workspaceId))
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, workspaceId),
            new(ClaimTypes.Email, email!),
            new("mode", nameof(UserMode.Real)),
            new("google_subject", subject)
        };
        var name = googlePrincipal.FindFirstValue("name");
        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role));
    }
}
