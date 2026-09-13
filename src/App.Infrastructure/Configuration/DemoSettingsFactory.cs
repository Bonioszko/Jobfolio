using App.Application;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure;

internal static class DemoSettingsFactory
{
    public static DemoSettings Create(IConfiguration configuration) => new(
        ConfigurationValues.GetPositiveInt(
            configuration,
            "Demo:SessionLifetimeHours",
            6),
        ConfigurationValues.GetPositiveInt(
            configuration,
            "Demo:MaxCompilationJobsPerWindow",
            2),
        ConfigurationValues.GetPositiveInt(
            configuration,
            "Demo:CompilationWindowMinutes",
            60));
}
