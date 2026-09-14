import assert from "node:assert/strict";
import test from "node:test";
import type { JobPosting } from "../src/features/job-postings/types/jobPosting.ts";
import {
  getAppliedJobPostings,
  summarizeApplications,
} from "../src/features/application-dashboard/utils/applicationDashboard.ts";

test("shows every submitted job while excluding jobs not yet applied to", () => {
  const applications = getAppliedJobPostings([
    posting("new", "NEW"),
    posting("queued", "TO_APPLY"),
    posting("applied", "APPLIED"),
    posting("interview", "INTERVIEWING"),
    posting("closed", "REJECTED"),
    posting("skipped", "SKIP"),
  ]);

  assert.deepEqual(
    applications.map((application) => application.id),
    ["applied", "closed", "interview"],
  );
});

test("orders submitted jobs by newest received date", () => {
  const applications = getAppliedJobPostings([
    posting("older", "APPLIED", "2026-08-14T08:00:00Z"),
    posting("newest", "INTERVIEWING", "2026-09-14T08:00:00Z"),
    posting("middle", "REJECTED", "2026-09-01T08:00:00Z"),
  ]);

  assert.deepEqual(
    applications.map((application) => application.id),
    ["newest", "middle", "older"],
  );
});

test("summarizes each application stage", () => {
  const summary = summarizeApplications([
    posting("one", "APPLIED"),
    posting("two", "APPLIED"),
    posting("three", "INTERVIEWING"),
    posting("four", "REJECTED"),
  ]);

  assert.deepEqual(summary, {
    total: 4,
    awaitingResponse: 2,
    interviewing: 1,
    closed: 1,
  });
});

function posting(
  id: string,
  workflowStatus: string,
  sourceReceivedAt = "2026-09-14T08:00:00Z",
): JobPosting {
  return {
    id,
    sourceKey: "LinkedIn",
    displayTitle: id,
    parsedData: {
      company: "Example",
      location: "Warsaw",
    },
    workflowStatus,
    parserKey: "linkedin",
    parserVersion: 1,
    sourceReceivedAt,
  };
}
