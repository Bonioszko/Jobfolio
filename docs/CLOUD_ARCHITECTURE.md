# Future GCP Architecture

## Status

This document describes the intended future production deployment.

Do not implement these resources prematurely during early local development.

The local architecture should preserve seams that make this migration straightforward.

---

## Target services

Planned GCP services:

- Cloud Run
- managed PostgreSQL
- Google Cloud Storage
- Google Cloud Tasks
- Google Cloud Scheduler
- Secret Manager
- Cloud KMS
- Artifact Registry

Terraform should represent production infrastructure.

---

## Service layout

Potential deployment:

```text
App.Api
  → public/authenticated Cloud Run service

App.AiWorker
  → private Cloud Run service

App.CompilerWorker
  → private Cloud Run service

App.GmailSync
  → Cloud Run Job

App.DemoCleanup
  → Cloud Run Job
```

---

## Queues

```text
cv-generation queue
→ authenticated invocation
→ App.AiWorker
```

```text
cv-compilation queue
→ authenticated invocation
→ App.CompilerWorker
```

Queue payloads contain job IDs only.

---

## Scheduling

Cloud Scheduler may trigger:

- Gmail synchronization,
- demo cleanup.

Initial Gmail sync cadence may be around 10–15 minutes.

---

## PostgreSQL

Use managed PostgreSQL.

Persist all durable business/job state there.

Queue delivery must not become the system of record.

---

## Artifact storage

Use a private Cloud Storage bucket.

Example logical paths:

Real:

```text
users/{userId}/cvs/{cvId}/versions/{versionId}/{compileJobId}.pdf
```

Demo:

```text
demo/{sessionId}/cvs/{cvId}/versions/{versionId}/{compileJobId}.pdf
```

Never make the bucket public.

---

## Secrets

Store secrets in Secret Manager.

Use KMS for Gmail refresh-token encryption.

Use least privilege.

---

## Service accounts

Separate identities should exist where privilege boundaries differ.

Example categories:

```text
web
gmail-sync
ai-worker
compiler-worker
demo-cleanup
generation-task-invoker
compilation-task-invoker
```

Exact names are deployment details.

The compiler must not receive Gmail or OpenAI credentials.

The Gmail worker must not receive OpenAI credentials.

The AI worker must not receive Gmail token-decryption permission unless a concrete requirement changes the architecture.

---

## Terraform

Terraform should eventually cover:

- Artifact Registry,
- Cloud Run services/jobs,
- managed PostgreSQL,
- private storage bucket,
- Scheduler jobs,
- Tasks queues,
- KMS,
- Secret Manager,
- service accounts,
- IAM.

Avoid undocumented manual production-only configuration.

---

## Migration principle

Application code should depend on abstractions such as:

```text
ICvGenerationQueue
ICvCompilationQueue
IArtifactStorage
ITokenCipher
```

Local adapters and GCP adapters may differ.

Do not force cloud SDK types into application/domain code merely to make future deployment easier.
