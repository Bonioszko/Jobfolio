resource "google_secret_manager_secret" "postgres_password" {
  project   = var.project_id
  secret_id = "jobparser-postgres-password"
  labels    = local.common_labels

  replication {
    auto {}
  }

  depends_on = [google_project_service.required["secretmanager.googleapis.com"]]
}

resource "google_secret_manager_secret_iam_member" "database_vm_password_accessor" {
  project   = google_secret_manager_secret.postgres_password.project
  secret_id = google_secret_manager_secret.postgres_password.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.database_vm.email}"
}

resource "google_secret_manager_secret_iam_member" "operator_password_version_adder" {
  project   = google_secret_manager_secret.postgres_password.project
  secret_id = google_secret_manager_secret.postgres_password.secret_id
  role      = "roles/secretmanager.secretVersionAdder"
  member    = "user:${var.operator_email}"
}

resource "google_secret_manager_secret_iam_member" "operator_password_accessor" {
  project   = google_secret_manager_secret.postgres_password.project
  secret_id = google_secret_manager_secret.postgres_password.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "user:${var.operator_email}"
}
