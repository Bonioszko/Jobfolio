# Database Design

## Technology

Use PostgreSQL 17 with Entity Framework Core.

EF Core mappings, migrations, and PostgreSQL-specific queries belong in `App.Infrastructure`.

---

## Core tables

Expected tables include:

```text
demo_sessions
job_postings
application_status_history
interview_notes
cv_templates
cv_template_versions
candidate_rule_documents
candidate_rule_versions
cv_generation_jobs
generated_cvs
generated_cv_versions
cv_compile_jobs
pdf_artifacts
gmail_accounts
email_messages
audit_events
```

Exact names may follow established repository naming conventions.

---

## Workspace ownership

Every user-owned row must either:

- contain a workspace/user key,
- or be unambiguously joined to one.

Queries must scope by workspace before resource ID.

Bad:

```sql
SELECT *
FROM cv_templates
WHERE id = @id;
```

Preferred conceptually:

```sql
SELECT *
FROM cv_templates
WHERE workspace_id = @workspaceId
  AND id = @id;
```

This rule is important for both real users and isolated demo sessions.

---

## Job postings

A job posting should store normalized domain data directly enough to support the product.

Prefer clear fields for stable concepts such as:

- title,
- company,
- location,
- employment type,
- salary,
- description,
- URL,
- provider,
- provider external ID,
- received time,
- first application time,
- parser key/version.

JSONB is acceptable for optional provider-specific normalized metadata that is not important enough for dedicated schema.

Do not make the entire core domain one unstructured JSON blob merely for genericity.

---

## Workflow status

Current application status lives with the job posting/application workflow record.

Every status change also creates a status-history record.

Status update + history insert must be atomic.

`AppliedAt` records the first transition into `APPLIED`. Later workflow changes
preserve it so application dashboards can sort and display the actual submission
date rather than the email import date.

## Interview notes

Each interview stage is a separate workspace-owned row linked to a job posting.
Store the stage label, interview date, notes, and creation timestamp. Multiple
stages may belong to the same posting, and every query must scope by workspace
and job-posting ID before returning notes.

---

## Immutable versions

Never overwrite versioned content.

Applicable content includes:

- CV templates,
- candidate rules,
- generated CV TeX.

Editing creates a new version and updates the current-version pointer where needed.

Use a transaction when current-version pointer consistency matters.

---

## Async jobs

Generation and compilation jobs store:

- ID,
- workspace owner,
- referenced immutable inputs,
- status,
- attempt count,
- lease expiry,
- timestamps,
- output reference,
- safe error information.

Expected status values:

```text
QUEUED
RUNNING
SUCCEEDED
FAILED
```

Compilation may also support:

```text
TIMED_OUT
```

---

## Job leasing

Do not claim work using only `QUEUED -> RUNNING`.

A worker may crash after the transition.

Track:

```text
lease_until
attempt_count
```

A worker can reclaim a running job only when its lease expired.

Claim/reclaim transactionally.

A succeeded job must never run again.

---

## Transactions

Use transactions where multi-row or concurrency consistency matters.

Examples:

- status update + status history,
- async job claim/reclaim,
- template version insert + current version update,
- candidate rule version insert + current version update,
- generated CV version insert + current version update.

Do not wrap every independent immutable insert in an explicit transaction without reason.

---

## Dashboard query design

Expected filters:

- workspace,
- application status,
- source/provider,
- received time,
- possibly company/title text.

Use deliberate indexes.

Prefer keyset/cursor pagination for a growing job list.

A stable cursor tuple may use:

```text
(received_at, id)
```

Avoid offset pagination for the primary growing feed.

---

## Deduplication

Email ingestion must tolerate repeated synchronization.

Use stable provider/Gmail identifiers and database uniqueness constraints where appropriate.

Do not rely only on in-memory checks.

---

## Migrations

All schema changes use EF Core migrations.

Do not edit production schema manually as the normal workflow.

Migration code stays with Infrastructure.

---

## Database testing

Integration tests should cover:

- workspace isolation,
- unique constraints,
- status-history transactions,
- version pointer consistency,
- job leasing,
- expired lease reclaim,
- duplicate ingestion,
- cursor ordering.
