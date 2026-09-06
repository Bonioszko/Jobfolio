# Architecture

## System goal

The product imports job-alert emails and helps a user turn a selected job posting into a tailored, versioned CV.

The core flow is:

```text
JOB-ALERT EMAIL
  ↓
PROVIDER-SPECIFIC PARSER
  ↓
JOB POSTING
  ↓
APPLICATION WORKFLOW
  ↓
IMMUTABLE CANDIDATE RULES + CV TEMPLATE + JOB SNAPSHOT
  ↓
CV GENERATION JOB
  ↓
GENERATION QUEUE
  ↓
AI WORKER
  ↓
VERSIONED GENERATED TEX
  ↓
COMPILE JOB
  ↓
COMPILATION QUEUE
  ↓
ISOLATED TECTONIC WORKER
  ↓
PRIVATE PDF
```

---

## Architectural principles

### Use domain language

The application is intentionally a job-posting/CV application.

Use domain-specific concepts rather than generic wrappers for hypothetical future products.

### Keep infrastructure at the edges

Core business behavior must not depend directly on provider SDKs.

External boundaries include:

- Gmail,
- OpenAI,
- queue delivery,
- object storage,
- token encryption,
- Tectonic execution.

### Persist business state independently of delivery

PostgreSQL stores durable job state.

Queue adapters deliver work.

A queue is not the source of truth for job state.

### Make generation inputs immutable

A generation job must reference exact versions/snapshots of:

- candidate rules,
- CV template,
- job posting,
- system rules.

Historical generation must remain reproducible in terms of inputs even after current rules/templates change.

### Explicit workflow

CV generation or compilation must not automatically change the user's application workflow status.

Application status changes are explicit user actions.

---

## Main bounded features

### Job ingestion

- Gmail synchronization for real users
- fake email fixtures for demo
- provider-specific parsers
- normalized job posting persistence

### Application workflow

User-managed statuses such as:

- No Action
- To Apply
- Applied
- Skip

Exact labels/codes come from domain configuration.

### CV templates

Three initial example TeX templates.

Users clone/edit their own versions.

### Candidate rules

Versioned Markdown describing verified candidate experience, constraints, and tailoring preferences.

### CV generation

Creates a persistent job, then asynchronous AI work.

### CV editing/versioning

AI output and user edits create immutable versions.

### Compilation

Selected TeX version is compiled in an isolated Tectonic worker.

### Artifacts

PDFs remain private and are authorized per workspace.

---

## User model

There are:

- two allowlisted real users,
- isolated demo sessions.

The design intentionally does not include general registration, organizations, teams, roles, or billing.

---

## Domain configuration

`config/domain.json` controls presentation/configurable workflow concepts such as:

- job field definitions,
- dashboard-visible fields,
- application statuses.

The backend is authoritative.

The frontend consumes the backend-provided configuration instead of duplicating status definitions.

---

## Local-first implementation

The first version should run locally with:

- ASP.NET Core services,
- React frontend,
- PostgreSQL in Docker,
- local private artifact storage,
- PostgreSQL-backed background worker polling.

Cloud-specific code should remain behind adapters.

---

## Future cloud migration

The target architecture is documented in `CLOUD_ARCHITECTURE.md`.

Do not implement cloud complexity before the relevant phase requires it.

---

## Non-goals

Unless explicitly requested, do not implement:

- general registration,
- admin dashboard,
- organizations,
- teams,
- subscriptions,
- billing,
- automatic job applications,
- AI email parsing,
- autonomous agents,
- RAG,
- embeddings,
- vector databases,
- WebSockets,
- collaborative editing,
- infinite AI repair loops.
