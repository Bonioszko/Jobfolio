using App.Application;

namespace App.Api.Endpoints.Configuration;

public static class DomainConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapDomainConfigurationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/config/domain",
            (IDomainConfigurationProvider configuration) => Results.Ok(configuration.Current));
        return endpoints;
    }
}
