# Security

## Security model

The application has a small user count but still handles:

- Gmail OAuth tokens,
- OpenAI access,
- private CV data,
- generated TeX,
- PDF artifacts,
- a public demo entry point.

Small scale is not a reason to weaken security boundaries.

---

## Real-user authentication

Use Google OpenID Connect.

After identity validation, require the email to exist in a configured allowlist.

Use secure server-side cookie sessions.

Production cookie expectations:

```text
Secure
HttpOnly
SameSite=Lax
```

Do not store authentication tokens in:

- localStorage,
- sessionStorage.

---

## Gmail authorization

Application login and Gmail mailbox authorization are separate concerns.

Gmail refresh tokens:

- never go to React,
- never go to OpenAI,
- never go to compiler workers,
- never go to logs,
- must be encrypted before database persistence.

Future cloud deployment uses KMS-backed encryption.

Local development may use a development-only encryption adapter with secrets outside source control.

---

## Workspace authorization

Every resource access must be scoped to the active workspace.

Real user:

```text
resource.workspaceId == currentWorkspace.workspaceId
```

Demo:

```text
resource.demoSessionId == currentWorkspace.demoSessionId
```

Never authorize by resource ID alone.

Prefer returning `404` when exposing resource existence would leak cross-workspace information.

---

## IDOR tests

Explicitly test:

- real user A cannot access real user B,
- real user cannot access demo data,
- demo cannot access real-user data,
- demo session A cannot access demo session B.

Cover:

- job posting,
- CV template,
- candidate rules,
- generation job,
- generated CV,
- compile job,
- PDF artifact,
- Gmail configuration.

---

## Public demo

Never use one shared mutable public demo workspace.

Each visitor gets a random isolated session.

Server-side authorization enforces demo restrictions.

Do not rely only on hidden frontend buttons.

Demo must not have:

- Gmail authorization,
- real Gmail sync,
- real OpenAI access,
- access to other sessions.

---

## Prompt injection boundary

Job descriptions and email content are untrusted data.

Imported text cannot redefine system instructions.

Prompt priority:

```text
1. hard-coded security instructions
2. repository system rules
3. candidate rules
4. selected CV template
5. job snapshot
6. optional user instruction
```

Never send secrets to the model.

Prefer normalized job posting data over raw email bodies.

---

## TeX security

Treat every TeX document as hostile.

Sources include:

- repository example templates,
- user templates,
- AI output,
- demo-generated output,
- user edits.

Static checks may reject dangerous/unwanted constructs such as:

```text
\write18
shell escape
../
absolute paths
unexpected \input
unexpected \include
```

Static validation is defense in depth, not the primary security boundary.

---

## Tectonic isolation

Compile in untrusted/offline mode.

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

Never enable shell escape.

Bake required dependencies into the compiler image.

Do not download arbitrary TeX packages during compilation.

---

## Compiler worker permissions

Compiler process/container must:

- run as non-root,
- use ephemeral working directories,
- enforce input-size limits,
- enforce timeouts,
- process low concurrency,
- delete temp files in `finally`.

It must not receive:

- Gmail credentials,
- OAuth refresh tokens,
- OpenAI API keys.

Grant only the minimum database/artifact permissions required.

---

## PDF storage

PDFs are private.

Never make the artifact bucket public.

Download/preview flow:

1. authenticate,
2. resolve workspace,
3. authorize artifact,
4. create short-lived access,
5. return/redirect.

Never expose permanent public object URLs.

---

## Logging

Use structured logs.

Useful fields:

```text
correlationId
workspaceId
jobId
jobPostingId
workerType
duration
status
```

Never log:

- Gmail access/refresh tokens,
- OpenAI API keys,
- OAuth client secrets,
- complete real email bodies,
- complete prompts,
- complete candidate rule documents,
- complete generated TeX.

Prefer identifiers, hashes, counts, durations, and statuses.

---

## Audit events

Record meaningful actions such as:

```text
GMAIL_CONNECTED
GMAIL_DISCONNECTED
JOB_POSTING_CREATED
APPLICATION_STATUS_CHANGED
CV_TEMPLATE_CREATED
CV_TEMPLATE_UPDATED
CANDIDATE_RULES_UPDATED
CV_GENERATION_REQUESTED
CV_GENERATION_COMPLETED
CV_GENERATION_FAILED
CV_COMPILE_REQUESTED
CV_COMPILE_COMPLETED
CV_COMPILE_FAILED
PDF_DOWNLOADED
```

Do not put large sensitive payloads in audit metadata.

---

## Rate limits

Use per-user/per-session quotas for cost-sensitive actions.

Demo mode requires especially strict limits.

Also control worker/queue concurrency.

Exact quotas are configuration, not hard-coded architectural truth.
