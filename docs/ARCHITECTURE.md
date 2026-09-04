# Architecture

The reusable pipeline is `Job-alert email -> provider SourceParser -> normalized SourceItem -> GenerationJob -> tailored CV version -> CompileJob -> PdfArtifact`.

Projects preserve inward dependencies: Domain has no infrastructure dependencies; Application owns contracts; Infrastructure supplies adapters; API and workers are process entry points.

## Local persistence decision

The first version uses PostgreSQL 17 through Docker and EF Core only inside `App.Infrastructure`, as explicitly requested. `WorkspaceKey` is mandatory on every owned record and all resource queries require it. This replaces the plan's Firestore adapter locally without changing domain and application contracts. A cloud migration should replace infrastructure repositories/job delivery, not the domain pipeline.

Long-running work remains outside browser requests. PostgreSQL-backed queued records are the local source of truth; dedicated worker processes claim jobs with leases. Production can replace delivery with Cloud Tasks while keeping job records authoritative.
