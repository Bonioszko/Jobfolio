locals {
  required_services = toset([
    "artifactregistry.googleapis.com",
    "cloudscheduler.googleapis.com",
    "cloudtasks.googleapis.com",
    "cloudresourcemanager.googleapis.com",
    "iamcredentials.googleapis.com",
    "compute.googleapis.com",
    "gmail.googleapis.com",
    "iam.googleapis.com",
    "iap.googleapis.com",
    "logging.googleapis.com",
    "monitoring.googleapis.com",
    "networkmanagement.googleapis.com",
    "oslogin.googleapis.com",
    "run.googleapis.com",
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

  enabled_gmail_sync_accounts = {
    for key, account in var.gmail_sync_accounts : key => account
    if account.enabled
  }

  allowed_user_entries = [
    for email, workspace_id in var.allowed_users : {
      email        = email
      workspace_id = workspace_id
    }
  ]

  allowed_emails_by_workspace = {
    for email, workspace_id in var.allowed_users : workspace_id => email...
  }

  cv_compilation_runtime_enabled = var.application_runtime_enabled && var.cv_compilation_enabled
}
