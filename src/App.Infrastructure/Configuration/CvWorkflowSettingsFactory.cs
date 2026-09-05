using App.Application;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure;

internal static class CvWorkflowSettingsFactory
{
    public static CvWorkflowSettings Create(IConfiguration configuration)
    {
        var settings = new CvWorkflowSettings(
            ConfigurationValues.GetPositiveInt(
                configuration,
                "CvWorkflow:MaxGenerationJobsPerWorkspace",
                10),
            ConfigurationValues.GetPositiveInt(
                configuration,
                "CvWorkflow:MaxCompilationJobsPerWorkspace",
                10),
            ConfigurationValues.GetPositiveInt(
                configuration,
                "CvWorkflow:MaxUserInstructionCharacters",
                5_000),
            configuration["CvWorkflow:GeneratorModel"] ?? "demo-deterministic-v1");

        if (string.IsNullOrWhiteSpace(settings.GeneratorModel))
        {
            throw new InvalidOperationException("The CV generator model must be configured.");
        }

        return settings;
    }
}
