namespace App.Api.Configuration;

public sealed record ApplicationFeatures(bool CvEnabled, bool DemoEnabled)
{
    public static ApplicationFeatures FromConfiguration(IConfiguration configuration) => new(
        configuration.GetValue("Features:CvEnabled", true),
        configuration.GetValue("Features:DemoEnabled", true));
}
