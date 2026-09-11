resource "google_compute_network" "personal" {
  name                    = "jobparser-personal"
  project                 = var.project_id
  auto_create_subnetworks = false
  routing_mode            = "REGIONAL"

  depends_on = [google_project_service.required["compute.googleapis.com"]]
}

resource "google_compute_subnetwork" "personal" {
  name                     = "jobparser-personal-${var.region}"
  project                  = var.project_id
  region                   = var.region
  network                  = google_compute_network.personal.id
  ip_cidr_range            = "10.42.0.0/24"
  private_ip_google_access = true
}

resource "google_compute_firewall" "iap_ssh" {
  name      = "jobparser-allow-iap-ssh"
  project   = var.project_id
  network   = google_compute_network.personal.name
  direction = "INGRESS"
  priority  = 1000

  source_ranges = ["35.235.240.0/20"]
  target_tags   = ["jobparser-db"]

  allow {
    protocol = "tcp"
    ports    = ["22"]
  }
}

resource "google_compute_firewall" "cloud_run_postgres" {
  name      = "jobparser-allow-cloud-run-postgres"
  project   = var.project_id
  network   = google_compute_network.personal.name
  direction = "INGRESS"
  priority  = 1000

  source_tags = ["jobparser-cloud-run"]
  target_tags = ["jobparser-db"]

  allow {
    protocol = "tcp"
    ports    = ["5432"]
  }
}
