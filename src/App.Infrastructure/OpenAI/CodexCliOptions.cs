using Microsoft.Extensions.Configuration;

namespace App.Infrastructure;

internal sealed record CodexCliOptions(
    string ExecutablePath,
    string? Model,
    TimeSpan Timeout,
    int MaxOutputCharacters)
{
    public static CodexCliOptions FromConfiguration(IConfiguration configuration)
    {
        var executablePath = configuration["CodexCli:ExecutablePath"] ?? "codex";
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException(
                "The Codex CLI executable path must be configured.");
        }

        var model = configuration["CodexCli:Model"];
        return new CodexCliOptions(
            executablePath.Trim(),
            string.IsNullOrWhiteSpace(model) ? null : model.Trim(),
            TimeSpan.FromSeconds(ConfigurationValues.GetPositiveInt(
                configuration,
                "CodexCli:TimeoutSeconds",
                300)),
            ConfigurationValues.GetPositiveInt(
                configuration,
                "CodexCli:MaxOutputCharacters",
                250_000));
    }
}
