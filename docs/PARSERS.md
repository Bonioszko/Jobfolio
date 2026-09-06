# Email Parser Architecture

## Goal

Job-alert providers send different email formats.

Each provider gets deterministic parsing logic.

Do not use AI parsing in the MVP.

---

## Interface

Conceptually:

```csharp
public interface IJobEmailParser
{
    string Key { get; }
    int Version { get; }

    bool CanParse(EmailMessage email);

    Task<ParseResult> ParseAsync(
        EmailMessage email,
        CancellationToken cancellationToken);
}
```

Exact method signatures may evolve with the codebase.

---

## Registry

Use a registry/resolver:

```text
JobEmailParserRegistry
```

Resolution outcomes:

```text
MATCHED
UNSUPPORTED
AMBIGUOUS
```

If multiple parsers match, do not guess.

If none match, persist/mark unsupported ingestion.

---

## Provider separation

Example:

```text
App.Parsers/
  Sources/
    LinkedInJobEmailParser.cs
    JustJoinItJobEmailParser.cs
    NoFluffJobsJobEmailParser.cs
```

Keep provider-specific selectors and assumptions inside the provider parser.

Do not put all providers into one giant parser.

---

## Parsing strategy

Prefer:

- DOM parsing,
- explicit selectors,
- targeted regex,
- normalization helpers,
- explicit required-field validation.

Avoid parsing entire HTML emails with one giant regex.

Do not silently persist malformed required data.

---

## Output

A normalized `JobPosting` should contain clear domain fields where stable.

Typical fields:

```text
title
company
location
employmentType
salary
description
url
provider
providerExternalId
receivedAt
```

Provider-specific optional normalized metadata may be stored separately where useful.

Gmail message identifiers belong to ingestion metadata, not the job description.

---

## Parser versioning

Persist:

```text
parserKey
parserVersion
```

with each parsed posting/ingestion result.

When behavior materially changes:

1. add or update a regression fixture,
2. modify parser,
3. increment parser version where meaningful,
4. run parser tests.

Never fix production parser behavior without a regression test.

---

## Fixtures

Keep sanitized fixtures under parser tests.

Cover:

- normal message,
- missing optional data,
- malformed required fields,
- changed HTML layout,
- unrelated message,
- ambiguous parser match when relevant.

Demo fixture emails should resemble realistic provider emails but contain fake/sanitized content.

---

## Demo mode

Demo emails should pass through the same real parser registry.

Do not seed already-parsed job postings unless a specific test requires it.

The public demo should prove the parser path works.
