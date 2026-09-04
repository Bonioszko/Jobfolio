using App.Application;

namespace App.Parsers;

public sealed class SourceParserRegistry(IEnumerable<ISourceParser> parsers) : ISourceParserRegistry
{
    private readonly ISourceParser[] _parsers = parsers.ToArray();
    public ParserSelection Select(EmailMessage email)
    {
        var matches = _parsers.Where(parser => parser.CanParse(email)).ToArray();
        return matches.Length switch
        {
            0 => new(ParserMatch.Unsupported, null),
            1 => new(ParserMatch.Matched, matches[0]),
            _ => new(ParserMatch.Ambiguous, null)
        };
    }
}
