resource "google_service_account" "web" {
  project      = var.project_id
  account_id   = "jobparser-web"
  display_name = "Job Parser web runtime"

  depends_on = [google_project_service.required["iam.googleapis.com"]]
}

resource "google_service_account" "gmail_sync" {
  project      = var.project_id
  account_id   = "jobparser-gmail-sync"
  display_name = "Job Parser Gmail sync jobs"

  depends_on = [google_project_service.required["iam.googleapis.com"]]
}

resource "google_service_account" "database_migrator" {
  project      = var.project_id
  account_id   = "jobparser-db-migrator"
  display_name = "Job Parser database migrator"

  depends_on = [google_project_service.required["iam.googleapis.com"]]
}

resource "google_service_account" "scheduler" {
  project      = var.project_id
  account_id   = "jobparser-scheduler"
  display_name = "Job Parser scheduler invoker"

  depends_on = [google_project_service.required["iam.googleapis.com"]]
}

resource "google_secret_manager_secret_iam_member" "web_database_password" {
  project   = var.project_id
  secret_id = google_secret_manager_secret.postgres_password.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.web.email}"
}

resource "google_secret_manager_secret_iam_member" "web_auth_client_secret" {
  project   = var.project_id
  secret_id = google_secret_manager_secret.google_auth_client_secret.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.web.email}"
}

resource "google_secret_manager_secret_iam_member" "migrator_database_password" {
  project   = var.project_id
  secret_id = google_secret_manager_secret.postgres_password.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.database_migrator.email}"
}

resource "google_secret_manager_secret_iam_member" "gmail_database_password" {
  project   = var.project_id
  secret_id = google_secret_manager_secret.postgres_password.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.gmail_sync.email}"
}

resource "google_secret_manager_secret_iam_member" "gmail_client_secret" {
  project   = var.project_id
  secret_id = google_secret_manager_secret.gmail_oauth_client_secret.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.gmail_sync.email}"
}

resource "google_secret_manager_secret_iam_member" "gmail_refresh_token" {
  for_each = google_secret_manager_secret.gmail_refresh_token

  project   = var.project_id
  secret_id = each.value.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.gmail_sync.email}"
}
