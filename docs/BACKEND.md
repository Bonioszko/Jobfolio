# Backend Architecture

## Purpose

The backend is a .NET 10 solution using ASP.NET Core, Entity Framework Core, and PostgreSQL.

The goal is clear dependency direction and feature ownership without excessive Clean Architecture ceremony.

---

## Projects

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
```

Responsibilities:

### App.Domain

Contains core domain models and rules that do not depend on infrastructure.

Examples:

- `JobPosting`
- `ApplicationStatus`
- `CvTemplate`
- `CvTemplateVersion`
- `CandidateRuleDocument`
- `CandidateRuleVersion`
- `CvGenerationJob`
- `GeneratedCv`
- `GeneratedCvVersion`
- `CvCompileJob`
- `PdfArtifact`

### App.Application

Contains application use cases and abstractions needed to execute them.

Examples:

```text
JobPostings/
ApplicationWorkflow/
CvTemplates/
CandidateRules/
CvGeneration/
CvCompilation/
Demo/
Gmail/
```

### App.Infrastructure

Contains adapters and persistence.

```text
Persistence/
Authentication/
Gmail/
OpenAI/
Queues/
Storage/
Encryption/
```

### App.Api

Contains HTTP composition and endpoint/controller code.

Controllers/endpoints stay thin.

### App.Parsers

Contains deterministic provider-specific email parsers.

### Workers

`App.AiWorker`, `App.CompilerWorker`, `App.GmailSync`, and `App.DemoCleanup` are process boundaries for background work.

---

## Dependency rules

Preferred dependency flow:

```text
Api
 ↓
Application
 ↓
Domain
```

Infrastructure implements application/domain abstractions.

Domain must not reference:

- EF Core,
- PostgreSQL libraries,
- OpenAI SDK,
- Gmail SDK,
- Google Cloud SDK,
- ASP.NET controller types.

Application must not expose infrastructure SDK types in public interfaces.

---

## Application feature structure

Prefer feature-oriented organization.

Example:

```text
App.Application/
  JobPostings/
    GetJobPostings/
    GetJobPosting/
    ImportJobPosting/

  ApplicationWorkflow/
    ChangeApplicationStatus/

  CvTemplates/
    CreateCvTemplate/
    UpdateCvTemplate/
    GetCvTemplate/

  CvGeneration/
    RequestCvGeneration/
    GetCvGenerationJob/

  CvCompilation/
    RequestCvCompilation/
    GetCvCompileJob/
```

Do not mechanically implement CQRS libraries if simple classes/methods are sufficient.

Commands and queries are useful naming concepts; they do not require MediatR.

---

## Services

Prefer small services with explicit responsibilities.

Bad:

```text
JobService.cs
```

containing parsing, persistence, filtering, status changes, generation, and audit logging.

Good:

```text
JobPostingQueryService
ApplicationStatusService
CvGenerationRequestService
```

or use-case classes when they make ownership clearer.

Do not create interfaces for every class automatically.

Add an interface when:

- it represents a boundary,
- multiple implementations exist,
- isolation/testing materially benefits,
- infrastructure must implement application behavior.

---

## Repositories

Repositories should represent meaningful persistence behavior.

Good examples:

```text
IJobPostingRepository
ICvTemplateRepository
ICandidateRuleRepository
ICvGenerationJobRepository
ICvCompileJobRepository
IPdfArtifactRepository
```

Avoid one generic repository abstraction that hides all PostgreSQL capabilities.

Keep EF-specific implementation in Infrastructure.

---

## Controllers/endpoints

HTTP code should normally:

1. resolve the current workspace,
2. validate input,
3. call application logic,
4. map the result to HTTP.

Do not put:

- EF queries,
- parser logic,
- OpenAI prompt construction,
- queue SDK code,
- Tectonic process execution

inside controllers.

---

## Current user/workspace

Use one central abstraction to represent the active workspace.

Conceptually:

```csharp
public enum UserMode
{
    Real,
    Demo
}

public sealed record CurrentWorkspace(
    string WorkspaceId,
    UserMode Mode,
    string? Email,
    string? DemoSessionId);
```

Exact shape may differ.

Do not scatter checks such as:

```csharp
if (email == "demo@example.com")
```

through application code.

---

## Error handling

Prefer explicit application errors/results for expected failures.

Examples:

- validation failed,
- resource not found,
- workflow status invalid,
- quota exceeded,
- job already completed,
- compilation rejected.

Do not expose raw database or SDK exceptions to HTTP clients.

Log protected diagnostics with identifiers and correlation metadata.

---

## Async

Use asynchronous I/O for:

- PostgreSQL,
- Gmail,
- OpenAI,
- cloud storage,
- queue delivery,
- process execution where practical.

Pass `CancellationToken` through relevant boundaries.

Do not use `.Result` or `.Wait()` in application code.

---

## Validation

Validate inputs at the system boundary and enforce domain invariants where they belong.

Do not rely on frontend validation.

Validate parser output before persistence.

Validate AI structured output before creating a generated CV version.

Validate TeX before compilation.

---

## Tests

Unit tests belong around pure behavior.

Integration tests belong around real persistence and concurrency semantics.

Tests must cover:

- workspace scoping,
- status history,
- immutable version behavior,
- job claiming,
- expired leases,
- duplicate delivery,
- parser regressions.

---

## Backend anti-patterns

Avoid:

- giant service classes,
- giant controllers,
- generic `Helpers/` dumping grounds,
- generic repositories hiding useful DB behavior,
- direct SDK use in controllers,
- speculative abstraction for unrelated future domains,
- business logic in EF entity configuration,
- unnecessary reflection/metaprogramming.

---

## Definition of done

Before a backend change is complete:

```bash
dotnet build
dotnet test
```

Also:

1. inspect the diff,
2. verify dependency direction,
3. verify workspace authorization,
4. verify async APIs where relevant,
5. verify tests cover meaningful behavior,
6. update documentation when architecture changes.
