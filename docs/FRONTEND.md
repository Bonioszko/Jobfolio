# Frontend architecture

The frontend is a React and TypeScript application organized by product feature. The entry point
only mounts the app; top-level application composition belongs in `src/app`.

## Dependency direction

Feature pages compose feature hooks and components. Components do not construct API URLs or call
`fetch` directly. Feature API modules use the shared HTTP client in `src/lib/api`, while feature
types stay with the feature that owns them.

```text
main.tsx
  -> app/App.tsx
       -> feature pages and components
            -> feature hooks
                 -> feature API modules
                      -> lib/api/httpClient.ts
```

Cross-feature UI composition is allowed at page boundaries. Shared infrastructure must remain
domain-neutral; domain-specific behavior belongs in a feature.

## Current features

- `auth`: discovers the active authenticated session.
- `demo`: creates an isolated demo session and owns the public landing experience.
- `domain-config`: loads configurable labels, fields, and workflow statuses.
- `job-postings`: lists, inspects, and updates imported job postings.
- `cv-templates` and `candidate-rules`: load immutable generation inputs.
- `cv-generation`: starts and tracks asynchronous CV generation.
- `generated-cvs`: presents generated TeX and the compiled result.
- `cv-compilation`: starts and tracks asynchronous TeX compilation.

The reusable asynchronous polling implementation lives in `src/lib/api/asyncJob.ts`. Generation
and compilation keep their own request and presentation semantics while sharing cancellation-safe
polling.

## Adding behavior

Add endpoint calls to the owning feature's `api` directory, server-state behavior to its `hooks`
directory, API shapes to `types`, and rendering to focused `components` or `pages`. Add a shared
module only when at least two features need a genuinely domain-neutral capability.

Async features must explicitly render loading, empty, failure, success, and mutation-in-progress
states. Long-running generation and compilation requests remain job-based and cancellable when a
component unmounts.
