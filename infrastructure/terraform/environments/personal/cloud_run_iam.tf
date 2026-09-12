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

resource "google_service_account" "compiler" {
  project      = var.project_id
  account_id   = "jobparser-compiler"
  display_name = "Job Parser CV compiler"

  depends_on = [google_project_service.required["iam.googleapis.com"]]
}

resource "google_service_account" "compilation_task_invoker" {
  project      = var.project_id
  account_id   = "jobparser-compile-invoker"
  display_name = "Job Parser compilation task invoker"

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

resource "google_secret_manager_secret_iam_member" "compiler_database_password" {
  project   = var.project_id
  secret_id = google_secret_manager_secret.postgres_password.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.compiler.email}"
}

resource "google_storage_bucket_iam_member" "compiler_pdf_objects" {
  count = var.cv_compilation_enabled ? 1 : 0

  bucket = google_storage_bucket.pdf_artifacts[0].name
  role   = "roles/storage.objectUser"
  member = "serviceAccount:${google_service_account.compiler.email}"
}

resource "google_storage_bucket_iam_member" "web_pdf_viewer" {
  count = var.cv_compilation_enabled ? 1 : 0

  bucket = google_storage_bucket.pdf_artifacts[0].name
  role   = "roles/storage.objectViewer"
  member = "serviceAccount:${google_service_account.web.email}"
}

resource "google_service_account_iam_member" "web_use_compilation_task_invoker" {
  count = var.cv_compilation_enabled ? 1 : 0

  service_account_id = google_service_account.compilation_task_invoker.name
  role               = "roles/iam.serviceAccountUser"
  member             = "serviceAccount:${google_service_account.web.email}"
}

resource "google_service_account_iam_member" "cloud_tasks_mint_compilation_token" {
  count = var.cv_compilation_enabled ? 1 : 0

  service_account_id = google_service_account.compilation_task_invoker.name
  role               = "roles/iam.serviceAccountTokenCreator"
  member             = "serviceAccount:service-${data.google_project.current.number}@gcp-sa-cloudtasks.iam.gserviceaccount.com"

  depends_on = [google_project_service.required["cloudtasks.googleapis.com"]]
}
