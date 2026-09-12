output "project_id" {
  description = "Google Cloud project used by this environment."
  value       = var.project_id
}

output "region" {
  description = "Google Cloud region used by this environment."
  value       = var.region
}

output "zone" {
  description = "Google Cloud zone used by this environment."
  value       = var.zone
}

output "required_services" {
  description = "Google Cloud APIs managed by this environment."
  value       = sort(tolist(local.required_services))
}

output "database_vm_name" {
  description = "Name of the PostgreSQL Compute Engine VM."
  value       = try(google_compute_instance.database[0].name, null)
}

output "database_vm_internal_ip" {
  description = "Internal IPv4 address of the PostgreSQL VM."
  value       = try(google_compute_instance.database[0].network_interface[0].network_ip, null)
}

output "database_data_disk" {
  description = "Name of the protected PostgreSQL data disk."
  value       = try(google_compute_disk.database[0].name, null)
}

output "container_repository" {
  description = "Private Artifact Registry Docker repository used by the internal-only VM."
  value       = "${var.region}-docker.pkg.dev/${var.project_id}/${google_artifact_registry_repository.containers.repository_id}"
}

output "postgres_image" {
  description = "Immutable PostgreSQL image that the internal-only VM will run."
  value       = local.postgres_image
}

output "postgres_password_secret" {
  description = "Secret Manager secret that will contain the PostgreSQL password."
  value       = google_secret_manager_secret.postgres_password.secret_id
}

output "web_service_url" {
  description = "Public Cloud Run URL for the low-cost web service."
  value       = try(google_cloud_run_v2_service.web[0].uri, null)
}

output "compiler_service_url" {
  description = "Internal Cloud Run URL targeted by the CV compilation queue."
  value       = try(google_cloud_run_v2_service.compiler[0].uri, null)
}

output "pdf_artifact_bucket" {
  description = "Private bucket containing compiled CV PDFs."
  value       = try(google_storage_bucket.pdf_artifacts[0].name, null)
}

output "application_secret_ids" {
  description = "Secret containers that require operator-managed values before enabling the application runtime."
  value = merge(
    {
      google_auth_client_secret = google_secret_manager_secret.google_auth_client_secret.secret_id
      gmail_oauth_client_secret = google_secret_manager_secret.gmail_oauth_client_secret.secret_id
    },
    {
      for key, secret in google_secret_manager_secret.gmail_refresh_token :
      "gmail_refresh_token_${key}" => secret.secret_id
    },
  )
}

output "github_workload_identity_provider" {
  description = "Provider resource name used by google-github-actions/auth."
  value       = try(google_iam_workload_identity_pool_provider.github[0].name, null)
}

output "github_deployer_service_account" {
  description = "Keyless GitHub Actions deployment service account."
  value       = try(google_service_account.github_deployer[0].email, null)
}
