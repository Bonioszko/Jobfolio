resource "google_cloud_tasks_queue" "cv_compilation" {
  count = local.cv_compilation_runtime_enabled ? 1 : 0

  name     = "jobparser-cv-compilation"
  project  = var.project_id
  location = var.region

  rate_limits {
    max_concurrent_dispatches = 1
    max_dispatches_per_second = 1
  }

  retry_config {
    max_attempts       = 10
    max_retry_duration = "3600s"
    min_backoff        = "5s"
    max_backoff        = "60s"
    max_doublings      = 4
  }

  stackdriver_logging_config {
    sampling_ratio = 1
  }

  depends_on = [google_project_service.required["cloudtasks.googleapis.com"]]
}

resource "google_cloud_run_v2_service" "compiler" {
  count = local.cv_compilation_runtime_enabled ? 1 : 0

  name                = "jobparser-compiler"
  project             = var.project_id
  location            = var.region
  ingress             = "INGRESS_TRAFFIC_INTERNAL_ONLY"
  deletion_protection = false
  labels              = merge(local.common_labels, { component = "compiler" })

  template {
    service_account                  = google_service_account.compiler.email
    timeout                          = "60s"
    max_instance_request_concurrency = 1

    scaling {
      min_instance_count = 0
      max_instance_count = 1
    }

    vpc_access {
      egress = "PRIVATE_RANGES_ONLY"

      network_interfaces {
        network    = google_compute_network.personal.name
        subnetwork = google_compute_subnetwork.personal.name
        tags       = ["jobparser-cloud-run"]
      }
    }

    containers {
      image = var.compiler_image

      ports {
        name           = "http1"
        container_port = 8080
      }

      resources {
        limits = {
          cpu    = "1"
          memory = "1Gi"
        }
        cpu_idle          = true
        startup_cpu_boost = true
      }

      startup_probe {
        initial_delay_seconds = 0
        timeout_seconds       = 1
        period_seconds        = 3
        failure_threshold     = 30

        tcp_socket {
          port = 8080
        }
      }

      liveness_probe {
        initial_delay_seconds = 10
        timeout_seconds       = 2
        period_seconds        = 30
        failure_threshold     = 3

        http_get {
          path = "/health"
          port = 8080
        }
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "CompilationWorker__PollingEnabled"
        value = "false"
      }

      env {
        name  = "Compilation__Executable"
        value = "tectonic"
      }

      env {
        name  = "Compilation__TimeoutSeconds"
        value = "25"
      }

      env {
        name  = "Compilation__MaxTexBytes"
        value = "200000"
      }

      env {
        name  = "Artifacts__Provider"
        value = "GoogleCloud"
      }

      env {
        name  = "Artifacts__Bucket"
        value = google_storage_bucket.pdf_artifacts[0].name
      }

      dynamic "env" {
        for_each = local.cloud_run_database_environment
        content {
          name  = env.key
          value = env.value
        }
      }

      env {
        name = "Database__Password"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.postgres_password.secret_id
            version = "latest"
          }
        }
      }
    }
  }

  lifecycle {
    precondition {
      condition     = var.compiler_image != ""
      error_message = "compiler_image must reference an immutable image digest before CV compilation is enabled."
    }

    ignore_changes = [
      client,
      client_version,
      template[0].containers[0].image,
    ]
  }

  depends_on = [
    google_project_service.required["run.googleapis.com"],
    google_secret_manager_secret_iam_member.compiler_database_password,
    google_storage_bucket_iam_member.compiler_pdf_objects,
  ]
}

resource "google_cloud_run_v2_service_iam_member" "task_compiler_invoker" {
  count = local.cv_compilation_runtime_enabled ? 1 : 0

  project  = var.project_id
  location = var.region
  name     = google_cloud_run_v2_service.compiler[0].name
  role     = "roles/run.invoker"
  member   = "serviceAccount:${google_service_account.compilation_task_invoker.email}"
}

resource "google_cloud_tasks_queue_iam_member" "web_compilation_enqueuer" {
  count = local.cv_compilation_runtime_enabled ? 1 : 0

  project  = var.project_id
  location = var.region
  name     = google_cloud_tasks_queue.cv_compilation[0].name
  role     = "roles/cloudtasks.enqueuer"
  member   = "serviceAccount:${google_service_account.web.email}"
}
