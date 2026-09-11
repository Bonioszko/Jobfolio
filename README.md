# Job Parser

Local-first .NET 10 and React implementation for parsing job-alert emails, tracking applications, tailoring versioned TeX CVs, and compiling private PDFs. The initial runtime uses PostgreSQL in Docker; cloud adapters are intentionally deferred.

## Start PostgreSQL

Copy `.env.example` to `.env` before the first run. If port `5432` is already
occupied, change both `POSTGRES_PORT` and the port in
`ConnectionStrings__Postgres` to the same free port. Docker Compose reads the
file automatically; export its values in each shell that runs a .NET process.

```bash
cp .env.example .env
set -a
source .env
set +a
docker compose up -d postgres
```

## Run the backend

```powershell
dotnet restore --configfile NuGet.Config
dotnet run --project src/App.Api
```

The API listens on the URL printed by ASP.NET Core. Create a demo session with `POST /api/auth/demo`, retaining the returned cookie, then call `GET /api/source-items`.

In separate terminals, start the asynchronous workers and frontend:

```powershell
dotnet run --project src/App.AiWorker
dotnet run --project src/App.CompilerWorker
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The Vite development proxy targets the API's HTTP launch profile at `http://localhost:5121`.

Real-user CV tailoring uses the locally installed Codex CLI in non-interactive mode and reuses its saved authentication. Verify it is available with `codex --version` and sign in with `codex login` when needed. The AI worker invokes Codex ephemerally in a read-only sandbox and requests schema-constrained TeX output. Public demo sessions continue to use the deterministic fake generator and never invoke Codex.

The worker configuration supports `CodexCli:ExecutablePath`, an optional `CodexCli:Model`, `CodexCli:TimeoutSeconds`, and `CodexCli:MaxOutputCharacters`. `CvWorkflow:RealUserGenerator` selects the persisted provider for newly requested real-user jobs; its current value is `local-codex`.

## Verify

```powershell
dotnet build JobParser.slnx
dotnet test JobParser.slnx
```

Tectonic must be installed on the compiler worker's `PATH`; compilation is deliberately never performed by `App.Api`.

## Current scope

Implemented: solution layering, PostgreSQL schema, generic domain configuration, LinkedIn/Just Join IT/No Fluff Jobs parser edges, isolated demo authentication/seeding, job list/detail/status APIs, source filtering, dated multi-stage interview notes, three base CV templates, immutable template/rule/job/document models, local Codex CV tailoring for real users, deterministic demo tailoring, TeX validation/compiler, and private local artifact storage.

The personal GCP profile runs the frontend and API in one scale-to-zero Cloud Run service,
keeps PostgreSQL on the internal-only Free Tier VM, and runs Gmail imports as scheduled Cloud
Run jobs. CV endpoints and UI can be disabled through `Features:CvEnabled`; the personal cloud
profile keeps them disabled until the OpenAI and compiler deployment is implemented.
