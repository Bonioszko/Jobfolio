resource "google_service_account" "database_vm" {
  project      = var.project_id
  account_id   = "jobparser-db-vm"
  display_name = "Job Parser database VM"

  depends_on = [google_project_service.required["iam.googleapis.com"]]
}

locals {
  database_vm_project_roles = toset([
    "roles/logging.logWriter",
    "roles/monitoring.metricWriter",
  ])
}

resource "google_project_iam_member" "database_vm" {
  for_each = local.database_vm_project_roles

  project = var.project_id
  role    = each.value
  member  = "serviceAccount:${google_service_account.database_vm.email}"
}

resource "google_project_iam_member" "operator_iap_tunnel" {
  project = var.project_id
  role    = "roles/iap.tunnelResourceAccessor"
  member  = "user:${var.operator_email}"
}

resource "google_project_iam_member" "operator_os_admin" {
  project = var.project_id
  role    = "roles/compute.osAdminLogin"
  member  = "user:${var.operator_email}"
}

resource "google_service_account_iam_member" "operator_use_database_vm" {
  service_account_id = google_service_account.database_vm.name
  role               = "roles/iam.serviceAccountUser"
  member             = "user:${var.operator_email}"
}
