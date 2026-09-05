using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.CandidateRules;

public static class CandidateRuleEndpoints
{
    public static RouteGroupBuilder MapCandidateRuleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet(
            "/rules",
            async (
                ICurrentWorkspaceAccessor workspaceAccessor,
                ICandidateRuleQueryService candidateRules,
                CancellationToken cancellationToken) =>
                Results.Ok(await candidateRules.GetCurrentAsync(
                    workspaceAccessor.GetRequired().Key,
                    cancellationToken)));
        return group;
    }
}
