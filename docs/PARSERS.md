# Parsers

`ISourceParser` exposes a stable key and version. `SourceParserRegistry` returns matched, unsupported, or ambiguous and never guesses. Demo fixtures enter through this registry exactly as real Gmail messages will.

The initial provider parsers represent LinkedIn, Just Join IT, and No Fluff Jobs alert formats. Each extracts a provider job ID, title, company, optional location/employment type/salary/URL, and required description. Missing required fields fail explicitly. Fixtures are sanitized and intentionally use non-routable example URLs.

The current delimited fixture format is a deterministic first implementation, not a claim that live provider HTML will remain stable. When Gmail ingestion is added, capture sanitized regression fixtures for the real message layouts and evolve each provider parser independently.
