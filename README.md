# Job Parser

Local-first .NET 10 and React implementation for parsing job-alert emails, tracking applications, tailoring versioned TeX CVs, and compiling private PDFs. The initial runtime uses PostgreSQL in Docker; cloud adapters are intentionally deferred.

## Start PostgreSQL

```powershell
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

## Verify

```powershell
dotnet build JobParser.slnx
dotnet test JobParser.slnx
```

Tectonic must be installed on the compiler worker's `PATH`; compilation is deliberately never performed by `App.Api`.

## Current scope

Implemented: solution layering, PostgreSQL schema, generic domain configuration, LinkedIn/Just Join IT/No Fluff Jobs parser edges, isolated demo authentication/seeding, job list/detail/status APIs, three base CV templates, immutable template/rule/job/document models, deterministic CV tailoring, TeX validation/compiler, and private local artifact storage.

Next phases: template/rule API, generation and compiler worker job orchestration, React dashboard, Google OIDC/Gmail adapters, OpenAI provider, production GCP/Terraform.
