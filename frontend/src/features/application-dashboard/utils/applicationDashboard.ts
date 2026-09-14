import type { JobPosting } from "../../job-postings/types/jobPosting";

const APPLIED_WORKFLOW_STATUSES = new Set([
  "APPLIED",
  "INTERVIEWING",
  "REJECTED",
]);

export type ApplicationSummary = {
  total: number;
  awaitingResponse: number;
  interviewing: number;
  closed: number;
};

export function getAppliedJobPostings(postings: JobPosting[]) {
  return postings
    .filter((posting) =>
      APPLIED_WORKFLOW_STATUSES.has(posting.workflowStatus.toUpperCase()),
    )
    .sort((left, right) =>
      right.sourceReceivedAt.localeCompare(left.sourceReceivedAt) ||
      left.displayTitle.localeCompare(right.displayTitle),
    );
}

export function summarizeApplications(postings: JobPosting[]): ApplicationSummary {
  return postings.reduce<ApplicationSummary>((summary, posting) => {
    const status = posting.workflowStatus.toUpperCase();

    summary.total += 1;
    if (status === "APPLIED") summary.awaitingResponse += 1;
    if (status === "INTERVIEWING") summary.interviewing += 1;
    if (status === "REJECTED") summary.closed += 1;

    return summary;
  }, {
    total: 0,
    awaitingResponse: 0,
    interviewing: 0,
    closed: 0,
  });
}

export function formatApplicationDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(value));
}
