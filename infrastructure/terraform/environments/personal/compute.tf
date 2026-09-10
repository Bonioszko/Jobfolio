data "google_compute_image" "container_optimized_os" {
  project = "cos-cloud"
  family  = "cos-stable"
}

resource "google_compute_resource_policy" "database_snapshots" {
  count = var.database_vm_enabled ? 1 : 0

  name    = "jobparser-db-daily-snapshots"
  project = var.project_id
  region  = var.region

  snapshot_schedule_policy {
    schedule {
      daily_schedule {
        days_in_cycle = 1
        start_time    = "04:00"
      }
    }

    retention_policy {
      max_retention_days    = 7
      on_source_disk_delete = "KEEP_AUTO_SNAPSHOTS"
    }

    snapshot_properties {
      guest_flush       = false
      labels            = local.common_labels
      storage_locations = [var.region]
    }
  }

  depends_on = [google_project_service.required["compute.googleapis.com"]]
}

resource "google_compute_disk" "database" {
  count = var.database_vm_enabled ? 1 : 0

  name    = "jobparser-postgres-data"
  project = var.project_id
  zone    = var.zone
  type    = "pd-standard"
  size    = var.database_disk_size_gb
  labels  = local.common_labels

  lifecycle {
    prevent_destroy = true
  }
}

resource "google_compute_disk_resource_policy_attachment" "database_snapshots" {
  count = var.database_vm_enabled ? 1 : 0

  name    = google_compute_resource_policy.database_snapshots[0].name
  project = var.project_id
  zone    = var.zone
  disk    = google_compute_disk.database[0].name
}

resource "google_compute_instance" "database" {
  count = var.database_vm_enabled ? 1 : 0

  name                      = "jobparser-db"
  project                   = var.project_id
  zone                      = var.zone
  machine_type              = var.vm_machine_type
  allow_stopping_for_update = true
  can_ip_forward            = false
  deletion_protection       = true
  labels                    = local.common_labels
  tags                      = ["jobparser-db"]

  boot_disk {
    auto_delete = true

    initialize_params {
      image  = data.google_compute_image.container_optimized_os.self_link
      size   = var.boot_disk_size_gb
      type   = "pd-standard"
      labels = local.common_labels
    }
  }

  attached_disk {
    source      = google_compute_disk.database[0].id
    device_name = "jobparser-postgres-data"
    mode        = "READ_WRITE"
  }

  network_interface {
    subnetwork = google_compute_subnetwork.personal.id
  }

  metadata = {
    block-project-ssh-keys = "TRUE"
    enable-oslogin         = "TRUE"
    serial-port-enable     = "FALSE"
    startup-script = templatefile("${path.module}/templates/postgres-startup.sh.tftpl", {
      postgres_image     = local.postgres_image
      postgres_secret_id = google_secret_manager_secret.postgres_password.secret_id
      project_id         = var.project_id
      registry_host      = "${var.region}-docker.pkg.dev"
    })
  }

  scheduling {
    automatic_restart   = true
    on_host_maintenance = "MIGRATE"
    preemptible         = false
  }

  service_account {
    email  = google_service_account.database_vm.email
    scopes = ["https://www.googleapis.com/auth/cloud-platform"]
  }

  shielded_instance_config {
    enable_integrity_monitoring = true
    enable_secure_boot          = true
    enable_vtpm                 = true
  }

  depends_on = [
    google_project_iam_member.database_vm,
    google_project_iam_member.operator_os_admin,
    google_project_iam_member.operator_iap_tunnel,
    google_service_account_iam_member.operator_use_database_vm,
    google_artifact_registry_repository_iam_member.database_vm_reader,
    google_secret_manager_secret_iam_member.database_vm_password_accessor,
  ]
}
