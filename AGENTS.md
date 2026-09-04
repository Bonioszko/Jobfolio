# AGENTS.md

## 1. Purpose

This file defines the engineering rules for AI coding agents working in this repository.

Treat these instructions as project-level constraints.

The application is a small, secure, locally hosted first version designed for a later cloud migration. It:

1. receives job-alert emails,
2. parses them with provider-specific deterministic parsers,
3. stores normalized job posts as generic `SourceItem` objects,
4. lets users manage application workflow statuses,
5. combines a selected job, verified candidate rules, system rules, and a base CV TeX template,
6. generates a tailored, versioned TeX CV asynchronously,
7. compiles the exact selected TeX version asynchronously,
8. stores the resulting PDF privately,
9. supports two real users and isolated public demo sessions.

The active domain is job-post ingestion and tailored CV generation, while the reusable architecture MUST remain domain-independent.

Job-alert emails from providers such as LinkedIn, Just Join IT, and No Fluff Jobs are parsed into normalized source items. Users select a base CV template and tailor a versioned CV to a specific posting.

---

# 2. Primary architectural goal

Keep domain-specific behavior at the edges.

The reusable core flow is:

```text
JOB-ALERT EMAIL
  ↓
PROVIDER-SPECIFIC PARSER
  ↓
NORMALIZED SOURCE ITEM (JOB POST)
  ↓
APPLICATION WORKFLOW
  ↓
IMMUTABLE CANDIDATE RULES + BASE CV TEMPLATE + JOB SNAPSHOT
  ↓
GENERATION JOB
  ↓
AI QUEUE
  ↓
AI WORKER
  ↓
VERSIONED TAILORED CV TEX
  ↓
COMPILE JOB
  ↓
COMPILATION QUEUE
  ↓
ISOLATED COMPILER
  ↓
PRIVATE PDF
```

The temporary domain may change.

The pipeline above should not.

---

# 3. Technology decisions

Use the following stack unless there is a strong documented reason to change it.

## Backend

- .NET 10
- ASP.NET Core
- C#

## Frontend

- React
- TypeScript
- Vite
- Tailwind CSS
- shadcn/ui
- Monaco Editor
- PDF.js

## Local infrastructure

- PostgreSQL 17 in Docker
- Entity Framework Core in `App.Infrastructure`
- private local artifact storage
- database-backed local job polling

## Future cloud

- Google Cloud Run
- managed PostgreSQL, with the exact service selected during cloud migration
- Google Cloud Storage
- Google Cloud Tasks
- Google Cloud Scheduler
- Google Secret Manager
- Google Cloud KMS
- Google Artifact Registry

## External services

- Gmail API
- OpenAI Responses API
- Tectonic

## Infrastructure

- Terraform

## Tests

- xUnit
- Playwright
- PostgreSQL integration tests where useful

PostgreSQL and Entity Framework Core are the approved persistence stack for the first version. Do not introduce MongoDB, Redis, RabbitMQ, Kafka, Kubernetes, Elasticsearch, a vector database, RAG, or another infrastructure component without a concrete requirement.

Prefer the simplest secure design that satisfies the requirement.

---

# 4. Solution architecture

The expected solution structure is:

```text
src/
  App.Api/
  App.Domain/
  App.Application/
  App.Infrastructure/
  App.Parsers/
  App.AiWorker/
  App.CompilerWorker/
  App.GmailSync/
  App.DemoCleanup/

frontend/

config/
  domain.json

system-rules/
  generation.md
  latex-safety.md

example-templates/
  example-1.tex
  example-2.tex
  example-3.tex

demo-fixtures/
  emails/
  generated-documents/

tests/
  App.Domain.Tests/
  App.Application.Tests/
  App.Infrastructure.Tests/
  App.Parsers.Tests/
  App.IntegrationTests/
  App.E2E/

infrastructure/
  terraform/

docs/
  ARCHITECTURE.md
  SECURITY.md
  DOMAIN.md
  PARSERS.md
  QUEUES.md
  DEMO_MODE.md
```

Do not collapse the solution into one project.

Do not create unnecessary projects either.

---

# 5. Dependency direction

Keep dependencies approximately:

```text
App.Api
    ↓
App.Application
    ↓
App.Domain
```

`App.Infrastructure` implements interfaces defined by the application/domain layer.

Example abstractions:

```text
ISourceItemRepository
IDocumentTemplateRepository
IUserRuleRepository
IGenerationJobRepository
IGeneratedDocumentRepository
ICompileJobRepository
IPdfArtifactRepository
IGmailAccountRepository

IGenerationQueue
ICompilationQueue

IAiDocumentGenerator
IArtifactStorage
IEmailProvider
ITokenCipher
```

Infrastructure implementations may include:

```text
PostgresSourceItemRepository
GoogleCloudTasksGenerationQueue
GoogleCloudTasksCompilationQueue
OpenAiDocumentGenerator
GoogleCloudStorageArtifactStorage
GmailEmailProvider
KmsTokenCipher
```

Do not reference `DbContext` throughout controllers or application services.

Do not reference Google Cloud Tasks directly from controllers.

Do not make business logic depend on OpenAI SDK types.

---

# 6. Domain independence

The core application must use generic concepts.

Preferred names:

```text
SourceItem
SourceParser
SourceParserRegistry
DocumentTemplate
TemplateVersion
UserRuleDocument
RuleVersion
GenerationJob
GeneratedDocument
GeneratedDocumentVersion
CompileJob
PdfArtifact
WorkflowStatusCode
```

Avoid coupling reusable core infrastructure to presentation-specific names such as:

```text
JobPostRecord
TailoredCvJob
LinkedInApplication
FireAlert
NewsArticle
```

Domain-specific names are allowed only in:

- `config/domain.json`
- source-specific parsers
- parser fixtures
- demo fixtures
- system rules
- example templates
- presentation copy

---

# 7. Domain configuration

Domain behavior and UI terminology should come from:

```text
config/domain.json
```

This should define at least:

- source item singular/plural label,
- generated document singular/plural label,
- parsed field definitions,
- dashboard-visible fields,
- workflow statuses.

The frontend must consume the backend-provided domain configuration.

Do not duplicate status lists or parsed-field definitions in React.

Do not hard-code `"Company"`, `"Location"`, `"Job"`, or `"To apply"` throughout the UI.

---

# 8. User model

There are only:

```text
2 real users
+
public isolated demo sessions
```

Do not build:

- registration,
- user CRUD,
- invitations,
- organizations,
- teams,
- role management,
- admin user management.

Real users authenticate with Google and are checked against a configured allowlist.

Keep real email addresses outside source code where practical.

Use environment/configuration for the allowlist.

---

# 9. Current user abstraction

Use a central user/workspace model.

Example:

```csharp
public enum UserMode
{
    Real,
    Demo
}

public sealed record CurrentUser(
    string UserId,
    UserMode Mode,
    string? Email,
    string? DemoSessionId);
```

Do not scatter checks such as:

```csharp
if (email == "demo@example.com")
```

Use authorization policies and centralized workspace abstractions.

---

# 10. Real-user authentication

Real-user flow:

```text
Google OpenID Connect
    ↓
validated Google identity
    ↓
email exists in allowlist
    ↓
application cookie session
```

Use secure server-side cookie authentication.

Production cookies should be:

```text
Secure
HttpOnly
SameSite=Lax
```

Do not store authentication tokens in:

- `localStorage`
- `sessionStorage`

Do not expose Gmail refresh tokens to the browser.

---

# 11. Demo authentication

Public users enter through:

```text
Try Demo
```

The application creates a random `demoSessionId`.

No shared public username/password.

No Google account required.

Each visitor must receive an isolated demo workspace.

Never use a single mutable global demo workspace.

---

# 12. Demo workspace isolation

Conceptually:

```text
visitor A → demo session A
visitor B → demo session B
visitor C → demo session C
```

Visitors must never see or modify one another's data.

Demo data must also be isolated from real-user data.

Storage may use a structure such as:

```text
demoSessions/{sessionId}/...
```

or another implementation with equivalent isolation.

Every repository operation must know the active workspace.

---

# 13. Demo permissions

Server-side authorization must enforce demo restrictions.

Recommended behavior:

| Capability | Real | Demo |
|---|---:|---:|
| Connect Gmail | yes | no |
| Real Gmail synchronization | yes | no |
| Change workflow status | yes | yes |
| Edit templates | yes | yes |
| Edit rules | yes | yes |
| Real OpenAI generation | yes | no |
| Demo generation provider | no | yes |
| Compile with real Tectonic worker | yes | yes |
| Preview PDF | yes | yes |
| Download PDF | yes | yes |

Do not rely only on hiding frontend buttons.

---

# 14. Demo session cleanup

Demo sessions are temporary.

Use an expiry such as:

```text
6 hours
```

Store:

```text
createdAt
expiresAt
```

`App.DemoCleanup` must periodically remove:

- expired demo PostgreSQL rows,
- demo PDFs,
- demo compiler logs,
- other demo artifacts.

Cleanup must be safe to run repeatedly.

---

# 15. Email ingestion

Real users receive email through Gmail.

Demo users receive fake email fixtures.

Both paths must feed the same parser infrastructure.

Real:

```text
Gmail
  ↓
Gmail sync
  ↓
SourceParserRegistry
```

Demo:

```text
demo fixture
  ↓
SourceParserRegistry
```

Do not directly seed parsed `SourceItem` objects for demo mode unless a test specifically requires it.

The public demo should exercise real parser code.

---

# 16. Gmail synchronization

Use:

```text
Cloud Scheduler
  ↓
App.GmailSync Cloud Run Job
```

Initial cadence:

```text
10–15 minutes
```

Initial sync should be limited to a recent range.

Subsequent synchronization should use Gmail incremental history.

Persist the Gmail history checkpoint.

If the checkpoint becomes invalid or too old, perform a limited resynchronization and establish a new checkpoint.

One Gmail account failure must not stop all accounts.

One malformed email must not stop processing remaining messages.

---

# 17. Gmail token handling

Application login and Gmail mailbox authorization are separate.

Gmail refresh tokens:

- never go to React,
- never go into logs,
- never go to AI,
- never go to the compiler,
- must be encrypted before PostgreSQL persistence.

Use Cloud KMS.

Only services that need to decrypt them should have decrypt permission.

---

# 18. Parser design

Create:

```csharp
public interface ISourceParser
{
    string Key { get; }
    int Version { get; }

    bool CanParse(EmailMessage email);

    Task<ParseResult> ParseAsync(
        EmailMessage email,
        CancellationToken cancellationToken);
}
```

Use a registry:

```text
SourceParserRegistry
```

Parser resolution can produce:

```text
MATCHED
UNSUPPORTED
AMBIGUOUS
```

If multiple parsers match, do not guess.

If no parser matches, mark unsupported.

Do not use AI to select or execute parsers in the MVP.

---

# 19. Parser implementation rules

Keep each source separate.

Example:

```text
App.Parsers/Sources/
  LinkedInJobParser.cs
  JustJoinItJobParser.cs
  NoFluffJobsParser.cs
```

Prefer:

- DOM parsing,
- explicit selectors,
- small targeted regular expressions,
- normalization helpers,
- explicit validation.

Do not parse large HTML documents with one giant regex.

Do not silently accept malformed required data.

---

# 20. Parser versioning

Every parser has:

```text
Key
Version
```

Persist these with parsed items.

When parser behavior changes:

1. add/update regression fixture,
2. modify parser,
3. increment parser version when behavior meaningfully changes,
4. run parser tests.

Never fix production parser behavior without a regression test.

---

# 21. Parser fixtures

Keep sanitized fixtures:

```text
tests/App.Parsers.Tests/Fixtures/
```

Cover:

- normal message,
- missing optional data,
- malformed fields,
- changed HTML layout,
- unrelated message,
- ambiguous patterns when relevant.

Demo fixture emails may reuse realistic parser fixture formats, but should remain clearly fake/sanitized.

---

# 22. SourceItem design

Use a domain-independent document.

Conceptually:

```csharp
public sealed class SourceItem
{
    public string Id { get; init; }
    public string OwnerId { get; init; }

    public string SourceKey { get; init; }
    public string? SourceExternalId { get; init; }

    public string DisplayTitle { get; init; }

    public Dictionary<string, object?> ParsedData { get; init; }
    public Dictionary<string, object?> SearchData { get; init; }

    public string WorkflowStatus { get; init; }

    public ParserReference Parser { get; init; }

    public DateTimeOffset SourceReceivedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
```

`ParsedData` contains full normalized domain data.

For the current job domain it contains:

```text
company
location
employmentType
salary
description
url
```

`DisplayTitle` is the job title and `SourceExternalId` is the provider's job identifier. Gmail message identifiers belong to email-ingestion metadata rather than `ParsedData`.

`SearchData` contains scalar values required by known dashboard queries and indexes.

Do not over-index arbitrary parsed fields.

---

# 23. PostgreSQL principles

PostgreSQL is the application database for the first version.

Entity Framework Core belongs in `App.Infrastructure`. Keep entity configuration, migrations, and PostgreSQL-specific queries out of controllers and reusable domain logic.

Use normalized relational tables for ownership, immutable versions, jobs, status history, and artifact metadata. JSONB is appropriate for generic `ParsedData`, `SearchData`, and immutable source snapshots where the schema is intentionally domain-configurable.

Every user-owned row must carry or be unambiguously joined to a workspace key. Repository and service queries must scope by workspace before resource ID.

Core tables include:

```text
demo_sessions
source_items
status_history
document_templates
document_template_versions
user_rule_documents
user_rule_versions
generation_jobs
generated_documents
generated_document_versions
compile_jobs
pdf_artifacts
gmail_accounts
email_messages
audit_events
```

Use transactions only where concurrency or multi-row consistency matters.

---

# 24. PostgreSQL transactions

Use transactions for:

- status update + status-history record,
- job claiming,
- lease reclaiming,
- template current-version pointer update,
- rule current-version pointer update,
- generated-document current-version pointer update.

Do not wrap every immutable insert in a transaction without reason.

---

# 25. PostgreSQL query design

Design queries deliberately.

Expected dashboard filters:

- owner,
- workflow status,
- source,
- created/received time,
- cursor pagination.

Use keyset/cursor pagination based on a stable ordered tuple such as `(receivedAt, id)`.

Avoid offset pagination for growing job lists.

Do not promise arbitrary full-text search.

Use deliberate PostgreSQL indexes for workspace, status, source, provider external ID, and received time. Do not add Elasticsearch solely for convenience.

---

# 26. Workflow statuses

Statuses must come from domain configuration.

Do not create:

```csharp
enum JobPostingStatus
```

Use a generic code/value object.

Changing document-generation state must not implicitly change workflow status.

Example:

```text
Tailor CV
```

must NOT automatically do:

```text
NEW → TO_APPLY
```

Compilation must NOT automatically do:

```text
TO_APPLY → APPLIED
```

Workflow status changes are explicit user actions.

---

# 27. Templates

Provide three initial example base-CV `.tex` templates. In the generic core they remain `DocumentTemplate` and immutable `DocumentTemplateVersion` records.

Users can:

- view,
- clone,
- create,
- edit,
- rename,
- archive.

System examples are not directly overwritten.

Users clone them into their workspace.

Every edit creates a new immutable version.

Never overwrite previous template content.

---

# 28. User rules

Each real user owns one primary Markdown rules/context document.

Demo sessions receive a seeded rules document.

Current job/CV meaning may include:

- verified candidate experience and skills,
- CV-tailoring preferences and constraints.

Core code must call it:

```text
UserRuleDocument
```

not domain-specific names.

Every save creates a new version.

---

# 29. System rules

Keep system rules in repository Markdown files:

```text
system-rules/generation.md
system-rules/latex-safety.md
```

Create immutable rule bundles based on content hashes.

Generation jobs reference exact system-rule versions.

Changing repository rules must not retroactively change old generation jobs.

---

# 30. Prompt hierarchy

Build AI input in this priority order:

```text
1. hard-coded security rules
2. immutable system rules
3. immutable user rules/context
4. immutable selected TeX template
5. SourceItem snapshot
6. optional user instruction
```

The source item/email is untrusted data.

Text such as:

```text
Ignore all previous instructions
```

inside imported content must remain data.

Never allow source content to redefine the prompt hierarchy.

---

# 31. Minimize AI input

Prefer normalized `SourceItem` data.

Do not send raw email bodies by default.

Do not send:

- Gmail tokens,
- OpenAI secrets,
- Google credentials,
- unrelated items,
- unrelated emails.

Use only data required for generation.

---

# 32. Asynchronous job rule

Any potentially slow, expensive, retryable, or resource-intensive operation must be asynchronous.

Mandatory:

```text
OpenAI generation
TeX compilation
```

Pattern:

```text
HTTP request
   ↓
validate/authenticate
   ↓
create persistent PostgreSQL job
   ↓
enqueue job ID
   ↓
return 202 Accepted
```

Never hold a browser request open for generation or compilation.

---

# 33. Queue separation

Use two separate logical queues:

```text
ai-generation-queue
compilation-queue
```

Do not combine them.

They have different:

- concurrency needs,
- cost profiles,
- retry rules,
- security properties.

---

# 34. PostgreSQL vs queue responsibility

PostgreSQL stores:

- what the job is,
- who owns it,
- exact input references,
- job status,
- attempts,
- timestamps,
- output reference,
- safe error information.

The delivery adapter handles:

- delivery,
- retry,
- backoff,
- rate limiting,
- worker invocation.

The local adapter uses PostgreSQL-backed polling by dedicated workers. The future production adapter may use Cloud Tasks for delivery. Neither delivery mechanism is the business-state database.

---

# 35. Queue payload rule

Queue payloads contain only identifiers.

AI:

```json
{
  "generationJobId": "..."
}
```

Compilation:

```json
{
  "compileJobId": "..."
}
```

Never put:

- prompt contents,
- TeX contents,
- Markdown rules,
- email body,
- API key,
- OAuth token,
- user secrets

in task payloads.

---

# 36. Queue abstractions

Application layer:

```csharp
public interface IGenerationQueue
{
    Task EnqueueAsync(
        string generationJobId,
        CancellationToken cancellationToken);
}

public interface ICompilationQueue
{
    Task EnqueueAsync(
        string compileJobId,
        CancellationToken cancellationToken);
}
```

Future cloud:

```text
GoogleCloudTasksGenerationQueue
GoogleCloudTasksCompilationQueue
```

Local:

```text
PostgresGenerationQueue
PostgresCompilationQueue
dedicated database-polling workers
```

Tests may use:

```text
RecordingGenerationQueue
RecordingCompilationQueue
```

Controllers must depend on abstractions.

---

# 37. Job status model

Use:

```text
QUEUED
RUNNING
SUCCEEDED
FAILED
```

Compilation additionally supports:

```text
TIMED_OUT
```

Cancellation is not required for MVP.

---

# 38. Job leases

Do not claim a job using only:

```text
QUEUED → RUNNING
```

A worker can crash while `RUNNING`.

Use:

```text
leaseUntil
attemptCount
```

A retry may reclaim a `RUNNING` job only when its lease expired.

A `SUCCEEDED` job must never run again.

Claim/reclaim jobs transactionally.

---

# 39. Retry safety

Workers must assume duplicate delivery can happen.

Processing must be idempotent.

Before doing expensive work:

1. load job,
2. transactionally claim it,
3. verify lease/state,
4. process only if successfully claimed.

Do not rely only on queue-level deduplication.

---

# 40. Retry classification

Distinguish permanent failures from transient failures.

Transient examples:

- network error,
- rate limit,
- temporary OpenAI failure,
- temporary PostgreSQL failure,
- temporary GCS failure.

Permanent examples:

- referenced version missing,
- invalid application state,
- deterministic TeX syntax error,
- unsafe TeX,
- unsupported package,
- invalid structured AI result after allowed attempts.

For permanent failures:

1. mark job `FAILED`,
2. persist safe error,
3. return success to Cloud Tasks so it does not retry forever.

For transient failures:

1. preserve retry-safe state,
2. return retriable worker failure,
3. allow Cloud Tasks backoff/retry.

---

# 41. AI worker

`App.AiWorker` is a private Cloud Run service.

It must:

1. receive generation job ID,
2. authenticate Cloud Tasks invocation,
3. claim the job,
4. load exact immutable inputs,
5. choose provider,
6. generate,
7. validate structured output,
8. create generated document/version,
9. mark job successful.

Public internet users must not invoke the worker directly.

---

# 42. AI provider abstraction

Create:

```csharp
public interface IAiDocumentGenerator
{
    Task<AiGenerationResult> GenerateAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken);
}
```

Implement:

```text
OpenAiDocumentGenerator
DemoDocumentGenerator
```

Do not spread OpenAI-specific SDK types through the application.

---

# 43. Real AI behavior

Real users use `OpenAiDocumentGenerator`.

Configuration:

```text
OPENAI_API_KEY
OPENAI_MODEL
```

Do not hard-code model names throughout the solution.

Use structured output.

Validate response before persistence.

Prefer request settings that avoid unnecessary remote retention.

Do not write complete prompts/responses into Cloud Logging.

---

# 44. Demo AI behavior

Demo visitors use `DemoDocumentGenerator`.

Do not provide unlimited public OpenAI access.

The demo provider should:

- still run through `GenerationJob`,
- still run through Cloud Tasks,
- still run through the AI worker,
- produce realistic deterministic TeX output,
- optionally simulate a short processing delay,
- create a normal versioned generated document.

Do not bypass the architecture in demo mode.

The demo should demonstrate the real queue/job workflow even if the expensive model call is replaced.

---

# 45. AI output contract

Use structured output similar to:

```json
{
  "tex": "\\documentclass{article}...",
  "summary": [],
  "assumptions": [],
  "warnings": [],
  "usedSourceFields": []
}
```

Reject malformed output.

Do not parse arbitrary Markdown code fences.

Do not persist assistant commentary as TeX.

---

# 46. Generated documents

Use:

```text
GeneratedDocument
GeneratedDocumentVersion
```

Version origins may include:

```text
AI
DEMO_GENERATOR
USER_EDIT
AI_REPAIR
```

Never mutate old generated-document versions.

A user edit creates a new version.

---

# 47. Compiler queue

Compilation uses its own:

```text
compilation-queue
```

Suggested initial concurrency is low.

Compiler Cloud Run container concurrency should be:

```text
1
```

The compiler is CPU/security sensitive.

Do not optimize for high throughput.

---

# 48. Compiler worker

`App.CompilerWorker` must be private.

It must:

1. authenticate Cloud Tasks invocation,
2. claim CompileJob,
3. load exact TeX version,
4. run static security checks,
5. create isolated temporary directory,
6. invoke Tectonic,
7. upload PDF/log,
8. create `PdfArtifact`,
9. mark job success/failure,
10. clean temporary files.

---

# 49. TeX security

Treat every TeX document as hostile.

This includes:

- system example template,
- user template,
- AI output,
- demo-generated output,
- user-edited TeX.

Never trust source based on origin.

---

# 50. Tectonic execution

Use untrusted/offline compilation.

Conceptually:

```text
tectonic
-X compile
--untrusted
--only-cached
--keep-logs
--outdir ...
main.tex
```

Bake required dependencies into the compiler image.

Do not download arbitrary TeX packages during compilation.

Never enable shell escape.

---

# 51. Static TeX validation

Perform defense-in-depth checks for dangerous/unwanted constructs such as:

```text
\write18
shell escape
../
absolute paths
unexpected \input
unexpected \include
```

Static validation is not the security boundary.

The isolated compiler is the security boundary.

---

# 52. Compiler isolation

Compiler worker must:

- run as non-root,
- use an ephemeral working directory,
- have no Gmail credentials,
- have no OpenAI credentials,
- have no OAuth refresh tokens,
- have minimal PostgreSQL permissions,
- have minimal GCS permissions,
- enforce input-size limits,
- enforce timeout,
- process one job at a time per instance,
- delete temporary files in `finally`.

---

# 53. Compilation timeout

Use a configurable application-level timeout.

Initial target:

```text
20–30 seconds
```

If exceeded:

- terminate child process,
- mark job `TIMED_OUT`,
- do not automatically retry deterministic pathological input.

---

# 54. Demo compilation

Demo mode should use the real compiler pipeline.

This is intentional.

Demo users should exercise:

```text
CompileJob
→ compilation queue
→ private compiler
→ real Tectonic
→ private PDF
```

Use stricter per-session quotas.

Example:

```text
10 compilations/session
```

---

# 55. PDF storage

Store PDFs in private Google Cloud Storage.

Never make the bucket public.

Suggested paths:

Real:

```text
users/{userId}/documents/{documentId}/versions/{versionId}/{compileJobId}.pdf
```

Demo:

```text
demo/{sessionId}/documents/{documentId}/versions/{versionId}/{compileJobId}.pdf
```

PostgreSQL stores only artifact metadata.

---

# 56. PDF download

Download/preview flow:

1. authenticate user,
2. resolve workspace,
3. authorize artifact,
4. generate short-lived signed URL,
5. return/redirect.

Suggested URL lifetime:

```text
~5 minutes
```

Never return permanent public object URLs.

---

# 57. Frontend job UX

Generation:

```text
Queued
→ Generating...
→ Generated
```

Compilation:

```text
Queued for compilation
→ Compiling...
→ PDF ready
```

Poll job endpoints approximately every 1–2 seconds while active.

Stop polling when terminal.

WebSockets are not required for MVP.

---

# 58. Security and authorization

Every resource access must be scoped to the active workspace.

Real:

```text
resource.userId == currentUser.userId
```

Demo:

```text
resource.demoSessionId == currentUser.demoSessionId
```

Never authorize using only a resource ID.

Prefer repository/service signatures that require owner/workspace identity.

---

# 59. IDOR testing

Explicitly test:

- real user A cannot access real user B,
- real user cannot access demo data,
- demo cannot access real-user data,
- demo session A cannot access demo session B.

Cover:

- SourceItem,
- template,
- rules,
- GenerationJob,
- generated document,
- CompileJob,
- PdfArtifact,
- Gmail configuration.

Prefer `404` where resource existence should not be leaked.

---

# 60. Logging rules

Use structured logs.

Useful fields:

```text
correlationId
userId or demoSessionId
jobId
sourceItemId
worker type
duration
status
```

Never log:

- Gmail refresh token,
- Gmail access token,
- OpenAI API key,
- OAuth client secret,
- full real email body,
- complete prompt,
- complete `rules.md`,
- complete generated TeX.

Log identifiers, hashes, counts, durations, statuses.

---

# 61. Audit events

Record important application actions such as:

```text
GMAIL_CONNECTED
GMAIL_DISCONNECTED
SOURCE_ITEM_CREATED
ITEM_STATUS_CHANGED
TEMPLATE_CREATED
TEMPLATE_UPDATED
RULES_UPDATED
GENERATION_REQUESTED
GENERATION_COMPLETED
GENERATION_FAILED
COMPILE_REQUESTED
COMPILE_COMPLETED
COMPILE_FAILED
PDF_DOWNLOADED
```

Do not place large or sensitive content inside audit metadata.

---

# 62. Rate limiting

Apply per-user/per-session limits.

Examples:

Real:

```text
AI generations: configurable, e.g. 20/day/user
compiles: configurable, e.g. 100/day/user
```

Demo:

```text
demo-session creation: rate-limited
demo generation: limited/session
compiles: limited/session
```

Also limit:

- Cloud Run max instances,
- AI queue concurrency,
- compiler queue concurrency.

Public demo must not create uncontrolled cost.

---

# 63. Secrets and IAM

Store secrets in Secret Manager.

Use separate service accounts:

```text
sa-web
sa-gmail-sync
sa-ai-worker
sa-compiler-worker
sa-demo-cleanup
sa-ai-task-invoker
sa-compile-task-invoker
```

Use least privilege.

The compiler must not receive credentials unrelated to compiling/storing its artifact.

The AI worker must not receive Gmail refresh-token decrypt permission.

The Gmail worker must not receive OpenAI secrets.

---

# 64. Cloud Tasks authentication

AI queue:

```text
ai-generation-queue
→ sa-ai-task-invoker
→ App.AiWorker
```

Compile queue:

```text
compilation-queue
→ sa-compile-task-invoker
→ App.CompilerWorker
```

Workers must reject unauthenticated/public invocation.

---

# 65. Testing expectations

Every meaningful feature requires tests.

Do not postpone all testing until the end.

## Unit tests

Cover:

- domain configuration,
- parser matching,
- parsers,
- normalization,
- prompt builder,
- provider selection,
- TeX validation,
- workflow status validation,
- job error classification.

## Integration tests

Cover:

- PostgreSQL repository behavior,
- transactions,
- status history,
- user isolation,
- demo-session isolation,
- job claiming,
- expired leases,
- version creation,
- deduplication.

## Queue tests

Cover:

- job persisted before enqueue,
- payload contains only ID,
- duplicate delivery,
- active lease,
- expired lease,
- already-successful job,
- transient retry,
- permanent failure.

## Demo tests

Cover:

- demo session creation,
- fake emails pass through real parsers,
- demo provider routing,
- demo compile flow,
- session isolation,
- cleanup.

## Security tests

Cover:

- IDOR,
- OAuth state,
- unauthorized worker invocation,
- unsafe TeX,
- path traversal,
- oversized input,
- signed URL authorization.

## E2E

Cover the public demo happy path and the real-user flow where practical.

---

# 66. Frequent git commits

Make frequent, focused commits.

Do not accumulate an entire multi-phase implementation into one large commit.

Preferred rhythm:

```text
1 meaningful behavior / refactor / test group
=
1 commit
```

A typical implementation phase should result in several commits.

Examples:

```text
feat(domain): add domain configuration model
feat(api): expose domain configuration endpoint
test(domain): validate invalid domain configuration

feat(auth): add whitelisted Google authentication
test(auth): reject non-whitelisted users

feat(parsers): add source parser abstraction
feat(parsers): implement LinkedIn job parser
test(parsers): add LinkedIn regression fixtures

feat(queue): add generation queue abstraction
feat(queue): add Cloud Tasks generation queue
test(queue): verify generation job idempotency

feat(compiler): add Tectonic process runner
feat(compiler): enforce timeout and cleanup
test(compiler): reject unsafe TeX inputs
```

Commit before moving to a new architectural concern.

Do not mix unrelated changes.

---

# 67. Commit size

Prefer commits that are:

- independently understandable,
- buildable where practical,
- testable,
- revertable.

Avoid commits that modify dozens of unrelated files.

If one requested feature requires:

```text
domain model
repository
API
frontend
tests
```

it may reasonably become multiple commits.

---

# 68. Commit message style

Use Conventional Commit-style messages where practical:

```text
feat(...)
fix(...)
refactor(...)
test(...)
docs(...)
chore(...)
build(...)
ci(...)
```

Examples:

```text
feat(demo): create isolated demo sessions
fix(parsers): handle missing description in LinkedIn job emails
refactor(queue): extract generation job claiming service
test(security): cover cross-demo-session access
docs(architecture): document AI worker boundary
```

Keep messages specific.

Do not use vague commits such as:

```text
updates
changes
fix stuff
wip
more work
```

---

# 69. Never commit broken intermediate work intentionally

Before a normal commit, run the relevant fast checks.

At minimum for backend changes:

```bash
dotnet build
dotnet test
```

For frontend changes:

```bash
npm run lint
npm run typecheck
npm run build
```

Use the exact scripts available in the repository.

If the full suite is expensive, run the relevant targeted suite before each commit and full suite before completing the phase.

---

# 70. Git discipline for agents

Before editing:

```text
git status
```

Understand existing uncommitted changes.

Do not delete or overwrite unrelated user changes.

Do not reset or clean the repository destructively unless explicitly instructed.

Avoid:

```text
git reset --hard
git clean -fd
```

unless the user explicitly asks for destructive cleanup.

Do not rewrite published history unless explicitly requested.

Do not force-push.

---

# 71. Existing changes

If unrelated changes already exist:

- preserve them,
- work around them,
- avoid formatting unrelated files,
- avoid staging unrelated changes.

Stage only files relevant to the current commit.

Use focused commits.

---

# 72. Commit after tests pass

The preferred cycle is:

```text
inspect
  ↓
implement small slice
  ↓
run targeted tests
  ↓
review diff
  ↓
commit
  ↓
continue
```

Do not wait until the entire project is complete before committing.

---

# 73. Phase completion rule

Before declaring an implementation phase complete:

1. inspect `git diff`,
2. run relevant tests,
3. run build/typecheck,
4. ensure no secrets were added,
5. ensure docs were updated if architecture changed,
6. ensure work is committed in focused commits,
7. summarize what changed,
8. mention any remaining limitations.

---

# 74. Coding style

Prefer clear C# over clever C#.

Prefer:

- explicit names,
- small services,
- dependency injection,
- immutable records/value objects where helpful,
- asynchronous I/O,
- `CancellationToken`,
- nullable reference types,
- central configuration,
- structured exceptions/error results.

Avoid:

- deeply nested conditionals,
- magic strings scattered everywhere,
- static service locators,
- giant service classes,
- generic repository abstractions that hide important PostgreSQL behavior,
- unnecessary reflection,
- unnecessary metaprogramming.

---

# 75. Async rules

Use asynchronous APIs for:

- PostgreSQL,
- Gmail,
- OpenAI,
- GCS,
- Cloud Tasks,
- process management where practical.

Pass `CancellationToken` through service boundaries.

Do not use:

```csharp
.Result
.Wait()
```

on asynchronous operations in application code.

---

# 76. API design

Controllers/endpoints should remain thin.

A controller should typically:

1. authenticate/resolve current user,
2. validate request,
3. call application service,
4. map result to HTTP response.

Do not put parser logic, Entity Framework queries, OpenAI prompt construction, or Tectonic process management directly in controllers.

---

# 77. HTTP semantics

Use appropriate responses.

Examples:

```text
200 OK
201 Created
202 Accepted
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
429 Too Many Requests
```

Asynchronous creation endpoints should normally return:

```text
202 Accepted
```

with job identifier/status resource.

---

# 78. Validation

Validate all external inputs.

Centralize limits.

Examples:

```text
user Markdown: ~100 KB
template TeX: ~250 KB
generated TeX: ~500 KB
optional generation instruction: ~5 KB
```

Do not accept unlimited user content.

Validate AI structured results as untrusted external data.

Validate parser outputs before persistence.

---

# 79. Error handling

Use safe user-facing errors.

Do not expose stack traces.

Persist safe structured job errors such as:

```json
{
  "code": "AI_RATE_LIMIT",
  "category": "TRANSIENT",
  "message": "Generation service temporarily unavailable."
}
```

Detailed diagnostics belong in protected structured logs.

---

# 80. Do not overengineer

Expected initial scale:

```text
2 real users
+
small public demo
```

Optimize for:

- clarity,
- security,
- maintainability,
- low operational burden,
- low cost.

Do not build infrastructure for thousands of users unless a requirement changes.

Avoid speculative abstractions.

Add abstractions mainly at external-service boundaries or true architectural seams.

---

# 81. Do not underengineer security

Small user count is NOT a reason to weaken:

- authentication,
- authorization,
- token encryption,
- worker authentication,
- prompt-injection boundaries,
- compiler sandboxing,
- signed PDF URLs,
- demo isolation.

Security boundaries remain mandatory.

---

# 82. Documentation requirements

Update relevant docs when behavior changes.

Important files:

```text
docs/ARCHITECTURE.md
docs/SECURITY.md
docs/DOMAIN.md
docs/PARSERS.md
docs/QUEUES.md
docs/DEMO_MODE.md
```

Do not let documentation significantly drift from implementation.

Architectural changes require documentation in the same work.

---

# 83. Infrastructure as code

Production GCP infrastructure should be represented in Terraform.

Expected resources include:

- Artifact Registry,
- Cloud SQL for PostgreSQL,
- private GCS bucket,
- App.Api Cloud Run service,
- App.AiWorker private Cloud Run service,
- App.CompilerWorker private Cloud Run service,
- App.GmailSync Cloud Run Job,
- App.DemoCleanup Cloud Run Job,
- Gmail Cloud Scheduler trigger,
- demo cleanup scheduler,
- AI Cloud Tasks queue,
- compilation Cloud Tasks queue,
- KMS,
- Secret Manager,
- service accounts,
- IAM,
- PostgreSQL indexes and migration execution.

Avoid manual production-only configuration that is not documented or codified.

---

# 84. CI expectations

Pull-request CI should run relevant:

```text
dotnet restore
dotnet build
dotnet test

frontend install
frontend lint
frontend typecheck
frontend tests
frontend build
```

Normal CI should not call real OpenAI.

Use mocks/fakes.

Compiler security tests should run in a controlled environment.

---

# 85. MVP non-goals

Do not implement unless explicitly requested:

- general registration,
- admin dashboard,
- teams,
- roles,
- subscriptions,
- billing,
- automatic submission to job boards,
- automatic publishing,
- AI-based email parsing,
- autonomous agents,
- RAG,
- embeddings,
- vector database,
- multiple AI providers,
- mobile application,
- collaborative editor,
- WebSockets,
- automatic workflow status transitions,
- automatic infinite AI repair loops.

---

# 86. Implementation strategy

Work incrementally.

Do not attempt the whole application in one prompt or one commit.

Preferred sequence:

```text
scaffolding
→ domain config
→ auth
→ demo sessions
→ PostgreSQL foundation
→ parser framework
→ demo seeding
→ dashboard
→ templates
→ rules
→ Gmail OAuth
→ Gmail sync
→ GenerationJob
→ AI queue
→ provider routing
→ OpenAI
→ generated documents
→ CompileJob
→ compilation queue
→ compiler worker
→ PDF storage
→ demo limits
→ cleanup
→ security hardening
→ observability
→ Terraform
→ E2E
```

---

# 87. Before each task

Before changing code:

1. inspect relevant files,
2. read relevant docs,
3. inspect `git status`,
4. understand existing abstractions,
5. identify the smallest coherent implementation slice,
6. identify tests required,
7. avoid rewriting unrelated code.

Do not start by generating large amounts of code blindly.

---

# 88. During each task

While implementing:

- keep changes focused,
- reuse existing conventions,
- avoid unnecessary dependencies,
- add tests with behavior,
- run targeted checks frequently,
- commit frequently,
- update docs when architecture changes.

If a design conflicts with this file, follow this file unless the user explicitly overrides it.

---

# 89. When requirements are ambiguous

Prefer the option that is:

1. simpler,
2. safer,
3. easier to test,
4. easier to replace,
5. consistent with existing architecture.

Do not introduce speculative features to solve hypothetical future requirements.

If the ambiguity materially changes architecture, document the assumption in the implementation summary.

---

# 90. Domain evolution rule

The active domain is job-post ingestion and tailored CV generation. When adding or changing job providers, parsed fields, CV content, or application workflow terminology, primarily modify:

```text
config/domain.json
App.Parsers/Sources/*
parser fixtures
demo email fixtures
demo generated-document fixtures
system-rules/*
example-templates/*
domain-specific presentation text
```

Do not unnecessarily rewrite:

```text
authentication
real-user allowlist
demo-session architecture
PostgreSQL infrastructure
Gmail OAuth
Gmail synchronization
SourceItem abstraction
GenerationJob
AI queue
provider architecture
CompileJob
compilation queue
compiler
GCS artifact storage
job idempotency
security boundaries
Terraform architecture
```

---

# 91. Final engineering principle

The project should remain understandable to another engineer without reverse-engineering hidden conventions.

Prefer:

```text
explicit architecture
small focused commits
tested behavior
documented boundaries
simple abstractions
least privilege
immutable history
retry-safe jobs
```

over:

```text
clever shortcuts
large hidden side effects
one huge service
one huge commit
shared mutable demo data
synchronous expensive work
implicit authorization
domain-specific coupling
```

When in doubt, protect the core invariant:

```text
SOURCE DATA
  ↓
DETERMINISTIC NORMALIZATION
  ↓
EXPLICIT USER WORKFLOW
  ↓
IMMUTABLE GENERATION INPUTS
  ↓
QUEUED AI WORK
  ↓
VERSIONED TEX
  ↓
QUEUED ISOLATED COMPILATION
  ↓
PRIVATE PDF
```
