using System.Globalization;
using System.Text;

namespace App.Application;

public sealed record JobPostingIdentity(
    string Title,
    string Company,
    string? Location);

public static class JobPostingDeduplication
{
    public static JobPostingIdentity? CreateIdentity(ParseResult posting)
    {
        ArgumentNullException.ThrowIfNull(posting);

        return CreateIdentity(
            posting.DisplayTitle,
            ReadText(posting.ParsedData, "company"),
            ReadText(posting.ParsedData, "location"));
    }

    public static JobPostingIdentity? CreateIdentity(
        string? title,
        string? company,
        string? location)
    {
        var normalizedTitle = Normalize(title);
        var normalizedCompany = Normalize(company);
        if (normalizedTitle.Length == 0 || normalizedCompany.Length == 0)
        {
            return null;
        }

        var normalizedLocation = Normalize(location);
        return new JobPostingIdentity(
            normalizedTitle,
            normalizedCompany,
            normalizedLocation.Length == 0 ? null : normalizedLocation);
    }

    public static bool AreDuplicates(
        JobPostingIdentity first,
        JobPostingIdentity second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return string.Equals(first.Title, second.Title, StringComparison.Ordinal) &&
               string.Equals(first.Company, second.Company, StringComparison.Ordinal) &&
               (first.Location is null ||
                second.Location is null ||
                string.Equals(first.Location, second.Location, StringComparison.Ordinal));
    }

    private static string? ReadText(
        IReadOnlyDictionary<string, object?> values,
        string key) =>
        values.TryGetValue(key, out var value)
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC);
        var result = new StringBuilder(normalized.Length);
        var separatorPending = false;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                if (separatorPending && result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(char.ToLowerInvariant(character));
                separatorPending = false;
            }
            else if (result.Length > 0)
            {
                separatorPending = true;
            }
        }

        return result.ToString();
    }
}
