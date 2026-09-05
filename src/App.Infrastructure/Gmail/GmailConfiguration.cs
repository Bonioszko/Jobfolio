using App.Application;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure;

internal sealed record GmailOAuthSettings(
    string ClientSecretsPath,
    string TokenStoreDirectory,
    string UserKey);

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
        var clientSecretsPath = configuration["Gmail:OAuth:ClientSecretsPath"]?.Trim();
        if (string.IsNullOrWhiteSpace(clientSecretsPath))
        {
            throw new InvalidOperationException("Gmail:OAuth:ClientSecretsPath is required.");
        }

        var tokenStoreDirectory = configuration["Gmail:OAuth:TokenStoreDirectory"]?.Trim();
        if (string.IsNullOrWhiteSpace(tokenStoreDirectory))
        {
            tokenStoreDirectory = Path.Combine(AppContext.BaseDirectory, ".appdata", "gmail-token");
        }

        var userKey = configuration["Gmail:OAuth:UserKey"]?.Trim();
        return new GmailOAuthSettings(
            Path.GetFullPath(clientSecretsPath),
            Path.GetFullPath(tokenStoreDirectory),
            string.IsNullOrWhiteSpace(userKey) ? "primary" : userKey);
    }
}
