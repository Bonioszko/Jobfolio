resource "google_storage_bucket" "pdf_artifacts" {
  count = var.cv_compilation_enabled ? 1 : 0

  name                        = "${var.project_id}-jobparser-pdf-artifacts"
  project                     = var.project_id
  location                    = var.region
  storage_class               = "STANDARD"
  uniform_bucket_level_access = true
  public_access_prevention    = "enforced"
  force_destroy               = false
  labels                      = merge(local.common_labels, { component = "pdf-artifacts" })

  versioning {
    enabled = false
  }

  soft_delete_policy {
    retention_duration_seconds = 0
  }

  depends_on = [google_project_service.required["storage.googleapis.com"]]
}
