# Async Jobs and Queues

## Why queues exist

OpenAI generation and TeX compilation are:

- potentially slow,
- retryable,
- expensive or resource-intensive,
- poor fits for a browser-held HTTP request.

Both operations must use persistent jobs and asynchronous workers.

---

## HTTP flow

```text
HTTP request
  ↓
authenticate + authorize
  ↓
validate
  ↓
create PostgreSQL job
  ↓
enqueue job ID
  ↓
202 Accepted
```

Do not enqueue before the persistent job exists.

---

## Separate queues

Use separate logical queues:

```text
cv-generation
cv-compilation
```

They differ in:

- concurrency,
- retry behavior,
- security,
- resource usage,
- cost.

Do not combine them.

---

## Queue payloads

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

Never put:

- prompts,
- TeX,
- candidate rules,
- email bodies,
- API keys,
- OAuth tokens,
- user secrets

in queue payloads.

---

## PostgreSQL responsibility

PostgreSQL stores:

- job identity,
- workspace owner,
- exact immutable input references,
- state,
- attempt count,
- lease,
- timestamps,
- output reference,
- safe error information.

Queue delivery is not the business-state database.

---

## Local queue adapter

For local development, dedicated workers may poll PostgreSQL.

Requirements:

- transactional claim,
- lease,
- duplicate-safe processing,
- backoff where reasonable.

Do not add RabbitMQ/Redis merely to emulate future infrastructure.

---

## Future queue adapter

Future GCP deployment may use Google Cloud Tasks.

Application code continues to depend on:

```text
ICvGenerationQueue
ICvCompilationQueue
```

rather than Cloud Tasks SDK types.

---

## Job states

Generation:

```text
QUEUED
RUNNING
SUCCEEDED
FAILED
```

Compilation:

```text
QUEUED
RUNNING
SUCCEEDED
FAILED
TIMED_OUT
```

Cancellation is not required for MVP.

---

## Leases

A worker can crash while a job is `RUNNING`.

Track:

```text
leaseUntil
attemptCount
```

Processing flow:

1. load candidate job,
2. transactionally attempt to claim,
3. verify state/lease,
4. perform expensive work only after successful claim.

A running job may be reclaimed only after lease expiry.

A succeeded job must never execute again.

---

## Idempotency

Assume duplicate delivery can occur.

Workers must be safe when the same job ID arrives more than once.

Do not rely solely on queue-level deduplication.

---

## Retry classification

Transient examples:

- temporary network failure,
- OpenAI rate limit,
- temporary OpenAI service failure,
- temporary PostgreSQL issue,
- temporary object-storage issue.

Permanent examples:

- referenced version missing,
- invalid application state,
- unsafe TeX,
- unsupported TeX package,
- deterministic TeX syntax failure after allowed repair policy,
- invalid structured AI result after allowed attempts.

Permanent failures should be persisted as failed and not retried forever.

Transient failures should preserve retry-safe state.

---

## AI worker

AI worker responsibilities:

1. receive job ID,
2. authenticate invocation where applicable,
3. claim job,
4. load exact immutable inputs,
5. select real/demo generator,
6. generate,
7. validate structured output,
8. create immutable generated-CV version,
9. mark job succeeded.

Public users must not invoke a production worker directly.

---

## Demo generator

Demo sessions use a deterministic fake generator.

It still goes through:

```text
CvGenerationJob
→ generation queue
→ AI worker
→ GeneratedCvVersion
```

Do not bypass the job architecture in demo mode.

---

## Compiler worker

Compiler worker responsibilities:

1. authenticate invocation,
2. claim compile job,
3. load exact TeX version,
4. run static safety checks,
5. create isolated temp directory,
6. invoke Tectonic,
7. persist/upload PDF and safe log metadata,
8. create artifact record,
9. mark terminal state,
10. clean temporary files.

Use low concurrency.

---

## Compilation timeout

Use configurable timeout.

An initial range such as 20–30 seconds is reasonable.

On timeout:

- terminate the process,
- mark `TIMED_OUT`,
- clean temp files,
- do not blindly retry pathological deterministic input.

---

## Frontend polling

The frontend polls active job endpoints.

A 1–2 second interval is reasonable for local/MVP UX.

Stop polling on terminal state.

Centralize polling behavior.

WebSockets are not needed for MVP.

---

## Queue tests

Cover:

- job persisted before enqueue,
- payload contains ID only,
- duplicate delivery,
- active lease,
- expired lease,
- already-succeeded job,
- transient retry,
- permanent failure,
- worker crash/reclaim behavior.
