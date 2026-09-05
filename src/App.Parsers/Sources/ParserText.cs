using App.Application;
using System.Net;
using System.Text.RegularExpressions;

namespace App.Parsers.Sources;

internal static partial class ParserText
{
    public static string Clean(string? value) =>
        WhitespaceRegex().Replace(WebUtility.HtmlDecode(value ?? string.Empty), " ").Trim();

    public static string? NullIfEmpty(string? value)
    {
        var cleaned = Clean(value);
        return cleaned.Length == 0 || cleaned.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : cleaned;
    }

    public static IReadOnlyList<string> Lines(string? value) =>
        (value ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(Clean)
            .Where(line => line.Length > 0)
            .ToArray();

    public static IReadOnlyList<string> MessageLines(EmailMessage email)
    {
        if (!string.IsNullOrWhiteSpace(email.TextBody))
        {
            return Lines(email.TextBody);
        }

        var withLineBreaks = HtmlBreakRegex().Replace(email.HtmlBody, "\n");
        return Lines(TagRegex().Replace(withLineBreaks, string.Empty));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"<(?:br\s*/?|/?(?:p|div|tr|td|li|h[1-6]|pre))\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlBreakRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
