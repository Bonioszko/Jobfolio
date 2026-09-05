using System.Security.Claims;
using App.Application;
using App.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace App.Api.Endpoints.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/demo", CreateDemoAsync)
            .RequireRateLimiting("demo-session");
        endpoints.MapPost("/api/auth/logout", (Delegate)LogoutAsync)
            .RequireAuthorization();
        endpoints.MapGet("/api/me", GetCurrentSession)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> CreateDemoAsync(
        HttpContext httpContext,
        IDemoSessionService demoSessions,
        CancellationToken cancellationToken)
    {
        var session = await demoSessions.CreateAsync(cancellationToken);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "demo"),
            new Claim("mode", nameof(UserMode.Demo)),
            new Claim("demo_session_id", session.Id.ToString())
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var properties = new AuthenticationProperties
        {
            AllowRefresh = false,
            ExpiresUtc = session.ExpiresAt,
            IsPersistent = true
        };
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        return Results.Ok(new { mode = "demo", expiresAt = session.ExpiresAt });
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync();
        return Results.NoContent();
    }

    private static IResult GetCurrentSession(ClaimsPrincipal user) =>
        Results.Ok(new
        {
            userId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            mode = user.FindFirstValue("mode")
        });
}
