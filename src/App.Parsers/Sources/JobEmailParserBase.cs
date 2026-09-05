using System.Net.Mail;
using App.Application;

namespace App.Parsers.Sources;

public abstract partial class JobEmailParserBase(params string[] senderDomains) : ISourceParser
{
    public abstract string Key { get; }
    public virtual int Version => 2;

    public bool CanParse(EmailMessage email)
    {
        if (!TryGetSenderDomain(email.Sender, out var senderDomain))
        {
            return false;
        }

        return senderDomains.Any(allowedDomain =>
            senderDomain.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) ||
            senderDomain.EndsWith($".{allowedDomain}", StringComparison.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<ParseResult>> ParseAsync(
        EmailMessage email,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = Parse(email);
        if (results.Count == 0)
        {
            throw new FormatException($"The '{Key}' email did not contain a recognizable job posting.");
        }

        return Task.FromResult(results);
    }

    protected abstract IReadOnlyList<ParseResult> Parse(EmailMessage email);

    protected ParseResult CreateResult(
        string externalId,
        string title,
        string company,
        Uri url,
        string? location = null,
        string? employmentType = null,
        string? salary = null,
        string? description = null)
    {
        externalId = ParserText.Clean(externalId);
        title = ParserText.Clean(title);
        company = ParserText.Clean(company);

        if (externalId.Length == 0 || title.Length == 0 || company.Length == 0)
        {
            throw new FormatException($"A '{Key}' posting is missing a required identifier, title, or company.");
        }

        if (!url.IsAbsoluteUri || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
        {
            throw new FormatException($"A '{Key}' posting contains an invalid job URL.");
        }

        location = ParserText.NullIfEmpty(location);
        employmentType = ParserText.NullIfEmpty(employmentType);
        salary = ParserText.NullIfEmpty(salary);
        description = ParserText.NullIfEmpty(description);

        var parsed = new Dictionary<string, object?>
        {
            ["company"] = company,
            ["location"] = location,
            ["employmentType"] = employmentType,
            ["salary"] = salary,
            ["description"] = description,
            ["url"] = url.AbsoluteUri
        };
        var search = new Dictionary<string, object?>
        {
            ["company"] = company,
            ["location"] = location,
            ["employmentType"] = employmentType
        };

        return new ParseResult(Key, externalId, title, parsed, search);
    }

    private static bool TryGetSenderDomain(string sender, out string domain)
    {
        try
        {
            var address = new MailAddress(sender).Address;
            var separatorIndex = address.LastIndexOf('@');
            if (separatorIndex < 0 || separatorIndex == address.Length - 1)
            {
                domain = string.Empty;
                return false;
            }

            domain = address[(separatorIndex + 1)..].TrimEnd('.');
            return domain.Length > 0;
        }
        catch (FormatException)
        {
            domain = string.Empty;
            return false;
        }
    }
}
