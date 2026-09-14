using System.Text.Json;
using App.Application;
using App.Domain;

namespace App.Infrastructure;

public sealed class ManualJobPostingService(
    AppDbContext db,
    TimeProvider timeProvider) : IManualJobPostingService
{
    public async Task<CreateManualJobPostingResult> CreateAsync(
        string workspaceKey,
        CreateManualJobPosting request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation.Errors.Count > 0)
        {
            return new CreateManualJobPostingResult(
                CreateManualJobPostingOutcome.InvalidInput,
                ValidationErrors: validation.Errors);
        }

        var now = timeProvider.GetUtcNow();
        var parsedData = new Dictionary<string, object?>
        {
            ["company"] = validation.Company,
            ["location"] = validation.Location,
            ["employmentType"] = validation.EmploymentType,
            ["salary"] = validation.Salary,
            ["description"] = validation.Description,
            ["url"] = validation.Url
        };
        var searchData = new Dictionary<string, object?>
        {
            ["company"] = validation.Company,
            ["location"] = validation.Location,
            ["employmentType"] = validation.EmploymentType
        };
        var posting = new JobPosting
        {
            WorkspaceKey = workspaceKey,
            ProviderKey = "manual",
            ProviderExternalId = null,
            Title = validation.Title,
            NormalizedDataJson = JsonSerializer.Serialize(parsedData, JsonDefaults.Web),
            SearchDataJson = JsonSerializer.Serialize(searchData, JsonDefaults.Web),
            ApplicationStatus = "NEW",
            ParserKey = "manual",
            ParserVersion = 1,
            SourceReceivedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.JobPostings.Add(posting);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateManualJobPostingResult(
            CreateManualJobPostingOutcome.Created,
            new JobPostingView(
                posting.Id,
                posting.ProviderKey,
                posting.Title,
                parsedData,
                searchData,
                posting.ApplicationStatus,
                posting.ParserKey,
                posting.ParserVersion,
                posting.SourceReceivedAt,
                null));
    }

    private static ValidatedManualJobPosting Validate(CreateManualJobPosting request)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        var company = request.Company?.Trim() ?? string.Empty;
        var location = NormalizeOptional(request.Location);
        var employmentType = NormalizeOptional(request.EmploymentType);
        var salary = NormalizeOptional(request.Salary);
        var description = request.Description?.Trim() ?? string.Empty;
        var url = NormalizeOptional(request.Url);
        var errors = new Dictionary<string, string[]>();

        ValidateRequiredText(
            errors,
            "title",
            title,
            ManualJobPostingLimits.MaxTitleLength,
            "Enter a job title.");
        ValidateRequiredText(
            errors,
            "company",
            company,
            ManualJobPostingLimits.MaxCompanyLength,
            "Enter a company name.");
        ValidateRequiredText(
            errors,
            "description",
            description,
            ManualJobPostingLimits.MaxDescriptionLength,
            "Paste the job description.");
        ValidateOptionalText(errors, "location", location, ManualJobPostingLimits.MaxLocationLength);
        ValidateOptionalText(
            errors,
            "employmentType",
            employmentType,
            ManualJobPostingLimits.MaxEmploymentTypeLength);
        ValidateOptionalText(errors, "salary", salary, ManualJobPostingLimits.MaxSalaryLength);

        if (url is not null)
        {
            if (url.Length > ManualJobPostingLimits.MaxUrlLength)
            {
                errors["url"] = [$"The job URL cannot exceed {ManualJobPostingLimits.MaxUrlLength} characters."];
            }
            else if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl) ||
                     (parsedUrl.Scheme != Uri.UriSchemeHttps && parsedUrl.Scheme != Uri.UriSchemeHttp) ||
                     parsedUrl.Host.Length == 0 ||
                     parsedUrl.UserInfo.Length > 0)
            {
                errors["url"] = ["Enter an absolute HTTP or HTTPS job URL without embedded credentials."];
            }
            else
            {
                url = parsedUrl.AbsoluteUri;
            }
        }

        return new ValidatedManualJobPosting(
            title,
            company,
            location,
            employmentType,
            salary,
            description,
            url,
            errors);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void ValidateRequiredText(
        IDictionary<string, string[]> errors,
        string field,
        string value,
        int maximumLength,
        string requiredMessage)
    {
        if (value.Length == 0)
        {
            errors[field] = [requiredMessage];
        }
        else if (value.Length > maximumLength)
        {
            errors[field] = [$"The value cannot exceed {maximumLength} characters."];
        }
    }

    private static void ValidateOptionalText(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maximumLength)
    {
        if (value?.Length > maximumLength)
        {
            errors[field] = [$"The value cannot exceed {maximumLength} characters."];
        }
    }

    private sealed record ValidatedManualJobPosting(
        string Title,
        string Company,
        string? Location,
        string? EmploymentType,
        string? Salary,
        string Description,
        string? Url,
        Dictionary<string, string[]> Errors);
}
