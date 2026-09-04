using System.Text.Json;
using System.Text.RegularExpressions;
using App.Application;

namespace App.Infrastructure;

public sealed class DemoDocumentGenerator : IAiDocumentGenerator
{
    private static readonly string[] CandidateSkills = ["C#", ".NET", "ASP.NET Core", "PostgreSQL", "SQL", "React", "TypeScript", "Docker", "Terraform", "Google Cloud", "automated testing", "CI/CD", "distributed systems", "mentoring"];

    public Task<string> GenerateAsync(string sourceSnapshotJson, string template, string rules, string? instruction, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var json = JsonDocument.Parse(sourceSnapshotJson);
        var root = json.RootElement;
        var title = root.GetProperty("displayTitle").GetString() ?? "Target role";
        var parsed = root.GetProperty("parsedData");
        var company = parsed.GetProperty("company").GetString() ?? "Target company";
        var description = parsed.GetProperty("description").GetString() ?? string.Empty;
        var matched = CandidateSkills.Where(skill => description.Contains(skill, StringComparison.OrdinalIgnoreCase)).ToArray();
        var skillText = matched.Length == 0 ? "General software engineering" : string.Join(" \\textbullet{} ", matched.Select(Escape));
        var summary = $"Software engineer with eight years of experience, applying for {Escape(title)} at {Escape(company)}. This CV emphasizes verified candidate capabilities that overlap with the posting: {skillText}.";
        var tex = template
            .Replace("{{target_role}}", Escape(title), StringComparison.Ordinal)
            .Replace("{{target_company}}", Escape(company), StringComparison.Ordinal)
            .Replace("{{tailored_summary}}", summary, StringComparison.Ordinal)
            .Replace("{{matched_skills}}", skillText, StringComparison.Ordinal);
        return Task.FromResult(tex);
    }

    private static string Escape(string value) => Regex.Replace(value, @"([#$%&_{}])", @"\$1");
}
