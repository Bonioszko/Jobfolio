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
