using System.Net;
using System.Text.RegularExpressions;
using App.Application;

namespace App.Parsers.Sources;

public abstract partial class DelimitedJobEmailParser : ISourceParser
{
    public abstract string Key { get; }
    public virtual int Version => 1;
    protected abstract string SenderMarker { get; }
    public bool CanParse(EmailMessage email) => email.Sender.Contains(SenderMarker, StringComparison.OrdinalIgnoreCase);

    public Task<ParseResult> ParseAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = Normalize(WebUtility.HtmlDecode(TagRegex().Replace(email.HtmlBody, " | ")));
        var jobId = Required(text, "Job ID");
        var title = Required(text, "Title");
        var company = Required(text, "Company");
        var description = Required(text, "Description");
        var parsed = new Dictionary<string, object?>
        {
            ["company"] = company,
            ["location"] = Optional(text, "Location"),
            ["employmentType"] = Optional(text, "Employment Type"),
            ["salary"] = Optional(text, "Salary"),
            ["description"] = description,
            ["url"] = Optional(text, "URL")
        };
        var search = new Dictionary<string, object?> { ["company"] = company, ["location"] = parsed["location"], ["employmentType"] = parsed["employmentType"] };
        return Task.FromResult(new ParseResult(Key, jobId, title, parsed, search));
    }

    private static string Required(string text, string label) => Optional(text, label) is { Length: > 0 } value
        ? value : throw new FormatException($"Required job field '{label}' was not found.");
    private static string? Optional(string text, string label)
    {
        var match = Regex.Match(text, $@"(?:^|\|)\s*{Regex.Escape(label)}\s*:\s*([^|]+)", RegexOptions.IgnoreCase);
        var value = match.Groups[1].Value.Trim();
        return value.Length == 0 || value.Equals("N/A", StringComparison.OrdinalIgnoreCase) ? null : value;
    }
    private static string Normalize(string value) => WhitespaceRegex().Replace(value, " ").Trim();
    [GeneratedRegex("<[^>]+>")] private static partial Regex TagRegex();
    [GeneratedRegex(@"\s+")] private static partial Regex WhitespaceRegex();
}
