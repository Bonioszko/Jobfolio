namespace App.Application;

/// <summary>Central size limits for manually entered job-posting fields.</summary>
public static class ManualJobPostingLimits
{
    public const int MaxTitleLength = 200;
    public const int MaxCompanyLength = 200;
    public const int MaxLocationLength = 300;
    public const int MaxEmploymentTypeLength = 100;
    public const int MaxSalaryLength = 100;
    public const int MaxDescriptionLength = 50_000;
    public const int MaxUrlLength = 2_048;
}

/// <summary>Contains the user-entered details for a manually added job posting.</summary>
public sealed record CreateManualJobPosting(
    string Title,
    string Company,
    string? Location,
    string? EmploymentType,
    string? Salary,
    string Description,
    string? Url);

public enum CreateManualJobPostingOutcome
{
    Created,
    InvalidInput
}

public sealed record CreateManualJobPostingResult(
    CreateManualJobPostingOutcome Outcome,
    JobPostingView? Posting = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public interface IManualJobPostingService
{
    Task<CreateManualJobPostingResult> CreateAsync(
        string workspaceKey,
        CreateManualJobPosting request,
        CancellationToken cancellationToken);
}
