using App.Application;
using App.Api.Configuration;

namespace App.Api.Endpoints.Configuration;

public static class DomainConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapDomainConfigurationEndpoints(
        this IEndpointRouteBuilder endpoints,
        ApplicationFeatures features)
    {
        endpoints.MapGet(
            "/api/config/domain",
            (IDomainConfigurationProvider configuration) =>
            {
                var current = configuration.Current;
                return Results.Ok(new
                {
                    current.SourceItem,
                    current.GeneratedDocument,
                    current.Fields,
                    current.Statuses,
                    Features = new
                    {
                        Cv = features.CvEnabled,
                        Demo = features.DemoEnabled
                    }
                });
            });
        return endpoints;
    }
}
