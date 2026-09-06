# AGENTS.md

## 1. Purpose

This repository contains a small job-posting ingestion and tailored-CV generation application.

The application:

1. imports job-alert emails,
2. parses supported providers with deterministic parsers,
3. stores normalized job postings,
4. lets users manage application workflow statuses,
5. lets users maintain CV templates and verified candidate rules,
6. generates tailored TeX CV versions through OpenAI,
7. compiles selected TeX versions asynchronously,
8. stores generated PDFs privately,
9. supports two real users and isolated public demo sessions.

This file is the primary coding-agent instruction file.

Detailed design belongs in `docs/`. Do not duplicate large design documents here.

---

## 2. Core engineering priorities

When implementing or changing code, optimize for:

1. clarity,
2. maintainability,
3. security,
4. testability,
5. small coherent changes,
6. easy local development,
7. easy future cloud migration.

Do not optimize for:

- minimum file count,
- clever abstractions,
- speculative future domains,
- unnecessary infrastructure,
- premature scalability.

The application has two real users plus a small public demo.

Do not design as though it serves thousands of users unless requirements change.

---

## 3. Approved stack

### Backend

- .NET 10
- ASP.NET Core
- C#
- Entity Framework Core
- PostgreSQL 17
- xUnit

### Frontend

- React
- TypeScript
- Vite
- Tailwind CSS
- shadcn/ui
- Monaco Editor where a TeX editor is needed
- PDF.js where PDF preview is needed
- Playwright for E2E tests

### External services

- Gmail API
- OpenAI Responses API
- Tectonic

### Local infrastructure

- PostgreSQL in Docker
- local private artifact storage
- PostgreSQL-backed worker polling

### Future cloud target

- Google Cloud Run
- managed PostgreSQL
- Google Cloud Storage
- Google Cloud Tasks
- Google Cloud Scheduler
- Secret Manager
- Cloud KMS
- Artifact Registry
- Terraform

Do not introduce MongoDB, Redis, RabbitMQ, Kafka, Kubernetes, Elasticsearch, a vector database, RAG, or another infrastructure dependency without a concrete requirement.

---

## 4. Required repository structure

Keep the solution approximately:

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
  src/
    app/
    features/
    components/
    lib/

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
```

Do not collapse the backend into one project.

Do not collapse a non-trivial frontend into `App.tsx` and `main.tsx`.

The smallest coherent implementation does **not** mean the smallest possible number of files.

---

## 5. Domain language

Prefer clear domain-specific names.

Use concepts such as:

```text
JobPosting
JobEmailParser
ApplicationStatus
CvTemplate
CvTemplateVersion
CandidateRuleDocument
CandidateRuleVersion
CvGenerationJob
GeneratedCv
GeneratedCvVersion
CvCompileJob
PdfArtifact
```

Do not invent generic names such as `SourceItem` or `GeneratedDocument` merely to make the application reusable for hypothetical unrelated future products.

Abstract true architectural seams, not the product domain.

Good abstractions include:

```text
IJobPostingRepository
ICvTemplateRepository
ICandidateRuleRepository
ICvGenerationQueue
ICvCompilationQueue
IAiCvGenerator
IArtifactStorage
IEmailProvider
ITokenCipher
```

---

## 6. Dependency direction

Keep backend dependencies approximately:

```text
App.Api
   ↓
App.Application
   ↓
App.Domain
```

`App.Infrastructure` implements interfaces required by the application/domain layers.

`App.Parsers` contains provider-specific email parsing.

Workers may depend on application abstractions and infrastructure composition, but core business rules must not depend on:

- EF Core types,
- OpenAI SDK types,
- Gmail SDK types,
- Google Cloud SDK types,
- HTTP controller types.

Do not use `DbContext` directly from controllers.

Do not call OpenAI, Gmail, Tectonic, or Cloud Tasks directly from controllers.

---

## 7. Backend code organization

Organize backend code by meaningful feature and responsibility.

Preferred shape:

```text
App.Application/
  JobPostings/
  ApplicationWorkflow/
  CvTemplates/
  CandidateRules/
  CvGeneration/
  CvCompilation/
  Demo/
  Gmail/

App.Infrastructure/
  Persistence/
  Authentication/
  Gmail/
  OpenAI/
  Queues/
  Storage/
  Encryption/

App.Api/
  Endpoints/
    JobPostings/
    Workflow/
    CvTemplates/
    CandidateRules/
    CvGeneration/
    CvCompilation/
    Demo/
    Gmail/
```

Do not create giant `Services/`, `Helpers/`, or `Managers/` folders containing unrelated code.

Avoid 800-line service classes.

Split by responsibility, not by arbitrary line-count rules.

Read `docs/BACKEND.md` before substantial backend changes.

---

## 8. Frontend architecture

The frontend must use feature-oriented organization.

Expected structure:

```text
frontend/src/
  app/
    App.tsx
    router.tsx
    providers/

  features/
    job-postings/
    cv-templates/
    candidate-rules/
    cv-generation/
    generated-cvs/
    demo/
    gmail/

  components/
    ui/
    layout/
    common/

  lib/
    api/
    config/
    errors/

  types/
```

Each non-trivial feature may contain:

```text
api/
components/
hooks/
pages/
types/
utils/
```

Create only the folders a feature actually needs.

### Non-negotiable frontend rules

Do not put substantial feature implementation directly in `App.tsx`.

`App.tsx` should mainly provide:

- routing,
- providers,
- top-level layout composition.

Do not scatter `fetch()` calls through React components.

Do not keep all application API functions in one giant file.

Do not keep all application types in one giant file.

Do not duplicate server state across multiple `useState` variables without reason.

Do not place application-specific feature components in `components/ui`.

Do not implement API calls, polling, complex form logic, business transformations, and hundreds of lines of JSX in one component.

A non-trivial feature should normally have multiple focused files.

Example:

```text
features/job-postings/
  api/jobPostingsApi.ts
  components/JobPostingCard.tsx
  components/JobPostingFilters.tsx
  hooks/useJobPostings.ts
  hooks/useUpdateApplicationStatus.ts
  pages/JobPostingsPage.tsx
  types/jobPosting.ts
```

Read `docs/FRONTEND.md` before substantial frontend work.

---

## 9. API access from frontend

Central HTTP infrastructure belongs under:

```text
frontend/src/lib/api/
```

Feature-specific endpoint functions belong under:

```text
frontend/src/features/<feature>/api/
```

Centralize:

- API base URL,
- credentials configuration,
- JSON handling,
- shared HTTP error mapping.

Components should consume feature API functions or hooks rather than manually constructing endpoint URLs.

Use TanStack Query when server-state complexity justifies it.

Do not introduce Redux unless a concrete requirement justifies it.

---

## 10. Frontend component responsibilities

Page components coordinate feature components and hooks.

Presentational components render focused UI.

Hooks encapsulate reusable stateful behavior.

API modules perform HTTP interaction.

Utilities perform pure transformations.

Do not hide business logic inside JSX.

Do not split components mechanically just to reduce line counts.

Split when responsibilities differ.

A component that simultaneously contains:

- several API calls,
- polling,
- modal state,
- complex forms,
- data transformation,
- large rendering branches

should normally be decomposed.

---

## 11. Frontend async UX

Every asynchronous page or feature must deliberately handle:

- initial loading,
- empty results,
- request failure,
- successful content,
- mutation-in-progress where relevant.

Do not use `alert()` for normal error handling.

Generation UX:

```text
Queued
→ Generating...
→ Generated
```

Compilation UX:

```text
Queued for compilation
→ Compiling...
→ PDF ready
```

Polling belongs in one reusable implementation rather than being copied into multiple components.

WebSockets are not required for MVP.

---

## 12. PostgreSQL principles

PostgreSQL is the application database.

EF Core belongs in `App.Infrastructure`.

Use relational tables for:

- ownership,
- job postings,
- workflow status history,
- immutable versions,
- async jobs,
- artifact metadata,
- Gmail account metadata,
- ingestion metadata,
- audit events.

Use JSONB only where schema flexibility is deliberate and useful.

Every user-owned query must scope by workspace/user before resource ID.

Read `docs/DATABASE.md` before database schema changes.

---

## 13. Authentication and authorization

There are only:

- two allowlisted real users,
- isolated public demo sessions.

Do not implement:

- registration,
- user CRUD,
- organizations,
- invitations,
- teams,
- role management,
- admin user management.

Real users authenticate with Google OpenID Connect and an allowlist.

Use secure server-side cookie authentication.

Do not store auth tokens in `localStorage` or `sessionStorage`.

Never expose Gmail refresh tokens to the browser.

Every resource access must be scoped to the active real-user or demo workspace.

Never authorize by resource ID alone.

Read `docs/SECURITY.md`.

---

## 14. Demo mode

Each public demo visitor receives a random isolated demo session.

Never use one shared mutable demo account.

Demo users:

- can browse seeded fake jobs,
- can change workflow statuses,
- can edit demo templates/rules,
- use deterministic fake AI generation,
- may use the real compilation pipeline,
- cannot connect Gmail,
- cannot invoke real OpenAI.

Demo fixtures must pass through real parser code where practical.

Read `docs/DEMO_MODE.md`.

---

## 15. Email parsing

Use deterministic provider-specific parsers.

Example:

```text
App.Parsers/
  Sources/
    LinkedInJobEmailParser.cs
    JustJoinItJobEmailParser.cs
    NoFluffJobsJobEmailParser.cs
```

Each parser must expose a key/version and determine whether it can parse a message.

Parser selection may result in:

```text
MATCHED
UNSUPPORTED
AMBIGUOUS
```

If multiple parsers match, do not guess.

If none match, mark unsupported.

Do not use AI to choose or execute parsers in the MVP.

Every production parser bug fix requires a regression fixture/test.

Read `docs/PARSERS.md`.

---

## 16. CV templates and candidate rules

Provide three example `.tex` CV templates.

Users may:

- view,
- clone,
- create,
- edit,
- rename,
- archive.

Every edit creates a new immutable version.

Never overwrite historical template content.

Candidate rules/context are stored as an immutable versioned Markdown document.

System rules remain repository Markdown files.

Generation jobs must reference exact immutable versions.

---

## 17. Prompt hierarchy

AI input priority:

```text
1. hard-coded security rules
2. immutable system rules
3. immutable candidate rules/context
4. immutable selected CV template
5. immutable job-posting snapshot
6. optional user instruction
```

Imported job/email content is untrusted data.

Text such as:

```text
Ignore all previous instructions
```

inside imported content must remain data.

Do not send raw email bodies to AI by default.

Never send:

- Gmail tokens,
- OpenAI secrets,
- Google credentials,
- unrelated emails,
- unrelated job postings.

---

## 18. Slow operations must be asynchronous

Mandatory async operations:

- OpenAI CV generation,
- TeX compilation.

HTTP flow:

```text
request
→ authenticate/validate
→ create persistent PostgreSQL job
→ enqueue job ID
→ return 202 Accepted
```

Never keep a browser request open while waiting for AI generation or TeX compilation.

Use separate logical queues for:

```text
cv-generation
cv-compilation
```

Read `docs/QUEUES.md`.

---

## 19. Queue payloads

Queue payloads contain identifiers only.

Generation:

```json
{
  "cvGenerationJobId": "..."
}
```

Compilation:

```json
{
  "cvCompileJobId": "..."
}
```

Never put prompts, TeX, Markdown rules, email bodies, credentials, or secrets in queue payloads.

Workers must be idempotent and safe for duplicate delivery.

---

## 20. AI provider boundary

Application code depends on an abstraction such as:

```csharp
public interface IAiCvGenerator
{
    Task<AiCvGenerationResult> GenerateAsync(
        AiCvGenerationRequest request,
        CancellationToken cancellationToken);
}
```

Implementations may include:

```text
OpenAiCvGenerator
DemoCvGenerator
```

Do not spread OpenAI SDK types through the application.

Real OpenAI model names belong in configuration.

Validate structured AI output before persistence.

Do not parse arbitrary Markdown code fences as the primary contract.

---

## 21. Compiler boundary

Treat every TeX document as hostile.

This includes:

- example templates,
- user templates,
- AI output,
- demo-generated output,
- user-edited TeX.

Use Tectonic in isolated/untrusted/offline mode.

Never enable shell escape.

Compiler worker must:

- run as non-root,
- use ephemeral working directories,
- enforce input limits,
- enforce timeouts,
- have no Gmail credentials,
- have no OpenAI credentials,
- clean temporary files in `finally`.

Read `docs/SECURITY.md` and `docs/QUEUES.md`.

---

## 22. HTTP semantics

Use standard HTTP semantics.

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

Async job creation should normally return `202 Accepted` with a job identifier/status resource.

Controllers/endpoints should remain thin.

They should typically:

1. resolve current user/workspace,
2. validate request,
3. invoke application logic,
4. map result to HTTP.

---

## 23. Validation

Validate every external input.

Centralize size limits.

Validate:

- API requests,
- parser outputs,
- AI structured responses,
- TeX input,
- filenames/paths,
- domain configuration.

Do not accept unlimited user content.

Do not expose stack traces to users.

---

## 24. Testing expectations

Every meaningful feature requires appropriate tests.

### Unit tests

Use for:

- parser matching,
- parser normalization,
- workflow validation,
- prompt building,
- provider routing,
- TeX validation,
- error classification,
- pure utilities.

### Integration tests

Use real PostgreSQL where repository behavior matters.

Cover:

- ownership scoping,
- status history,
- job claiming,
- expired leases,
- immutable version creation,
- deduplication,
- demo isolation.

### Frontend tests

Test important feature behavior and reusable hooks.

Do not test implementation details merely to increase coverage.

### E2E

Cover:

- public demo happy path,
- important real-user flow where practical.

---

## 25. Git discipline

Before editing:

```bash
git status
```

Preserve unrelated changes.

Do not use destructive cleanup such as:

```bash
git reset --hard
git clean -fd
```

unless explicitly requested.

Make frequent focused commits.

Prefer:

```text
1 meaningful behavior/refactor/test group
=
1 commit
```

Use Conventional Commit-style messages where practical.

Examples:

```text
feat(frontend): add job posting feature structure
feat(parsers): implement LinkedIn email parser
test(parsers): add LinkedIn regression fixtures
refactor(api): split CV generation endpoint logic
fix(security): scope PDF lookup by workspace
```

Do not use vague messages such as `updates`, `changes`, `fix stuff`, or `wip`.

---

## 26. Task workflow for coding agents

Before each task:

1. inspect `git status`,
2. inspect the relevant code,
3. read the relevant doc in `docs/`,
4. understand existing conventions,
5. identify the smallest coherent behavior slice,
6. identify required tests,
7. avoid unrelated rewrites.

During the task:

1. keep responsibilities separated,
2. avoid monolithic files,
3. reuse existing conventions,
4. add tests with behavior,
5. run targeted checks,
6. review the diff,
7. commit a focused slice before moving to a different concern.

The smallest coherent slice may require several files.

Do **not** optimize for minimizing files changed.

---

## 27. Definition of done

Before declaring a backend task complete:

```bash
dotnet build
dotnet test
```

Use the exact repository commands when they differ.

Before declaring a frontend task complete:

```bash
npm run lint
npm run typecheck
npm test
npm run build
```

Run only scripts that actually exist.

For every completed feature:

1. inspect `git diff`,
2. verify code organization,
3. verify no secrets were added,
4. verify loading/error states where relevant,
5. verify tests,
6. update docs if architecture changed,
7. commit focused changes,
8. summarize remaining limitations.

A feature is not complete merely because it visually works.

---

## 28. Explicit anti-patterns

Do not create or preserve these patterns in non-trivial code:

### Frontend

```text
src/
  App.tsx
  main.tsx
```

as the complete structure after multiple application features exist.

Avoid:

- 500+ line `App.tsx`,
- one global `api.ts` with every endpoint,
- one global `types.ts` with every type,
- `fetch()` scattered through page components,
- duplicated polling logic,
- large business transformations inside JSX,
- application-specific feature components in `components/ui`,
- a single component managing an entire feature.

### Backend

Avoid:

- giant service classes,
- EF queries inside controllers,
- OpenAI calls inside controllers,
- queue SDK calls inside controllers,
- unrelated classes placed in generic `Helpers` folders,
- generic repository abstractions that hide useful PostgreSQL behavior,
- speculative abstractions for hypothetical future products.

---

## 29. Documentation routing

Before changing these areas, read:

| Area | Document |
|---|---|
| frontend | `docs/FRONTEND.md` |
| backend organization | `docs/BACKEND.md` |
| overall boundaries | `docs/ARCHITECTURE.md` |
| database | `docs/DATABASE.md` |
| security/auth/compiler isolation | `docs/SECURITY.md` |
| async jobs and queues | `docs/QUEUES.md` |
| email parsing | `docs/PARSERS.md` |
| demo behavior | `docs/DEMO_MODE.md` |
| future GCP deployment | `docs/CLOUD_ARCHITECTURE.md` |

Do not copy large sections from these documents back into `AGENTS.md`.

---

## 30. Final engineering principle

The project should be understandable to another engineer without reverse-engineering hidden conventions.

Prefer:

```text
clear domain names
feature boundaries
small focused files
small focused commits
tested behavior
explicit authorization
immutable history
retry-safe jobs
simple abstractions
```

over:

```text
generic domain abstractions
large hidden side effects
one huge component
one huge service
one huge commit
shared mutable demo data
synchronous expensive work
implicit authorization
```

When uncertain, choose the simpler design that keeps responsibilities clear and preserves security boundaries.
