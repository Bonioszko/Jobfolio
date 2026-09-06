# Migration Notes for the Existing Codebase

## Goal

Use these notes when refactoring code generated under the previous large `AGENTS.md`.

The goal is not to rewrite the entire repository at once.

Refactor incrementally while implementing real features.

---

## 1. Frontend monolith recovery

If the frontend currently looks similar to:

```text
frontend/src/
  App.tsx
  main.tsx
```

do not keep adding functionality to `App.tsx`.

First create:

```text
src/
  app/
  features/
  components/
  lib/
```

Move top-level app wiring into:

```text
app/App.tsx
```

and leave the Vite bootstrap in:

```text
main.tsx
```

Then extract one feature at a time.

Recommended first feature extraction:

```text
features/job-postings/
  api/
  components/
  hooks/
  pages/
  types/
```

Do not create every future feature folder in one empty scaffolding commit.

---

## 2. Extract API calls

Search React code for:

```text
fetch(
```

Move generic HTTP behavior to:

```text
lib/api/apiClient.ts
```

Move feature endpoint calls next to the feature.

Do this incrementally.

---

## 3. Extract feature types

If types are defined inside `App.tsx`, move them into feature-local type files.

Do not create one giant global `types.ts`.

---

## 4. Extract polling

If generation/compilation polling exists directly in page code or `App.tsx`, move it into a reusable hook or server-state abstraction.

Avoid separate copy-pasted polling loops.

---

## 5. Extract UI components

Split UI based on responsibility.

Good first extractions may include:

```text
JobPostingCard
JobPostingFilters
ApplicationStatusSelect
CvTemplateSelector
GenerationStatus
CompileStatus
PdfPreview
```

Do not split every `div` into a component.

---

## 6. Replace generic domain names where practical

If the old code uses speculative names such as:

```text
SourceItem
DocumentTemplate
GeneratedDocument
```

prefer clearer product names during touched-area refactors:

```text
JobPosting
CvTemplate
GeneratedCv
```

Do not perform a huge repository-wide rename unless it is safe and useful.

Prefer renaming when a feature is already being modified.

---

## 7. Preserve architectural seams

Keep external provider boundaries:

```text
IEmailProvider
IAiCvGenerator
ICvGenerationQueue
ICvCompilationQueue
IArtifactStorage
ITokenCipher
```

Do not remove useful boundaries merely because domain names are becoming more specific.

---

## 8. Backend large-service recovery

If one service currently handles many unrelated concerns, split during feature work.

For example, instead of:

```text
JobService
```

doing ingestion + status + generation + templates, move toward:

```text
JobPostingQueryService
ApplicationStatusService
CvGenerationRequestService
```

or feature/use-case classes.

Avoid a big-bang rewrite.

---

## 9. Commit rhythm

Make refactor commits focused.

Examples:

```text
refactor(frontend): create application and feature folders
refactor(frontend): extract job posting API client
refactor(frontend): extract job posting page components
refactor(frontend): centralize generation polling
refactor(domain): rename source item to job posting
```

Each commit should build where practical.

---

## 10. Refactor completion check

After touching an area, ask:

- Does the file still have multiple unrelated responsibilities?
- Are API calls embedded in presentation code?
- Is business behavior hidden in JSX?
- Are types placed with the owning feature?
- Is polling duplicated?
- Is authorization scoped by workspace?
- Did I preserve unrelated user changes?
- Did tests/build pass?

Do not claim the architecture is fixed merely because folders now exist.
