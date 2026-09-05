using App.Api.Endpoints.Artifacts;
using App.Api.Endpoints.Authentication;
using App.Api.Endpoints.CandidateRules;
using App.Api.Endpoints.Configuration;
using App.Api.Endpoints.CvCompilation;
using App.Api.Endpoints.CvGeneration;
using App.Api.Endpoints.CvTemplates;
using App.Api.Endpoints.GeneratedCvs;
using App.Api.Endpoints.JobPostings;

namespace App.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
        endpoints.MapDomainConfigurationEndpoints();
        endpoints.MapAuthenticationEndpoints();

        var authenticated = endpoints.MapGroup("/api").RequireAuthorization();
        authenticated.MapJobPostingEndpoints();
        authenticated.MapCvTemplateEndpoints();
        authenticated.MapCandidateRuleEndpoints();
        authenticated.MapCvGenerationEndpoints();
        authenticated.MapGeneratedCvEndpoints();
        authenticated.MapCvCompilationEndpoints();
        authenticated.MapArtifactEndpoints();

        return endpoints;
    }
}
