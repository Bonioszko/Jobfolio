using App.Application;
using App.Infrastructure;

namespace App.Tests;

public sealed class DemoCvGeneratorTests
{
    [Fact]
    public async Task Tailors_template_to_job_using_matching_candidate_skills()
    {
        const string snapshot = """{"displayTitle":"Senior .NET Engineer","parsedData":{"company":"Northstar & Co","description":"Build .NET services with PostgreSQL and Rust"}}""";
        const string template = "Role: {{target_role}} Company: {{target_company}} Summary: {{tailored_summary}} Skills: {{matched_skills}}";

        var result = await new DemoCvGenerator().GenerateAsync(
            new AiCvGenerationRequest(
                "system rules",
                snapshot,
                template,
                "- Skills: .NET, PostgreSQL",
                null),
            CancellationToken.None);

        Assert.Equal("DEMO_GENERATOR", result.Origin);
        Assert.Contains("Senior .NET Engineer", result.Tex);
        Assert.Contains("Northstar \\& Co", result.Tex);
        Assert.Contains("PostgreSQL", result.Tex);
        Assert.DoesNotContain("Rust", result.Tex);
        Assert.DoesNotContain("{{", result.Tex);
    }
}
