using App.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace App.Tests;

public sealed class GmailConfigurationTests
{
    [Fact]
    public void Disabled_sync_does_not_require_labels_or_oauth_configuration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var settings = GmailConfiguration.CreateSyncSettings(configuration);

        Assert.False(settings.Enabled);
        Assert.Empty(settings.Labels);
    }

    [Fact]
    public void Enabled_sync_requires_a_real_workspace_and_at_least_one_label()
    {
        var missingWorkspace = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Gmail:Enabled"] = "true",
            ["Gmail:Labels:0"] = "Job alerts"
        });
        var missingLabels = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Gmail:Enabled"] = "true",
            ["Gmail:WorkspaceKey"] = "user:owner"
        });

        Assert.Throws<InvalidOperationException>(() =>
            GmailConfiguration.CreateSyncSettings(missingWorkspace));
        Assert.Throws<InvalidOperationException>(() =>
            GmailConfiguration.CreateSyncSettings(missingLabels));
    }

    [Fact]
    public void Enabled_sync_reads_and_deduplicates_the_label_allowlist()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Gmail:Enabled"] = "true",
            ["Gmail:WorkspaceKey"] = "user:owner",
            ["Gmail:Labels:0"] = "Job alerts",
            ["Gmail:Labels:1"] = "job alerts",
            ["Gmail:PollIntervalSeconds"] = "90",
            ["Gmail:MaxMessagesPerRun"] = "25"
        });

        var settings = GmailConfiguration.CreateSyncSettings(configuration);

        Assert.True(settings.Enabled);
        Assert.Equal("Job alerts", Assert.Single(settings.Labels));
        Assert.Equal(TimeSpan.FromSeconds(90), settings.PollInterval);
        Assert.Equal(25, settings.MaxMessagesPerRun);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
