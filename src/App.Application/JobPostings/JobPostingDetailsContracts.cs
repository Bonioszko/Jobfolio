namespace App.Application;

/// <summary>
/// Identifies the provider page that can enrich a posting imported from an alert email.
/// Alert parsers are responsible for producing these stable identifiers and canonical URLs.
/// </summary>
public sealed record JobPostingDetailsReference(
    string SourceKey,
    string SourceExternalId,
    Uri Url);

public sealed record JobPostingDetails(
    string Description,
    string? Location,
    string? EmploymentType,
    string? Salary);

/// <summary>
/// Boundary for the future, provider-specific job-page fetching step.
/// Implementations intentionally do not belong to the email parser layer.
/// </summary>
public interface IJobPostingDetailsFetcher
{
    string SourceKey { get; }

    Task<JobPostingDetails> FetchAsync(
        JobPostingDetailsReference reference,
        CancellationToken cancellationToken);
}

public interface IJobPostingDetailsEnricher
{
    Task<ParseResult> EnrichAsync(
        ParseResult posting,
        CancellationToken cancellationToken);
}
