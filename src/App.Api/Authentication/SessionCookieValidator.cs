using System.Security.Claims;
using App.Application;
using App.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.Api.Authentication;

public static class SessionCookieValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var mode = context.Principal?.FindFirstValue("mode");
        if (string.Equals(mode, nameof(UserMode.Real), StringComparison.OrdinalIgnoreCase))
        {
            var settings = context.HttpContext.RequestServices
                .GetRequiredService<GoogleAuthenticationSettings>();
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var workspaceId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (settings.Enabled && settings.IsAllowed(email, workspaceId)) return;

            await RejectAsync(context);
            return;
        }

        if (!string.Equals(mode, nameof(UserMode.Demo), StringComparison.OrdinalIgnoreCase))
        {
            await RejectAsync(context);
            return;
        }

        var sessionClaim = context.Principal?.FindFirstValue("demo_session_id");
        var isActive = Guid.TryParse(sessionClaim, out var sessionId) &&
                       await context.HttpContext.RequestServices
                           .GetRequiredService<IDemoSessionService>()
                           .IsActiveAsync(sessionId, context.HttpContext.RequestAborted);
        if (isActive) return;

        await RejectAsync(context);
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
