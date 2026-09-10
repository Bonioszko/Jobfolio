# Personal Google Cloud environment

This Terraform root manages the low-cost personal Google Cloud deployment for
Job Parser. The database VM is internal-only: it never receives an external
IPv4 address.

Terraform state is stored in the private, versioned Google Cloud Storage bucket
`your-gcp-project-id-jobparser-tfstate`. The bucket is a bootstrap resource: it
must exist before `terraform init`, and Terraform does not manage or delete it.

## Local setup

Authenticate with Application Default Credentials:

```bash
gcloud auth application-default login
```

Create the ignored local variable file:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Replace `operator_email` with the Google account used by `gcloud`. Terraform
grants that account IAP tunnel access, OS Login administrator access, and
permission to use the database VM's service account while connecting.

Create the state bucket once before the first initialization:

```bash
gcloud storage buckets create gs://your-gcp-project-id-jobparser-tfstate \
  --project=your-gcp-project-id \
  --location=us-central1 \
  --default-storage-class=STANDARD \
  --uniform-bucket-level-access \
  --public-access-prevention
gcloud storage buckets update gs://your-gcp-project-id-jobparser-tfstate \
  --versioning
```

Then initialize and validate the configuration:

```bash
terraform init
terraform fmt -check -recursive
terraform validate
```

Do not run `terraform apply` until the first infrastructure slice has been
reviewed.

## Mirror the PostgreSQL image

The `e2-micro` VM uses `linux/amd64`. Preserve the upstream multi-platform
manifest when copying PostgreSQL to Artifact Registry; pulling and pushing the
image normally from an Apple Silicon Mac copies only `linux/arm64` and the
container then fails with `exec format error`.

```bash
gcloud auth configure-docker us-central1-docker.pkg.dev
docker buildx imagetools create \
  --tag us-central1-docker.pkg.dev/your-gcp-project-id/jobparser-containers/postgres:17-alpine \
  docker.io/library/postgres:17-alpine
docker buildx imagetools inspect \
  us-central1-docker.pkg.dev/your-gcp-project-id/jobparser-containers/postgres:17-alpine
```

Set `postgres_image_digest` to the resulting top-level index digest. The VM
bootstrap also requests `linux/amd64` explicitly so an incompatible
single-platform digest fails during the pull rather than repeatedly crashing
at container startup.

## Personal database VM

The first infrastructure slice creates:

- a custom regional VPC and `/24` subnet;
- Private Google Access on that subnet;
- an ingress firewall rule allowing SSH only from Google's IAP range;
- a dedicated VM service account with logging and monitoring writer roles;
- a private regional Artifact Registry Docker repository;
- repository permissions for the operator and VM service account.

The next secret-bootstrap slice creates a Secret Manager secret and IAM
permissions, but deliberately does not manage the secret value in Terraform.
This keeps the database password out of configuration, plans, and state.

`database_vm_enabled` defaults to `false`. This lets the private PostgreSQL 17
image be copied into Artifact Registry before the internal-only VM exists.

After the image is available, enabling the VM creates:

- a single-zone `e2-micro` VM with a 10 GiB standard boot disk;
- Container-Optimized OS, which includes the Docker runtime;
- a separate, deletion-protected 20 GiB standard PostgreSQL data disk;
- daily data-disk snapshots retained for seven days.

The VM bootstrap is idempotent across reboots. It formats the data disk only
when it has no filesystem, mounts it, obtains the password using the VM service
account, pulls the digest-pinned private image, and starts PostgreSQL. The
password is held in `/run` and is not placed in VM metadata or Terraform state.
Docker's credential-helper configuration is written under the writable
`/var/lib` state partition because the Container-Optimized OS root filesystem
is read-only.

PostgreSQL port 5432 is not allowed through the VPC firewall. Administration
uses OS Login over IAP. Container images are pulled privately from Artifact
Registry through Private Google Access. No Cloud NAT or external VM address is
required. The Docker port is bound to `127.0.0.1` on the VM, so database access
currently requires an IAP SSH tunnel even from within the VPC.

Start the tunnel from the local machine and keep the process running:

```bash
gcloud compute ssh jobparser-db \
  --project=your-gcp-project-id \
  --zone=us-central1-a \
  --tunnel-through-iap \
  -- -N -L 127.0.0.1:5433:127.0.0.1:5432
```

Connect the application or a database client to `127.0.0.1:5433` using database
and user `jobparser` plus the password that was added to Secret Manager. The
tunnel uses the normal local `gcloud` SSH key and may prompt for its passphrase.

Terraform state and real `.tfvars` files are intentionally excluded from Git.
Commit `.terraform.lock.hcl` so provider selections remain reproducible.
