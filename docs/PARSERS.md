# Parsers

`ISourceParser` exposes a stable key and version. `SourceParserRegistry` returns matched, unsupported, or ambiguous and never guesses. Demo fixtures enter through this registry exactly as real Gmail messages will.

Price normalization converts Polish-formatted values such as `1 599,99 zł` to decimal `1599.99` plus currency `PLN`. Required fields fail explicitly.
