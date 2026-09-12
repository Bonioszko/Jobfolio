import assert from "node:assert/strict";
import test from "node:test";
import type { WorkflowStatus } from "../src/features/domain-config/types/domainConfig.ts";
import type { JobPosting } from "../src/features/job-postings/types/jobPosting.ts";
import { updateWorkflowStatus } from "../src/features/job-postings/utils/updateWorkflowStatus.ts";
import { groupApplicationsByStatus } from "../src/features/application-kanban/utils/applicationKanban.ts";

const statuses: WorkflowStatus[] = [
  { code: "NEW", label: "New" },
  { code: "TO_APPLY", label: "To apply" },
  { code: "APPLIED", label: "Applied" },
];

test("groups every application into configured Kanban columns in workflow order", () => {
  const postings = [
    posting("applied", "APPLIED"),
    posting("new", "NEW"),
    posting("queued", "TO_APPLY"),
  ];

  const columns = groupApplicationsByStatus(postings, statuses);

  assert.deepEqual(
    columns.map((column) => ({
      status: column.status.code,
      ids: column.postings.map((item) => item.id),
    })),
    [
      { status: "NEW", ids: ["new"] },
      { status: "TO_APPLY", ids: ["queued"] },
      { status: "APPLIED", ids: ["applied"] },
    ],
  );
});

test("keeps empty stages visible and appends legacy statuses without losing jobs", () => {
  const columns = groupApplicationsByStatus(
    [posting("legacy", "ON_HOLD")],
    statuses,
  );

  assert.deepEqual(
    columns.map((column) => [column.status.code, column.postings.length]),
    [
      ["NEW", 0],
      ["TO_APPLY", 0],
      ["APPLIED", 0],
      ["ON_HOLD", 1],
    ],
  );
});

test("moving an application updates only its Kanban column", () => {
  const original = [posting("target", "NEW"), posting("other", "NEW")];

  const updated = updateWorkflowStatus(original, "target", "APPLIED");
  const columns = groupApplicationsByStatus(updated, statuses);

  assert.equal(original[0].workflowStatus, "NEW");
  assert.deepEqual(
    columns.map((column) => [
      column.status.code,
      column.postings.map((item) => item.id),
    ]),
    [
      ["NEW", ["other"]],
      ["TO_APPLY", []],
      ["APPLIED", ["target"]],
    ],
  );
});

function posting(id: string, workflowStatus: string): JobPosting {
  return {
    id,
    sourceKey: "linkedin",
    displayTitle: `${id} engineer`,
    parsedData: {
      company: "Example",
      location: "Warsaw",
    },
    workflowStatus,
    parserKey: "linkedin",
    parserVersion: 2,
    sourceReceivedAt: "2026-09-13T08:00:00Z",
  };
}
