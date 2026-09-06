import { httpClient } from "../../../lib/api/httpClient";
import type { JobPosting, JobPostingPage } from "../types/jobPosting";

export function getJobPostings(cursor?: string, signal?: AbortSignal) {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : "";
  return httpClient.get<JobPostingPage>(`/api/source-items${query}`, { signal });
}

export function getJobPosting(id: string, signal?: AbortSignal) {
  return httpClient.get<JobPosting>(`/api/source-items/${id}`, { signal });
}

export function updateJobPostingStatus(id: string, status: string) {
  return httpClient.put<void>(`/api/source-items/${id}/status`, { status });
}
