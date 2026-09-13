using System.Net.Mail;
using App.Application;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure;

internal sealed record GmailOAuthSettings(
    string? ClientSecretsPath,
    string TokenStoreDirectory,
    string UserKey,
    string? ClientId,
    string? ClientSecret,
    string? RefreshToken,
    string? ExpectedEmail)
{
    public bool UsesRefreshToken => RefreshToken is not null;
}

internal static class GmailConfiguration
{
    public static GmailSyncSettings CreateSyncSettings(IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("Gmail:Enabled");
        var workspaceKey = configuration["Gmail:WorkspaceKey"]?.Trim() ?? string.Empty;
        var labels = configuration.GetSection("Gmail:Labels").Get<string[]>()?
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        var settings = new GmailSyncSettings(
            enabled,
            configuration.GetValue<bool>("Gmail:RunOnce"),
            workspaceKey,
            labels,
            TimeSpan.FromSeconds(ConfigurationValues.GetPositiveInt(
                configuration, "Gmail:PollIntervalSeconds", 60)),
            ConfigurationValues.GetPositiveInt(configuration, "Gmail:MaxMessagesPerRun", 100),
            ConfigurationValues.GetPositiveInt(configuration, "Gmail:MaxBodyBytes", 2_000_000));

        if (enabled)
        {
            if (!workspaceKey.StartsWith("user:", StringComparison.Ordinal) ||
                workspaceKey.Length is <= 5 or > 256)
            {
                throw new InvalidOperationException(
                    "Gmail:WorkspaceKey must identify a real workspace and start with 'user:'.");
            }

            if (labels.Length == 0)
            {
                throw new InvalidOperationException(
                    "At least one Gmail:Labels entry is required when Gmail sync is enabled.");
            }

            if (labels.Length > 20 || labels.Any(label => label.Length > 256))
            {
                throw new InvalidOperationException("Gmail:Labels contains too many or overly long entries.");
            }

            if (settings.MaxMessagesPerRun > 500)
            {
                throw new InvalidOperationException("Gmail:MaxMessagesPerRun cannot exceed 500.");
            }

            if (settings.MaxBodyBytes > 10_000_000)
            {
                throw new InvalidOperationException("Gmail:MaxBodyBytes cannot exceed 10000000.");
            }
        }

        return settings;
    }

    public static GmailOAuthSettings CreateOAuthSettings(IConfiguration configuration)
    {
        var clientId = NullIfWhiteSpace(configuration["Gmail:OAuth:ClientId"]);
        var clientSecret = NullIfWhiteSpace(configuration["Gmail:OAuth:ClientSecret"]);
        var refreshToken = NullIfWhiteSpace(configuration["Gmail:OAuth:RefreshToken"]);
        var expectedEmail = NullIfWhiteSpace(configuration["Gmail:AccountEmail"]);
        var directCredentialCount = new[] { clientId, clientSecret, refreshToken }
            .Count(value => value is not null);
        if (directCredentialCount is > 0 and < 3)
        {
            throw new InvalidOperationException(
                "Gmail OAuth ClientId, ClientSecret, and RefreshToken must all be configured together.");
        }

        var clientSecretsPath = configuration["Gmail:OAuth:ClientSecretsPath"]?.Trim();
        if (directCredentialCount == 0 && string.IsNullOrWhiteSpace(clientSecretsPath))
        {
            throw new InvalidOperationException(
                "Gmail OAuth direct credentials or Gmail:OAuth:ClientSecretsPath are required.");
        }

        var tokenStoreDirectory = configuration["Gmail:OAuth:TokenStoreDirectory"]?.Trim();
        if (string.IsNullOrWhiteSpace(tokenStoreDirectory))
        {
            tokenStoreDirectory = Path.Combine(AppContext.BaseDirectory, ".appdata", "gmail-token");
        }

        var userKey = configuration["Gmail:OAuth:UserKey"]?.Trim();
        ValidateExpectedEmail(expectedEmail);
        return new GmailOAuthSettings(
            string.IsNullOrWhiteSpace(clientSecretsPath) ? null : Path.GetFullPath(clientSecretsPath),
            Path.GetFullPath(tokenStoreDirectory),
            string.IsNullOrWhiteSpace(userKey) ? "primary" : userKey,
            clientId,
            clientSecret,
            refreshToken,
            expectedEmail);
    }

    private static void ValidateExpectedEmail(string? email)
    {
        if (email is null)
        {
            return;
        }

        try
        {
            var address = new MailAddress(email);
            if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Gmail:AccountEmail must be a valid email address.");
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
