import { httpClient } from "../../../lib/api/httpClient";
import type { JobPosting } from "../types/jobPosting";

export function getJobPostings(signal?: AbortSignal) {
  return httpClient.get<JobPosting[]>("/api/source-items", { signal });
}

export function getJobPosting(id: string, signal?: AbortSignal) {
  return httpClient.get<JobPosting>(`/api/source-items/${id}`, { signal });
}

export function updateJobPostingStatus(id: string, status: string) {
  return httpClient.put<void>(`/api/source-items/${id}/status`, { status });
}
