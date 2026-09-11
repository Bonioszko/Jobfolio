using App.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace App.Api.Tests;

public sealed class ApplicationFeaturesTests
{
    [Fact]
    public void Features_are_enabled_by_default_for_local_development()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var features = ApplicationFeatures.FromConfiguration(configuration);

        Assert.True(features.CvEnabled);
        Assert.True(features.DemoEnabled);
    }

    [Fact]
    public void Features_can_be_disabled_for_the_low_cost_cloud_runtime()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Features:CvEnabled"] = "false",
                ["Features:DemoEnabled"] = "false"
            }).Build();

        var features = ApplicationFeatures.FromConfiguration(configuration);

        Assert.False(features.CvEnabled);
        Assert.False(features.DemoEnabled);
    }
}
