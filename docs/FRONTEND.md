# Frontend Architecture

## Purpose

The frontend is a React + TypeScript application.

Its architecture must make individual features easy to find, change, test, and delete.

The project must not become a single `App.tsx` containing API calls, business logic, dialogs, forms, polling, and rendering.

---

## Directory structure

Use this as the default:

```text
frontend/src/
  app/
    App.tsx
    router.tsx
    providers/

  features/
    job-postings/
      api/
      components/
      hooks/
      pages/
      types/
      utils/

    cv-templates/
      api/
      components/
      hooks/
      pages/
      types/

    candidate-rules/
      api/
      components/
      hooks/
      pages/
      types/

    cv-generation/
      api/
      components/
      hooks/
      types/

    generated-cvs/
      api/
      components/
      hooks/
      pages/
      types/

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

Do not create empty folders mechanically.

Create a folder when the feature has code that belongs there.

---

## App layer

`app/App.tsx` is top-level composition only.

It may contain:

- app providers,
- router,
- layout shell,
- global boundaries.

It should not contain implementation for job postings, CV templates, candidate rules, generation, or compilation.

`app/router.tsx` owns route definitions.

Provider setup belongs in `app/providers/`.

---

## Feature ownership

Feature-specific code stays with its feature.

For example:

```text
features/job-postings/
  api/jobPostingsApi.ts
  components/JobPostingCard.tsx
  components/JobPostingFilters.tsx
  components/ApplicationStatusSelect.tsx
  hooks/useJobPostings.ts
  hooks/useUpdateApplicationStatus.ts
  pages/JobPostingsPage.tsx
  types/jobPosting.ts
```

Do not move code to global shared folders until it is genuinely reused by more than one feature.

---

## API architecture

Generic HTTP client infrastructure:

```text
src/lib/api/
  apiClient.ts
  apiError.ts
```

Feature endpoints:

```text
features/job-postings/api/jobPostingsApi.ts
features/cv-templates/api/cvTemplatesApi.ts
features/cv-generation/api/cvGenerationApi.ts
```

Do not call `fetch()` directly throughout components.

Do not create one enormous `api.ts`.

Centralize:

- base URL,
- cookie credentials,
- JSON serialization/deserialization,
- common non-success response mapping,
- correlation/request metadata if used.

---

## Server state

Treat these as server state:

- job postings,
- workflow status,
- templates,
- rule versions,
- generation jobs,
- generated CV versions,
- compile jobs,
- PDF metadata.

Do not copy complete server objects into local state merely to render them.

Use TanStack Query when the application reaches enough server-state complexity that cache invalidation, polling, mutations, and request lifecycle management materially benefit from it.

Do not introduce Redux by default.

Local UI state is appropriate for:

- dialog open/closed,
- active tab,
- unsaved form fields,
- temporary filters,
- editor state.

---

## Hooks

Hooks should encapsulate meaningful reusable/stateful behavior.

Examples:

```text
useJobPostings
useUpdateApplicationStatus
useCvTemplates
useCvGenerationJob
useCompileJob
useJobPolling
```

Do not create hooks that only hide a single trivial expression.

Polling must not be reimplemented independently in multiple components.

---

## Pages

Route-level screens live in feature `pages/`.

Examples:

```text
features/job-postings/pages/JobPostingsPage.tsx
features/cv-templates/pages/CvTemplatesPage.tsx
features/candidate-rules/pages/CandidateRulesPage.tsx
features/generated-cvs/pages/GeneratedCvPage.tsx
```

Pages coordinate child components and hooks.

Pages should not become giant components containing every implementation detail.

---

## Components

Split components by responsibility.

A component should usually have one primary reason to change.

Signs a component should be decomposed:

- multiple unrelated API calls,
- several independent dialogs,
- complex form state,
- polling,
- large data transformations,
- several large rendering branches,
- hundreds of lines of JSX.

Do not split solely because a file exceeds an arbitrary number of lines.

A 40-line component can be badly designed.
A 180-line component can be fine.

Responsibility matters more than line count.

---

## Shared components

Use:

```text
components/ui/
```

for generic primitives, especially shadcn/ui components.

Use:

```text
components/layout/
```

for reusable application layout.

Use:

```text
components/common/
```

only for application components reused across multiple features.

Bad:

```text
components/ui/JobPostingCard.tsx
```

Good:

```text
features/job-postings/components/JobPostingCard.tsx
```

---

## Forms

Small forms may use controlled inputs.

For larger forms, React Hook Form plus schema validation is acceptable.

Do not add form libraries for trivial forms.

Submission code should be separate from large rendering blocks.

Show validation errors next to relevant fields.

Disable or clearly indicate in-progress submission.

---

## Types

Keep API/domain types close to the feature using them.

Good:

```text
features/job-postings/types/jobPosting.ts
features/cv-generation/types/cvGeneration.ts
```

Avoid:

```text
src/types.ts
```

containing every type in the application.

Do not introduce `any` when a meaningful type can be expressed.

Use `unknown` for genuinely unknown external data, then validate/narrow it.

---

## Domain configuration

Workflow statuses and configurable parsed field definitions come from the backend-provided domain configuration.

Do not duplicate authoritative status lists in React.

UI presentation metadata may be mapped locally when it is purely presentation behavior.

---

## Loading, error, and empty states

Every async feature must intentionally render:

- loading,
- failure,
- empty data,
- success.

Mutations should expose:

- pending,
- success behavior,
- error behavior.

Do not leave unhandled rejected promises.

Do not use `alert()` as normal application UX.

---

## CV generation and compilation

The frontend starts async jobs and polls job status.

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

Do not keep the HTTP request open until work completes.

Do not duplicate polling logic.

Stop polling on terminal job state.

---

## Editors

Monaco may be used for TeX editing where a code editor materially improves the experience.

Keep editor-specific logic separate from the route page where practical.

Do not make Monaco state the canonical persisted application state until an explicit save occurs.

---

## PDF preview

PDF.js may be used for private PDF preview.

The frontend must never assume a permanent public PDF URL.

Use the authorized backend flow for preview/download.

---

## Frontend testing

Prefer tests for user-visible behavior and reusable logic.

Useful targets:

- status changes,
- filter behavior,
- generation job state transitions,
- compile job polling,
- form validation,
- error-state rendering,
- demo restrictions.

Use Playwright for the public demo happy path.

Do not test trivial implementation details just to increase coverage.

---

## Anti-pattern examples

Do not let a non-trivial application remain:

```text
src/
  App.tsx
  main.tsx
```

Do not create:

```text
App.tsx
```

with every route, API call, type, modal, hook, and form.

Do not create:

```text
api.ts
```

with every endpoint.

Do not create:

```text
types.ts
```

with every feature type.

Do not scatter:

```ts
fetch("/api/...")
```

through components.

Do not repeat job polling in multiple pages.

---

## Frontend definition of done

Before completing a frontend feature:

1. inspect `frontend/src`,
2. confirm the feature has a clear home,
3. confirm API logic is not embedded throughout components,
4. confirm large components have focused responsibilities,
5. confirm loading/error/empty states exist,
6. confirm types are meaningful,
7. run lint,
8. run typecheck,
9. run tests where applicable,
10. run production build.

A feature is not finished merely because it renders correctly.
