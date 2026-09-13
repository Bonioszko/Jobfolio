# Configuration

This repository keeps portable defaults and examples in Git while loading real
user, deployment, and secret values from sources that are not committed.

## Configuration ownership

| Value | Local development | Google Cloud | Commit to Git |
|---|---|---|---|
| Safe application defaults | `appsettings.json` | container image | yes |
| PostgreSQL connection | ignored `.env` | Cloud Run environment plus Secret Manager | no |
| Google sign-in client ID | .NET user secrets | Terraform/Cloud Run environment | no |
| Google sign-in client secret | .NET user secrets | Secret Manager | no |
| Allowed-user emails | .NET user secrets | ignored `terraform.tfvars` | no |
| Gmail OAuth client ID | .NET user secrets | ignored `terraform.tfvars` | no |
| Gmail OAuth secret and refresh tokens | .NET user secrets or an external client JSON file | Secret Manager | no |
| Terraform input values | not required | ignored `terraform.tfvars` | no |
| Terraform backend bucket | not required | ignored `backend.hcl` | no |
| PDF artifact storage | ignored local `.artifacts` | private Cloud Storage bucket | no |
| Compilation dispatch | PostgreSQL polling | Cloud Tasks | no |

ASP.NET Core loads environment variables after `appsettings.json`, so local or
Cloud Run values override the checked-in defaults. Terraform reads the ignored
`terraform.tfvars` automatically. Its GCS backend uses the ignored
`backend.hcl` supplied explicitly during initialization.

Public demo sessions use the checked-in `Demo` defaults unless overridden. The
session lifetime is six hours, and at most two PDF compilation requests are
accepted across all demo workspaces in a rolling 60-minute window. Override
`Demo__SessionLifetimeHours`, `Demo__MaxCompilationJobsPerWindow`, or
`Demo__CompilationWindowMinutes` to tune the portfolio deployment without a
code change. Demo CV generation is deterministic and never invokes the paid
generator.

Do not use `git update-index --skip-worktree` or `--assume-unchanged` for local
configuration. Those flags are local Git implementation details, not a secret
management boundary.

## Local development

Create the ignored environment file:

```bash
cp .env.example .env
```

The development PostgreSQL password in this file is only for the local Docker
container. If port 5432 is occupied, change both `POSTGRES_PORT` and the port in
`ConnectionStrings__Postgres`.

Start PostgreSQL and export the application values into the current shell:

```bash
docker compose up -d postgres
set -a
source .env
set +a
```

Configure real-user Google sign-in through .NET user secrets:

```bash
dotnet user-secrets --project src/App.Api set "Authentication:Google:Enabled" "true"
dotnet user-secrets --project src/App.Api set "Authentication:Google:ClientId" "YOUR_WEB_CLIENT_ID"
dotnet user-secrets --project src/App.Api set "Authentication:Google:ClientSecret" "YOUR_WEB_CLIENT_SECRET"
dotnet user-secrets --project src/App.Api set "Authentication:Google:AllowedUsers:0:Email" "YOUR_GOOGLE_EMAIL"
dotnet user-secrets --project src/App.Api set "Authentication:Google:AllowedUsers:0:WorkspaceId" "user-one"
```

Repeat the last two settings with index `1` and a different workspace ID for a
second real user. Workspace IDs are stable database ownership identifiers; do
not rename one after importing data.

For local Gmail synchronization, keep the downloaded Desktop OAuth JSON file
outside the repository and configure its absolute path:

```bash
dotnet user-secrets --project src/App.GmailSync set "Gmail:Enabled" "true"
dotnet user-secrets --project src/App.GmailSync set "Gmail:WorkspaceKey" "user:user-one"
dotnet user-secrets --project src/App.GmailSync set "Gmail:Labels:0" "Job alerts"
dotnet user-secrets --project src/App.GmailSync set "Gmail:OAuth:ClientSecretsPath" "/absolute/private/path/gmail-oauth-client.json"
```

The first Gmail worker run opens the consent flow. Its token store defaults to
the ignored `.appdata/gmail-token` directory.

Run the processes from shells in which `.env` was exported:

```bash
dotnet run --project src/App.Api
dotnet run --project src/App.AiWorker
dotnet run --project src/App.CompilerWorker
dotnet run --project src/App.GmailSync
```

The AI worker uses the separately authenticated Codex CLI. Its authentication
files remain outside this repository.

## Personal Google Cloud environment

Authenticate and select the Terraform directory:

```bash
gcloud auth application-default login
cd infrastructure/terraform/environments/personal
cp terraform.tfvars.example terraform.tfvars
cp backend.hcl.example backend.hcl
```

Edit `terraform.tfvars` with the real project ID, operator email, image digest,
application URL, OAuth client IDs, allowed users, and Gmail schedule. These
values are not all credentials, but they identify the private deployment and
therefore remain outside Git.

Edit `backend.hcl` with the private state bucket name. Backend files do
not support Terraform variable interpolation.

Export identifiers used by the documented `gcloud` commands:

```bash
export JOBPARSER_GCP_PROJECT_ID="your-gcp-project-id"
export JOBPARSER_GCP_REGION="us-central1"
export JOBPARSER_GCP_ZONE="us-central1-a"
export JOBPARSER_TF_STATE_BUCKET="your-gcp-project-id-jobparser-tfstate"
```

Create the private state bucket once:

```bash
gcloud storage buckets create "gs://${JOBPARSER_TF_STATE_BUCKET}" \
  --project="${JOBPARSER_GCP_PROJECT_ID}" \
  --location="${JOBPARSER_GCP_REGION}" \
  --default-storage-class=STANDARD \
  --uniform-bucket-level-access \
  --public-access-prevention
gcloud storage buckets update "gs://${JOBPARSER_TF_STATE_BUCKET}" --versioning
```

Initialize and review Terraform:

```bash
terraform init -backend-config=backend.hcl
terraform fmt -check -recursive
terraform validate
terraform plan
terraform apply
```

For an existing checkout that was initialized before `backend.hcl` was
introduced, reconfigure the same backend without moving state:

```bash
terraform init -reconfigure -backend-config=backend.hcl
```

The first apply creates Secret Manager containers but deliberately does not put
secret values in Terraform configuration, plans, or state. Add secret versions
from files located outside the repository:

```bash
gcloud secrets versions add jobparser-postgres-password \
  --project="${JOBPARSER_GCP_PROJECT_ID}" \
  --data-file=/absolute/private/path/postgres-password.txt

gcloud secrets versions add jobparser-google-auth-client-secret \
  --project="${JOBPARSER_GCP_PROJECT_ID}" \
  --data-file=/absolute/private/path/google-auth-client-secret.txt

gcloud secrets versions add jobparser-gmail-oauth-client-secret \
  --project="${JOBPARSER_GCP_PROJECT_ID}" \
  --data-file=/absolute/private/path/gmail-oauth-client-secret.txt
```

Each configured Gmail account also needs its corresponding
`jobparser-gmail-refresh-token-<account-key>` secret version. Never pass these
values through Terraform variables because Terraform would retain them in its
state.

Follow the staged infrastructure and image bootstrap procedure in
[`infrastructure/terraform/environments/personal/README.md`](../infrastructure/terraform/environments/personal/README.md).
Keep `application_runtime_enabled`, authentication, Gmail accounts, and
automated deployment disabled until their prerequisites exist.

## GitHub Actions deployment

The deployment uses Workload Identity Federation and does not require a Google
service-account JSON key. Configure these repository-level Actions variables:

```text
GCP_PROJECT_ID
GCP_REGION
GCP_ARTIFACT_REPOSITORY
GCP_WORKLOAD_IDENTITY_PROVIDER
GCP_DEPLOYER_SERVICE_ACCOUNT
GCP_DEPLOY_ENABLED
GCP_CV_COMPILATION_ENABLED
```

Use `us-central1` for `GCP_REGION` and `jobparser-containers` for
`GCP_ARTIFACT_REPOSITORY` when retaining the Terraform defaults. Obtain the
identity-provider and deployer-service-account values from:

```bash
terraform output -raw github_workload_identity_provider
terraform output -raw github_deployer_service_account
```

Keep `GCP_DEPLOY_ENABLED` unset or set to `false` during bootstrap. Set it to
`true` only after Terraform has created the runtime and deployment identity.
Set `GCP_CV_COMPILATION_ENABLED` to `true` only after Terraform has created the
compiler service and `cv_compilation_enabled` is true.

The deployment workflow changes the Cloud Run image revision only. Terraform
continues to own environment variables, Secret Manager references, networking,
and IAM. Consequently, pushing a new image does not replace real configuration
with the placeholders committed in `appsettings.json`.

## Updating configuration safely

- Edit `.env` or .NET user secrets for local application changes.
- Edit `terraform.tfvars`, then run `terraform plan` and `terraform apply`, for
  non-secret GCP configuration changes.
- Add a new Secret Manager version for a rotated secret.
- Do not put a secret into `terraform.tfvars`, a GitHub variable, a Docker build
  argument, or a tracked `appsettings` file.
- Do not copy real values back into an `.example` file.

Verify local files remain excluded before every public push:

```bash
git check-ignore -v .env
git check-ignore -v infrastructure/terraform/environments/personal/terraform.tfvars
git check-ignore -v infrastructure/terraform/environments/personal/backend.hcl
git status --short --ignored
```

The first three commands must report an ignore rule. Review every non-ignored
path reported by `git status` before staging it.

## Publishing the anonymized repository

Prefer creating a new, empty GitHub repository rather than changing the
visibility of a repository that already contains the old history. Do not ask
GitHub to initialize it with a README, license, or `.gitignore`.

Add the new repository and push only `main`:

```bash
git remote add origin git@github.com:YOUR_PUBLIC_ACCOUNT/Job-ParserNet.git
git push -u origin main
```

Do not use `git push --mirror` or push recovery refs, bundles, local backup
branches, or ignored files. A repository that previously received the private
history may retain unreachable objects, pull-request references, caches, or
forks. For strict anonymization, publish to a newly created repository and keep
the old repository private or delete it separately.
