# Domain configuration

`config/domain.json` owns user-facing item/document labels, parsed field definitions, and workflow statuses. The API exposes it at `GET /api/config/domain`; clients must not duplicate these values.

The current domain maps a generic `SourceItem` to a job post and a generic `GeneratedDocument` to a tailored CV. Parsed job fields are company, location, employment type, salary, description, and URL. Provider job identifiers use `SourceExternalId`; the normalized title uses `DisplayTitle`.

Workflow status codes are `NEW`, `TO_APPLY`, `SKIP`, `APPLIED`, `INTERVIEWING`, and `REJECTED`. They remain configuration values rather than a domain-specific C# enum.
