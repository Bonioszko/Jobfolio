import type { InterviewNote } from "../../interview-notes/types/interviewNote";
import type { JobPosting } from "../../job-postings/types/jobPosting";
import type { InterviewProcess } from "../types/interviewDashboard";

export function getInterviewCandidates(postings: JobPosting[]) {
  return postings
    .filter((posting) => posting.workflowStatus !== "INTERVIEWING")
    .sort((left, right) => left.displayTitle.localeCompare(right.displayTitle));
}

export function createInterviewProcess(
  posting: JobPosting,
  notes: InterviewNote[],
): InterviewProcess {
  const sortedNotes = [...notes].sort((left, right) =>
    right.interviewDate.localeCompare(left.interviewDate) ||
    right.createdAt.localeCompare(left.createdAt),
  );

  return {
    latestInterview: sortedNotes[0],
    notes: sortedNotes,
    posting,
  };
}

export function sortInterviewProcesses(processes: InterviewProcess[]) {
  return [...processes].sort((left, right) => {
    const leftDate = left.latestInterview?.interviewDate;
    const rightDate = right.latestInterview?.interviewDate;

    if (leftDate && rightDate) return rightDate.localeCompare(leftDate);
    if (leftDate) return -1;
    if (rightDate) return 1;

    return right.posting.sourceReceivedAt.localeCompare(left.posting.sourceReceivedAt);
  });
}

export function formatInterviewDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(`${value}T00:00:00Z`));
}
