namespace App.Api.Configuration;

public sealed record ApplicationFeatures(
    bool CvEnabled,
    bool CvGenerationEnabled,
    bool DemoEnabled)
{
    public static ApplicationFeatures FromConfiguration(IConfiguration configuration) => new(
        configuration.GetValue("Features:CvEnabled", true),
        configuration.GetValue("Features:CvGenerationEnabled", true),
        configuration.GetValue("Features:DemoEnabled", true));
}
