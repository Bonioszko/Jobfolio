resource "google_service_account" "github_deployer" {
  count = var.github_repository == "" ? 0 : 1

  project      = var.project_id
  account_id   = "jobparser-github-deployer"
  display_name = "Job Parser GitHub deployment"

  depends_on = [google_project_service.required["iam.googleapis.com"]]
}

resource "google_iam_workload_identity_pool" "github" {
  count = var.github_repository == "" ? 0 : 1

  project                   = var.project_id
  workload_identity_pool_id = "jobparser-github"
  display_name              = "Job Parser GitHub Actions"
}

resource "google_iam_workload_identity_pool_provider" "github" {
  count = var.github_repository == "" ? 0 : 1

  project                            = var.project_id
  workload_identity_pool_id          = google_iam_workload_identity_pool.github[0].workload_identity_pool_id
  workload_identity_pool_provider_id = "github"
  display_name                       = "Job Parser GitHub repository"

  attribute_mapping = {
    "google.subject"       = "assertion.sub"
    "attribute.repository" = "assertion.repository"
    "attribute.ref"        = "assertion.ref"
  }
  attribute_condition = "assertion.repository == '${var.github_repository}' && assertion.ref == 'refs/heads/main'"

  oidc {
    issuer_uri = "https://token.actions.githubusercontent.com"
  }
}

resource "google_service_account_iam_member" "github_workload_identity" {
  count = var.github_repository == "" ? 0 : 1

  service_account_id = google_service_account.github_deployer[0].name
  role               = "roles/iam.workloadIdentityUser"
  member             = "principalSet://iam.googleapis.com/${google_iam_workload_identity_pool.github[0].name}/attribute.repository/${var.github_repository}"
}

resource "google_artifact_registry_repository_iam_member" "github_writer" {
  count = var.github_repository == "" ? 0 : 1

  project    = google_artifact_registry_repository.containers.project
  location   = google_artifact_registry_repository.containers.location
  repository = google_artifact_registry_repository.containers.name
  role       = "roles/artifactregistry.writer"
  member     = "serviceAccount:${google_service_account.github_deployer[0].email}"
}

resource "google_project_iam_member" "github_run_developer" {
  count = var.github_repository == "" ? 0 : 1

  project = var.project_id
  role    = "roles/run.developer"
  member  = "serviceAccount:${google_service_account.github_deployer[0].email}"
}

resource "google_service_account_iam_member" "github_act_as_runtime" {
  for_each = var.github_repository == "" ? {} : {
    web               = google_service_account.web.name
    gmail-sync        = google_service_account.gmail_sync.name
    database-migrator = google_service_account.database_migrator.name
    compiler          = google_service_account.compiler.name
  }

  service_account_id = each.value
  role               = "roles/iam.serviceAccountUser"
  member             = "serviceAccount:${google_service_account.github_deployer[0].email}"
}

resource "google_cloud_run_v2_job_iam_member" "github_migrator_invoker" {
  count = var.github_repository != "" && var.application_runtime_enabled ? 1 : 0

  project  = var.project_id
  location = var.region
  name     = google_cloud_run_v2_job.database_migrator[0].name
  role     = "roles/run.invoker"
  member   = "serviceAccount:${google_service_account.github_deployer[0].email}"
}
