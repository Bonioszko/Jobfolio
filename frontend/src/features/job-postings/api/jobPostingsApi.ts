import { httpClient } from "../../../lib/api/httpClient";
import type { JobPosting, JobPostingPage } from "../types/jobPosting";

type JobPostingFilters = {
  status?: string;
};

export function getJobPostings(
  cursor?: string,
  signal?: AbortSignal,
  limit = 100,
  filters: JobPostingFilters = {},
) {
  const parameters = new URLSearchParams({ limit: String(limit) });
  if (cursor) parameters.set("cursor", cursor);
  if (filters.status) parameters.set("status", filters.status);

  const query = `?${parameters.toString()}`;
  return httpClient.get<JobPostingPage>(`/api/source-items${query}`, { signal });
}

export async function getAllJobPostings(
  signal?: AbortSignal,
  filters: JobPostingFilters = {},
) {
  const items: JobPosting[] = [];
  const seenIds = new Set<string>();
  const seenCursors = new Set<string>();
  let cursor: string | undefined;

  do {
    const page = await getJobPostings(cursor, signal, 100, filters);
    for (const posting of page.items) {
      if (seenIds.add(posting.id)) items.push(posting);
    }

    cursor = page.nextCursor ?? undefined;
    if (cursor && !seenCursors.add(cursor)) {
      throw new Error("The job posting pagination cursor repeated unexpectedly.");
    }
  } while (cursor);

  return items;
}

export function getJobPosting(id: string, signal?: AbortSignal) {
  return httpClient.get<JobPosting>(`/api/source-items/${id}`, { signal });
}

export function updateJobPostingStatus(id: string, status: string) {
  return httpClient.put<void>(`/api/source-items/${id}/status`, { status });
}
