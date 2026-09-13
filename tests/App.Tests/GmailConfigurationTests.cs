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
        Assert.False(settings.RunOnce);
        Assert.Empty(settings.Labels);
    }

    [Fact]
    public void Enabled_sync_requires_a_real_workspace_and_at_least_one_label()
    {
        var missingWorkspace = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Gmail:Enabled"] = "true",
            ["Gmail:RunOnce"] = "true",
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
            ["Gmail:RunOnce"] = "true",
            ["Gmail:Labels:0"] = "Job alerts",
            ["Gmail:Labels:1"] = "job alerts",
            ["Gmail:PollIntervalSeconds"] = "90",
            ["Gmail:MaxMessagesPerRun"] = "25"
        });

        var settings = GmailConfiguration.CreateSyncSettings(configuration);

        Assert.True(settings.Enabled);
        Assert.True(settings.RunOnce);
        Assert.Equal("Job alerts", Assert.Single(settings.Labels));
        Assert.Equal(TimeSpan.FromSeconds(90), settings.PollInterval);
        Assert.Equal(25, settings.MaxMessagesPerRun);
    }

    [Fact]
    public void Cloud_oauth_requires_a_complete_credential_set()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Gmail:OAuth:ClientId"] = "client.apps.googleusercontent.com",
            ["Gmail:OAuth:ClientSecret"] = "secret"
        });

        Assert.Throws<InvalidOperationException>(() =>
            GmailConfiguration.CreateOAuthSettings(configuration));
    }

    [Fact]
    public void Cloud_oauth_accepts_client_credentials_and_refresh_token()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Gmail:AccountEmail"] = "OWNER@example.com",
            ["Gmail:OAuth:ClientId"] = "client.apps.googleusercontent.com",
            ["Gmail:OAuth:ClientSecret"] = "secret",
            ["Gmail:OAuth:RefreshToken"] = "refresh-token"
        });

        var settings = GmailConfiguration.CreateOAuthSettings(configuration);

        Assert.True(settings.UsesRefreshToken);
        Assert.Null(settings.ClientSecretsPath);
        Assert.Equal("refresh-token", settings.RefreshToken);
        Assert.Equal("OWNER@example.com", settings.ExpectedEmail);
    }

    [Fact]
    public void Gmail_account_email_must_be_valid_when_configured()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Gmail:AccountEmail"] = "not-an-email",
            ["Gmail:OAuth:ClientId"] = "client.apps.googleusercontent.com",
            ["Gmail:OAuth:ClientSecret"] = "secret",
            ["Gmail:OAuth:RefreshToken"] = "refresh-token"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            GmailConfiguration.CreateOAuthSettings(configuration));

        Assert.Equal("Gmail:AccountEmail must be a valid email address.", exception.Message);
    }

    [Fact]
    public void Authorized_Gmail_account_match_is_case_insensitive()
    {
        GoogleGmailEmailProvider.EnsureExpectedAccount(
            "owner@example.com",
            "OWNER@example.com");
    }

    [Theory]
    [InlineData("other@example.com")]
    [InlineData(null)]
    public void Different_or_missing_authorized_Gmail_account_is_rejected(string? authorizedEmail)
    {
        const string expectedEmail = "owner@example.com";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            GoogleGmailEmailProvider.EnsureExpectedAccount(expectedEmail, authorizedEmail));

        Assert.Equal(
            "The authorized Gmail account does not match Gmail:AccountEmail.",
            exception.Message);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
