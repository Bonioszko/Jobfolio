locals {
  required_services = toset([
    "artifactregistry.googleapis.com",
    "cloudresourcemanager.googleapis.com",
    "compute.googleapis.com",
    "iam.googleapis.com",
    "iap.googleapis.com",
    "logging.googleapis.com",
    "monitoring.googleapis.com",
    "oslogin.googleapis.com",
    "secretmanager.googleapis.com",
    "storage.googleapis.com",
  ])

  common_labels = merge(
    {
      application = "job-parser"
      environment = "personal"
      managed-by  = "terraform"
    },
    var.labels,
  )

  postgres_image = "${var.region}-docker.pkg.dev/${var.project_id}/${google_artifact_registry_repository.containers.repository_id}/postgres@${var.postgres_image_digest}"
}
