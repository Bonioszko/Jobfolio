import type { JobPosting } from "../types/jobPosting";

export function updateWorkflowStatus(
  postings: JobPosting[],
  id: string,
  status: string,
) {
  return postings.map((posting) =>
    posting.id === id ? { ...posting, workflowStatus: status } : posting,
  );
}
