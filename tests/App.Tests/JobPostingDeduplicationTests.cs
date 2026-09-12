using App.Application;

namespace App.Tests;

public sealed class JobPostingDeduplicationTests
{
    [Fact]
    public void AreDuplicates_normalizes_case_whitespace_and_punctuation()
    {
        var first = JobPostingDeduplication.CreateIdentity(
            "Senior .NET Engineer",
            "Acme S.A.",
            "Warsaw, Poland");
        var second = JobPostingDeduplication.CreateIdentity(
            " senior   .net engineer ",
            "ACME S.A.",
            "Warsaw / Poland");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(JobPostingDeduplication.AreDuplicates(first, second));
    }

    [Fact]
    public void AreDuplicates_matches_when_one_source_omits_location()
    {
        var withoutLocation = JobPostingDeduplication.CreateIdentity(
            "Platform Engineer",
            "Northstar",
            null);
        var withLocation = JobPostingDeduplication.CreateIdentity(
            "Platform Engineer",
            "Northstar",
            "Remote");

        Assert.NotNull(withoutLocation);
        Assert.NotNull(withLocation);
        Assert.True(JobPostingDeduplication.AreDuplicates(withoutLocation, withLocation));
    }

    [Fact]
    public void AreDuplicates_rejects_different_known_locations()
    {
        var warsaw = JobPostingDeduplication.CreateIdentity(
            "Software Engineer",
            "Contoso",
            "Warsaw");
        var krakow = JobPostingDeduplication.CreateIdentity(
            "Software Engineer",
            "Contoso",
            "Kraków");

        Assert.NotNull(warsaw);
        Assert.NotNull(krakow);
        Assert.False(JobPostingDeduplication.AreDuplicates(warsaw, krakow));
    }

    [Theory]
    [InlineData("Platform Engineer", "Acme", "Software Engineer", "Acme")]
    [InlineData("Platform Engineer", "Acme", "Platform Engineer", "Contoso")]
    public void AreDuplicates_requires_both_title_and_company_to_match(
        string firstTitle,
        string firstCompany,
        string secondTitle,
        string secondCompany)
    {
        var first = JobPostingDeduplication.CreateIdentity(
            firstTitle,
            firstCompany,
            "Remote");
        var second = JobPostingDeduplication.CreateIdentity(
            secondTitle,
            secondCompany,
            "Remote");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.False(JobPostingDeduplication.AreDuplicates(first, second));
    }

    [Theory]
    [InlineData(null, "Acme")]
    [InlineData("Developer", null)]
    [InlineData("   ", "Acme")]
    [InlineData("Developer", "---")]
    public void CreateIdentity_requires_meaningful_title_and_company(
        string? title,
        string? company)
    {
        Assert.Null(JobPostingDeduplication.CreateIdentity(title, company, "Remote"));
    }
}
