# Demo Mode

## Purpose

The public demo lets visitors experience the application without:

- Google login,
- Gmail access,
- real OpenAI cost.

It must remain isolated from real-user data and from other demo visitors.

---

## Session model

When a visitor chooses `Try Demo`, create a random demo session ID.

Conceptually:

```text
visitor A → demo session A
visitor B → demo session B
visitor C → demo session C
```

Never use one shared mutable demo account.

---

## Isolation

Every demo-owned resource must be scoped to the current session.

Demo data must not overlap real-user data.

Repository/service APIs should require a workspace/session identity.

Server-side authorization is mandatory.

---

## Demo capabilities

Recommended:

| Capability | Real | Demo |
|---|---:|---:|
| Connect Gmail | yes | no |
| Gmail sync | yes | no |
| Browse jobs | yes | yes |
| Change workflow status | yes | yes |
| Edit templates | yes | yes |
| Edit candidate rules | yes | yes |
| Real OpenAI generation | yes | no |
| Deterministic demo generation | no | yes |
| TeX compilation | yes | yes |
| PDF preview/download | yes | yes |

The frontend may hide unavailable actions, but the backend must enforce restrictions.

---

## Demo data

Seed fake email fixtures.

Preferred flow:

```text
fake email fixture
→ real parser registry
→ normalized job posting
→ demo workspace
```

This proves actual parser code works.

---

## Demo AI

Use a deterministic `DemoCvGenerator`.

Do not grant public visitors real OpenAI access.

The demo generator still participates in normal job infrastructure:

```text
CvGenerationJob
→ queue
→ worker
→ GeneratedCvVersion
```

---

## Demo compilation

Using the real Tectonic compilation pipeline is acceptable and useful for demonstrating architecture.

Apply a stricter server-enforced quota. The default permits two compilation
requests across all demo sessions in a rolling 60-minute window. Both template
previews and generated-CV PDFs count toward the same allowance; completed and
failed requests remain counted because both may consume compiler capacity.

The limit is configurable through:

```text
Demo:MaxCompilationJobsPerWindow
Demo:CompilationWindowMinutes
```

Real-user workspaces do not consume or inherit this public-demo quota. PostgreSQL
advisory locking serializes the shared check across API instances.

---

## Lifetime

Demo sessions are temporary.

An expiry such as several hours is appropriate. The default is six hours and is
configured through `Demo:SessionLifetimeHours`.

Persist:

```text
createdAt
expiresAt
```

Cleanup must remove:

- expired database rows,
- demo PDFs,
- demo compiler logs,
- other demo artifacts.

Cleanup must be idempotent.

---

## Security tests

Test:

- demo A cannot read demo B,
- demo cannot read real-user data,
- real user cannot accidentally resolve demo-only resources,
- demo cannot connect Gmail,
- demo cannot invoke real OpenAI,
- expired sessions are rejected/cleaned.
