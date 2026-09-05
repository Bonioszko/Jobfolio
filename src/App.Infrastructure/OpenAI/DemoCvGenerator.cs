using System.Text.Json;
using System.Text.RegularExpressions;
using App.Application;

namespace App.Infrastructure;

public sealed class DemoCvGenerator : IAiCvGenerator
{
    public Task<AiCvGenerationResult> GenerateAsync(
        AiCvGenerationRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var json = JsonDocument.Parse(request.JobPostingSnapshotJson);
        var root = json.RootElement;
        var title = root.GetProperty("displayTitle").GetString() ?? "Target role";
        var parsedData = root.GetProperty("parsedData");
        var company = parsedData.GetProperty("company").GetString() ?? "Target company";
        var description = parsedData.GetProperty("description").GetString() ?? string.Empty;
        var candidateSkills = ExtractCandidateSkills(request.CandidateRulesMarkdown);
        var matchedSkills = candidateSkills
            .Where(skill => description.Contains(skill, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var skillText = matchedSkills.Length == 0
            ? "General software engineering"
            : string.Join(" \\textbullet{} ", matchedSkills.Select(Escape));
        var summary =
            $"Software engineer with eight years of experience, applying for {Escape(title)} at {Escape(company)}. " +
            $"This CV emphasizes verified candidate capabilities that overlap with the posting: {skillText}.";
        var tex = request.CvTemplateTex
            .Replace("{{target_role}}", Escape(title), StringComparison.Ordinal)
            .Replace("{{target_company}}", Escape(company), StringComparison.Ordinal)
            .Replace("{{tailored_summary}}", summary, StringComparison.Ordinal)
            .Replace("{{matched_skills}}", skillText, StringComparison.Ordinal);

        return Task.FromResult(new AiCvGenerationResult(tex, "DEMO_GENERATOR"));
    }

    private static string Escape(string value) =>
        Regex.Replace(value, @"([#$%&_{}])", @"\$1");

    private static string[] ExtractCandidateSkills(string candidateRules)
    {
        var skillsLine = candidateRules
            .Split('\n', StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => line.StartsWith("- Skills:", StringComparison.OrdinalIgnoreCase));

        return skillsLine is null
            ? []
            : skillsLine["- Skills:".Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(skill => skill.Length is > 0 and <= 100)
                .Take(50)
                .ToArray();
    }
}
