using System.Security.Claims;
using App.Application;
using App.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.Api.Authentication;

public static class DemoCookieValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var mode = context.Principal?.FindFirstValue("mode");
        if (!string.Equals(mode, nameof(UserMode.Demo), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sessionClaim = context.Principal?.FindFirstValue("demo_session_id");
        var isActive = Guid.TryParse(sessionClaim, out var sessionId) &&
                       await context.HttpContext.RequestServices
                           .GetRequiredService<IDemoSessionService>()
                           .IsActiveAsync(sessionId, context.HttpContext.RequestAborted);
        if (isActive) return;

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
