locals {
  cloud_run_database_environment = {
    "Database__Host"            = try(google_compute_instance.database[0].network_interface[0].network_ip, "")
    "Database__Port"            = "5432"
    "Database__Name"            = "jobparser"
    "Database__Username"        = "jobparser"
    "Database__MaximumPoolSize" = "5"
  }

  cloud_run_cv_environment = local.cv_compilation_runtime_enabled ? {
    "Artifacts__Provider"                            = "GoogleCloud"
    "Artifacts__Bucket"                              = google_storage_bucket.pdf_artifacts[0].name
    "Queues__CvCompilation__Provider"                = "CloudTasks"
    "Queues__CvCompilation__ProjectId"               = var.project_id
    "Queues__CvCompilation__Location"                = var.region
    "Queues__CvCompilation__QueueId"                 = google_cloud_tasks_queue.cv_compilation[0].name
    "Queues__CvCompilation__TargetUrl"               = "${google_cloud_run_v2_service.compiler[0].uri}/internal/cv-compilation-jobs"
    "Queues__CvCompilation__OidcServiceAccountEmail" = google_service_account.compilation_task_invoker.email
    "Queues__CvCompilation__OidcAudience"            = google_cloud_run_v2_service.compiler[0].uri
  } : {}
}

resource "google_cloud_run_v2_service" "web" {
  count = var.application_runtime_enabled ? 1 : 0

  name                = "jobparser-web"
  project             = var.project_id
  location            = var.region
  ingress             = "INGRESS_TRAFFIC_ALL"
  deletion_protection = false
  labels              = merge(local.common_labels, { component = "web" })

  template {
    service_account = google_service_account.web.email
    timeout         = "60s"

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
      image = var.application_image

      ports {
        name           = "http1"
        container_port = 8080
      }

      resources {
        limits = {
          cpu    = "1"
          memory = "512Mi"
        }
        cpu_idle          = true
        startup_cpu_boost = true
      }

      startup_probe {
        initial_delay_seconds = 0
        timeout_seconds       = 1
        period_seconds        = 3
        failure_threshold     = 20

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
          path = "/api/health"
          port = 8080
        }
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "ASPNETCORE_FORWARDEDHEADERS_ENABLED"
        value = "true"
      }

      env {
        name  = "Database__InitializeOnStartup"
        value = "false"
      }

      env {
        name  = "Features__CvEnabled"
        value = tostring(local.cv_compilation_runtime_enabled)
      }

      env {
        name  = "Features__CvGenerationEnabled"
        value = "false"
      }

      env {
        name  = "Features__DemoEnabled"
        value = "false"
      }

      env {
        name  = "Authentication__Google__Enabled"
        value = tostring(var.authentication_enabled)
      }

      env {
        name  = "Authentication__Google__ClientId"
        value = var.authentication_google_client_id
      }

      env {
        name  = "Authentication__Google__FrontendUrl"
        value = var.application_base_url == "" ? "http://localhost/" : var.application_base_url
      }

      dynamic "env" {
        for_each = local.cloud_run_database_environment
        content {
          name  = env.key
          value = env.value
        }
      }


      dynamic "env" {
        for_each = local.cloud_run_cv_environment
        content {
          name  = env.key
          value = env.value
        }
      }

      dynamic "env" {
        for_each = var.authentication_enabled ? local.allowed_user_entries : []
        content {
          name  = "Authentication__Google__AllowedUsers__${env.key}__Email"
          value = env.value.email
        }
      }

      dynamic "env" {
        for_each = var.authentication_enabled ? local.allowed_user_entries : []
        content {
          name  = "Authentication__Google__AllowedUsers__${env.key}__WorkspaceId"
          value = env.value.workspace_id
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

      dynamic "env" {
        for_each = var.authentication_enabled ? [true] : []
        content {
          name = "Authentication__Google__ClientSecret"
          value_source {
            secret_key_ref {
              secret  = google_secret_manager_secret.google_auth_client_secret.secret_id
              version = "latest"
            }
          }
        }
      }
    }
  }

  lifecycle {
    precondition {
      condition     = var.database_vm_enabled
      error_message = "database_vm_enabled must be true before enabling the application runtime."
    }

    precondition {
      condition     = var.application_image != ""
      error_message = "application_image must reference an immutable image digest."
    }

    precondition {
      condition = !var.authentication_enabled || (
        var.application_base_url != "" &&
        var.authentication_google_client_id != "" &&
        length(var.allowed_users) > 0
      )
      error_message = "Google authentication requires application_base_url, authentication_google_client_id, and allowed_users."
    }

    ignore_changes = [
      client,
      client_version,
      template[0].containers[0].image,
    ]
  }

  depends_on = [
    google_project_service.required["run.googleapis.com"],
    google_secret_manager_secret_iam_member.web_database_password,
    google_secret_manager_secret_iam_member.web_auth_client_secret,
  ]
}

resource "google_cloud_run_v2_service_iam_member" "public_web" {
  count = var.application_runtime_enabled ? 1 : 0

  project  = var.project_id
  location = var.region
  name     = google_cloud_run_v2_service.web[0].name
  role     = "roles/run.invoker"
  member   = "allUsers"
}

resource "google_cloud_run_v2_job" "database_migrator" {
  count = var.application_runtime_enabled ? 1 : 0

  name                = "jobparser-db-migrate"
  project             = var.project_id
  location            = var.region
  deletion_protection = false
  labels              = merge(local.common_labels, { component = "database-migrator" })

  template {
    template {
      service_account = google_service_account.database_migrator.email
      timeout         = "300s"
      max_retries     = 0

      vpc_access {
        egress = "PRIVATE_RANGES_ONLY"

        network_interfaces {
          network    = google_compute_network.personal.name
          subnetwork = google_compute_subnetwork.personal.name
          tags       = ["jobparser-cloud-run"]
        }
      }

      containers {
        image   = var.application_image
        command = ["dotnet"]
        args    = ["/app/database-migrator/App.DatabaseMigrator.dll"]

        resources {
          limits = {
            cpu    = "1"
            memory = "512Mi"
          }
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
  }

  lifecycle {
    ignore_changes = [
      client,
      client_version,
      template[0].template[0].containers[0].image,
    ]
  }

  depends_on = [
    google_project_service.required["run.googleapis.com"],
    google_secret_manager_secret_iam_member.migrator_database_password,
  ]
}

resource "google_cloud_run_v2_job" "gmail_sync" {
  for_each = var.application_runtime_enabled ? local.enabled_gmail_sync_accounts : {}

  name                = "jobparser-gmail-sync-${each.key}"
  project             = var.project_id
  location            = var.region
  deletion_protection = false
  labels              = merge(local.common_labels, { component = "gmail-sync", account = each.key })

  template {
    template {
      service_account = google_service_account.gmail_sync.email
      timeout         = "300s"
      max_retries     = 1

      vpc_access {
        egress = "PRIVATE_RANGES_ONLY"

        network_interfaces {
          network    = google_compute_network.personal.name
          subnetwork = google_compute_subnetwork.personal.name
          tags       = ["jobparser-cloud-run"]
        }
      }

      containers {
        image   = var.application_image
        command = ["dotnet"]
        args    = ["/app/gmail-sync/App.GmailSync.dll"]

        resources {
          limits = {
            cpu    = "1"
            memory = "512Mi"
          }
        }

        env {
          name  = "Gmail__Enabled"
          value = "true"
        }

        env {
          name  = "Gmail__RunOnce"
          value = "true"
        }

        env {
          name  = "Database__InitializeOnStartup"
          value = "false"
        }

        env {
          name  = "Gmail__WorkspaceKey"
          value = "user:${each.value.workspace_id}"
        }

        env {
          name  = "Gmail__MaxMessagesPerRun"
          value = "100"
        }

        env {
          name  = "Gmail__OAuth__ClientId"
          value = var.gmail_oauth_client_id
        }

        dynamic "env" {
          for_each = local.cloud_run_database_environment
          content {
            name  = env.key
            value = env.value
          }
        }

        dynamic "env" {
          for_each = toset(each.value.labels)
          content {
            name  = "Gmail__Labels__${index(each.value.labels, env.value)}"
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

        env {
          name = "Gmail__OAuth__ClientSecret"
          value_source {
            secret_key_ref {
              secret  = google_secret_manager_secret.gmail_oauth_client_secret.secret_id
              version = "latest"
            }
          }
        }

        env {
          name = "Gmail__OAuth__RefreshToken"
          value_source {
            secret_key_ref {
              secret  = google_secret_manager_secret.gmail_refresh_token[each.key].secret_id
              version = "latest"
            }
          }
        }
      }
    }
  }

  lifecycle {
    precondition {
      condition     = var.gmail_oauth_client_id != ""
      error_message = "gmail_oauth_client_id is required when Gmail sync accounts are enabled."
    }

    precondition {
      condition     = contains(values(var.allowed_users), each.value.workspace_id)
      error_message = "Every Gmail sync workspace_id must belong to an allowed user."
    }

    ignore_changes = [
      client,
      client_version,
      template[0].template[0].containers[0].image,
    ]
  }

  depends_on = [
    google_project_service.required["run.googleapis.com"],
    google_secret_manager_secret_iam_member.gmail_database_password,
    google_secret_manager_secret_iam_member.gmail_client_secret,
    google_secret_manager_secret_iam_member.gmail_refresh_token,
  ]
}

resource "google_cloud_run_v2_job_iam_member" "scheduler_gmail_invoker" {
  for_each = google_cloud_run_v2_job.gmail_sync

  project  = var.project_id
  location = var.region
  name     = each.value.name
  role     = "roles/run.invoker"
  member   = "serviceAccount:${google_service_account.scheduler.email}"
}

resource "google_cloud_scheduler_job" "gmail_sync" {
  for_each = google_cloud_run_v2_job.gmail_sync

  name        = "jobparser-gmail-sync-${each.key}"
  project     = var.project_id
  region      = var.region
  description = "Runs the ${each.key} Gmail import with scale-to-zero Cloud Run compute"
  schedule    = var.gmail_sync_accounts[each.key].schedule
  time_zone   = var.gmail_sync_accounts[each.key].time_zone

  attempt_deadline = "320s"

  retry_config {
    retry_count = 1
  }

  http_target {
    http_method = "POST"
    uri         = "https://run.googleapis.com/v2/projects/${var.project_id}/locations/${var.region}/jobs/${each.value.name}:run"

    oauth_token {
      service_account_email = google_service_account.scheduler.email
      scope                 = "https://www.googleapis.com/auth/cloud-platform"
    }
  }

  depends_on = [
    google_project_service.required["cloudscheduler.googleapis.com"],
    google_cloud_run_v2_job_iam_member.scheduler_gmail_invoker,
  ]
}
