import assert from "node:assert/strict";
import test from "node:test";
import type { InterviewNote } from "../src/features/interview-notes/types/interviewNote.ts";
import type { JobPosting } from "../src/features/job-postings/types/jobPosting.ts";
import {
  createInterviewProcess,
  getInterviewCandidates,
  sortInterviewProcesses,
} from "../src/features/interview-dashboard/utils/interviewDashboard.ts";

test("uses the most recent dated note as the latest interview", () => {
  const process = createInterviewProcess(posting("one", "2026-09-01T10:00:00Z"), [
    note("older", "2026-08-28", "2026-08-28T12:00:00Z"),
    note("latest", "2026-09-05", "2026-09-05T12:00:00Z"),
  ]);

  assert.equal(process.latestInterview?.id, "latest");
  assert.deepEqual(process.notes.map((item) => item.id), ["latest", "older"]);
});

test("offers only jobs that are not already in the interview dashboard", () => {
  const applied = posting("applied", "2026-09-08T10:00:00Z", "APPLIED");
  const interviewing = posting(
    "interviewing",
    "2026-09-09T10:00:00Z",
    "INTERVIEWING",
  );
  const newJob = posting("new", "YYYY-MM-DDT10:00:00Z", "NEW");

  const candidates = getInterviewCandidates([newJob, interviewing, applied]);

  assert.deepEqual(candidates.map((item) => item.id), ["applied", "new"]);
});

test("sorts active processes by latest interview and leaves missing history last", () => {
  const older = createInterviewProcess(posting("older", "2026-09-08T10:00:00Z"), [
    note("first", "2026-09-01", "2026-09-01T12:00:00Z"),
  ]);
  const newer = createInterviewProcess(posting("newer", "2026-09-07T10:00:00Z"), [
    note("second", "2026-09-06", "2026-09-06T12:00:00Z"),
  ]);
  const withoutHistory = createInterviewProcess(
    posting("without-history", "2026-09-09T10:00:00Z"),
    [],
  );

  const sorted = sortInterviewProcesses([withoutHistory, older, newer]);

  assert.deepEqual(sorted.map((item) => item.posting.id), [
    "newer",
    "older",
    "without-history",
  ]);
});

function posting(
  id: string,
  sourceReceivedAt: string,
  workflowStatus = "INTERVIEWING",
): JobPosting {
  return {
    id,
    sourceKey: "LinkedIn",
    displayTitle: id,
    parsedData: { company: "Example", location: "Warsaw" },
    workflowStatus,
    parserKey: "linkedin",
    parserVersion: 1,
    sourceReceivedAt,
    appliedAt: workflowStatus === "NEW" ? null : "2026-09-10T08:00:00Z",
  };
}

function note(id: string, interviewDate: string, createdAt: string): InterviewNote {
  return {
    id,
    jobPostingId: "job",
    stage: "Technical interview",
    interviewDate,
    notes: "Discussed the role.",
    createdAt,
  };
}
