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

resource "google_secret_manager_secret" "google_auth_client_secret" {
  project   = var.project_id
  secret_id = "jobparser-google-auth-client-secret"
  labels    = local.common_labels

  replication {
    auto {}
  }

  depends_on = [google_project_service.required["secretmanager.googleapis.com"]]
}

resource "google_secret_manager_secret" "gmail_oauth_client_secret" {
  project   = var.project_id
  secret_id = "jobparser-gmail-oauth-client-secret"
  labels    = local.common_labels

  replication {
    auto {}
  }

  depends_on = [google_project_service.required["secretmanager.googleapis.com"]]
}

resource "google_secret_manager_secret" "gmail_refresh_token" {
  for_each = var.gmail_sync_accounts

  project   = var.project_id
  secret_id = "jobparser-gmail-refresh-token-${each.key}"
  labels    = local.common_labels

  replication {
    auto {}
  }

  depends_on = [google_project_service.required["secretmanager.googleapis.com"]]
}

locals {
  operator_managed_application_secrets = merge(
    {
      google-auth-client-secret = google_secret_manager_secret.google_auth_client_secret.secret_id
      gmail-oauth-client-secret = google_secret_manager_secret.gmail_oauth_client_secret.secret_id
    },
    {
      for key, secret in google_secret_manager_secret.gmail_refresh_token :
      "gmail-refresh-token-${key}" => secret.secret_id
    },
  )
}

resource "google_secret_manager_secret_iam_member" "operator_application_secret_version_adder" {
  for_each = local.operator_managed_application_secrets

  project   = var.project_id
  secret_id = each.value
  role      = "roles/secretmanager.secretVersionAdder"
  member    = "user:${var.operator_email}"
}
