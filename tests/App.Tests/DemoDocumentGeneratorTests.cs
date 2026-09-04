using App.Infrastructure;

namespace App.Tests;

public sealed class DemoDocumentGeneratorTests
{
    [Fact]
    public async Task Tailors_template_to_job_using_matching_candidate_skills()
    {
        const string snapshot = """{"displayTitle":"Senior .NET Engineer","parsedData":{"company":"Northstar & Co","description":"Build .NET services with PostgreSQL and Rust"}}""";
        const string template = "Role: {{target_role}} Company: {{target_company}} Summary: {{tailored_summary}} Skills: {{matched_skills}}";

        var tex = await new DemoDocumentGenerator().GenerateAsync(snapshot, template, "candidate rules", null, CancellationToken.None);

        Assert.Contains("Senior .NET Engineer", tex);
        Assert.Contains("Northstar \\& Co", tex);
        Assert.Contains("PostgreSQL", tex);
        Assert.DoesNotContain("Rust", tex);
        Assert.DoesNotContain("{{", tex);
    }
}
