using System.Security.Claims;
using App.Domain;

namespace App.Api.Authentication;

public sealed record CurrentWorkspace(string Key, UserMode Mode);

public interface ICurrentWorkspaceAccessor
{
    CurrentWorkspace GetRequired();
}

public sealed class CurrentWorkspaceAccessor(IHttpContextAccessor httpContextAccessor)
    : ICurrentWorkspaceAccessor
{
    public CurrentWorkspace GetRequired()
    {
        var principal = httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("No authenticated principal is available.");
        var mode = principal.FindFirstValue("mode");

        if (string.Equals(mode, nameof(UserMode.Demo), StringComparison.OrdinalIgnoreCase))
        {
            var sessionId = principal.FindFirstValue("demo_session_id");
            if (!Guid.TryParse(sessionId, out var parsedSessionId))
            {
                throw new UnauthorizedAccessException("The demo session claim is invalid.");
            }

            return new CurrentWorkspace($"demo:{parsedSessionId}", UserMode.Demo);
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("The user identifier claim is missing.");
        }

        return new CurrentWorkspace($"user:{userId}", UserMode.Real);
    }
}
