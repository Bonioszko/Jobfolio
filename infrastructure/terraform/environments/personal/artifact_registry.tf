resource "google_artifact_registry_repository" "containers" {
  project       = var.project_id
  location      = var.region
  repository_id = "jobparser-containers"
  description   = "Private container images for the personal Job Parser environment"
  format        = "DOCKER"
  mode          = "STANDARD_REPOSITORY"
  labels        = local.common_labels

  depends_on = [google_project_service.required["artifactregistry.googleapis.com"]]
}

resource "google_artifact_registry_repository_iam_member" "database_vm_reader" {
  project    = google_artifact_registry_repository.containers.project
  location   = google_artifact_registry_repository.containers.location
  repository = google_artifact_registry_repository.containers.name
  role       = "roles/artifactregistry.reader"
  member     = "serviceAccount:${google_service_account.database_vm.email}"
}

resource "google_artifact_registry_repository_iam_member" "operator_writer" {
  project    = google_artifact_registry_repository.containers.project
  location   = google_artifact_registry_repository.containers.location
  repository = google_artifact_registry_repository.containers.name
  role       = "roles/artifactregistry.writer"
  member     = "user:${var.operator_email}"
}
