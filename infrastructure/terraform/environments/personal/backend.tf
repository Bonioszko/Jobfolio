terraform {
  backend "gcs" {
    bucket = "your-gcp-project-id-jobparser-tfstate"
    prefix = "environments/personal"
  }
}
