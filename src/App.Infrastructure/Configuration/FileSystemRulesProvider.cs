using System.Security.Cryptography;
using System.Text;
using App.Application;

namespace App.Infrastructure;

public sealed class FileSystemRulesProvider : ISystemRulesProvider
{
    public FileSystemRulesProvider()
    {
        var generationRules = File.ReadAllText(
            RepositoryFileLocator.FindFile("system-rules", "generation.md"));
        var latexSafetyRules = File.ReadAllText(
            RepositoryFileLocator.FindFile("system-rules", "latex-safety.md"));
        GenerationRulesMarkdown = $"{generationRules.TrimEnd()}\n\n{latexSafetyRules.Trim()}";
        GenerationRulesHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(GenerationRulesMarkdown)))
            .ToLowerInvariant();
    }

    public string GenerationRulesMarkdown { get; }
    public string GenerationRulesHash { get; }
}
