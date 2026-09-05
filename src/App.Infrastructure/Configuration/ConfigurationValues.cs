using Microsoft.Extensions.Configuration;

namespace App.Infrastructure;

internal static class ConfigurationValues
{
    public static int GetPositiveInt(
        IConfiguration configuration,
        string key,
        int defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            throw new InvalidOperationException($"Configuration value '{key}' must be a positive integer.");
        }

        return parsed;
    }
}
