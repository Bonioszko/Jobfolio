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
            ConfigurationValues.GetPositiveInt(
                configuration,
                "CvWorkflow:MaxCustomJobDescriptionCharacters",
                20_000),
            configuration["CvWorkflow:RealUserGenerator"] ?? CvGeneratorNames.LocalCodex);

        if (string.IsNullOrWhiteSpace(settings.RealUserGenerator))
        {
            throw new InvalidOperationException("The real-user CV generator must be configured.");
        }

        return settings;
    }
}
