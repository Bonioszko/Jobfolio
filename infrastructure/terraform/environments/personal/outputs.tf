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
