# Queues

Generation and compilation are separate persisted job types. An API request validates immutable references, persists a `Queued` job, signals only its ID, and returns `202 Accepted`. Workers transactionally claim queued or lease-expired work. Succeeded jobs are never processed again.

The local adapter uses PostgreSQL polling. The later GCP adapter will use separate Cloud Tasks queues whose payloads contain only `generationJobId` or `compileJobId`.
