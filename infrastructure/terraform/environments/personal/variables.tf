variable "project_id" {
  description = "Google Cloud project that owns the personal Job Parser environment."
  type        = string

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{4,28}[a-z0-9]$", var.project_id))
    error_message = "project_id must be a valid Google Cloud project ID."
  }
}

variable "region" {
  description = "Google Cloud region for all regional resources."
  type        = string
  default     = "us-central1"
}

variable "zone" {
  description = "Google Cloud zone for the single-zone personal database VM."
  type        = string
  default     = "us-central1-a"
}

variable "operator_email" {
  description = "Google account allowed to administer the VM through OS Login and IAP."
  type        = string

  validation {
    condition     = can(regex("^[^@[:space:]]+@[^@[:space:]]+\\.[^@[:space:]]+$", var.operator_email))
    error_message = "operator_email must be a valid email address."
  }
}

variable "database_vm_enabled" {
  description = "Creates the database VM only after the private PostgreSQL image is available."
  type        = bool
  default     = false
}

variable "postgres_image_digest" {
  description = "Immutable digest of the mirrored PostgreSQL 17 image in Artifact Registry."
  type        = string
  default     = "sha256:18cfe3ef5e6815560c98237d6216d1e5119702fb0f3894c8785dd58b8bbe5d73"

  validation {
    condition     = can(regex("^sha256:[0-9a-f]{64}$", var.postgres_image_digest))
    error_message = "postgres_image_digest must be a sha256 digest."
  }
}

variable "vm_machine_type" {
  description = "Compute Engine machine type used for the PostgreSQL VM."
  type        = string
  default     = "e2-micro"
}

variable "boot_disk_size_gb" {
  description = "Size of the VM boot disk in GiB."
  type        = number
  default     = 10
}

variable "database_disk_size_gb" {
  description = "Size of the protected PostgreSQL data disk in GiB."
  type        = number
  default     = 20
}

variable "labels" {
  description = "Additional labels applied to supported resources."
  type        = map(string)
  default     = {}
}

variable "application_runtime_enabled" {
  description = "Creates the low-cost Cloud Run web service and supporting jobs after the application image and secrets are ready."
  type        = bool
  default     = false
}

variable "application_image" {
  description = "Immutable linux/amd64 Job Parser image reference in Artifact Registry, including its sha256 digest."
  type        = string
  default     = ""

  validation {
    condition     = var.application_image == "" || can(regex("^[a-z0-9.-]+/[a-z0-9._/-]+@sha256:[0-9a-f]{64}$", var.application_image))
    error_message = "application_image must be empty or an Artifact Registry image pinned by sha256 digest."
  }
}

variable "cv_compilation_enabled" {
  description = "Enables CV APIs, Cloud Tasks dispatch, private PDF storage, and the scale-to-zero compiler service."
  type        = bool
  default     = false
}

variable "compiler_image" {
  description = "Immutable linux/amd64 compiler-worker image reference in Artifact Registry, including its sha256 digest."
  type        = string
  default     = ""

  validation {
    condition     = var.compiler_image == "" || can(regex("^[a-z0-9.-]+/[a-z0-9._/-]+@sha256:[0-9a-f]{64}$", var.compiler_image))
    error_message = "compiler_image must be empty or an Artifact Registry image pinned by sha256 digest."
  }
}

variable "application_base_url" {
  description = "Public HTTPS URL of the Cloud Run web service, used as the post-login redirect."
  type        = string
  default     = ""

  validation {
    condition     = var.application_base_url == "" || can(regex("^https://[^/]+/?$", var.application_base_url))
    error_message = "application_base_url must be empty or an HTTPS origin without a path."
  }
}

variable "authentication_enabled" {
  description = "Enables Google sign-in after the web OAuth client and allowed users are configured."
  type        = bool
  default     = false
}

variable "authentication_google_client_id" {
  description = "Google Web OAuth client ID used to sign in to the application."
  type        = string
  default     = ""
}

variable "allowed_users" {
  description = "Map of allowlisted Google account emails to stable application workspace IDs."
  type        = map(string)
  default     = {}

  validation {
    condition = (
      length(var.allowed_users) <= 10 &&
      length(distinct(values(var.allowed_users))) == length(var.allowed_users) &&
      alltrue([
        for email, workspace_id in var.allowed_users :
        can(regex("^[^@[:space:]]+@[^@[:space:]]+\\.[^@[:space:]]+$", email)) &&
        can(regex("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", workspace_id))
      ])
    )
    error_message = "allowed_users must contain at most ten valid emails mapped to unique, valid workspace IDs."
  }
}

variable "gmail_oauth_client_id" {
  description = "Desktop OAuth client ID used by the scheduled Gmail synchronization jobs."
  type        = string
  default     = ""
}

variable "gmail_sync_accounts" {
  description = "Scheduled Gmail imports keyed by a short stable account name. Secret values are added separately."
  type = map(object({
    workspace_id = string
    labels       = list(string)
    schedule     = optional(string, "*/30 * * * *")
    time_zone    = optional(string, "Europe/Warsaw")
    enabled      = optional(bool, true)
  }))
  default = {}

  validation {
    condition = (
      length(var.gmail_sync_accounts) <= 2 &&
      length(distinct([for account in values(var.gmail_sync_accounts) : account.workspace_id])) == length(var.gmail_sync_accounts) &&
      alltrue([
        for key, account in var.gmail_sync_accounts :
        can(regex("^[a-z0-9][a-z0-9-]{0,28}[a-z0-9]$", key)) &&
        can(regex("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", account.workspace_id)) &&
        length(account.labels) > 0 && length(account.labels) <= 20 &&
        length(distinct(account.labels)) == length(account.labels)
      ])
    )
    error_message = "gmail_sync_accounts supports at most two valid account definitions with unique workspaces and at least one label each."
  }
}

variable "github_repository" {
  description = "Optional GitHub repository in owner/name form. When set, creates keyless deployment identity federation."
  type        = string
  default     = ""

  validation {
    condition     = var.github_repository == "" || can(regex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$", var.github_repository))
    error_message = "github_repository must be empty or use owner/name form."
  }
}
