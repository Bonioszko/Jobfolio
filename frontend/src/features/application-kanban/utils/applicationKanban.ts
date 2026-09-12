import type { WorkflowStatus } from "../../domain-config/types/domainConfig";
import type { JobPosting } from "../../job-postings/types/jobPosting";

export type ApplicationKanbanColumn = {
  status: WorkflowStatus;
  postings: JobPosting[];
};

export function groupApplicationsByStatus(
  postings: JobPosting[],
  statuses: WorkflowStatus[],
): ApplicationKanbanColumn[] {
  const columns = statuses.map((status) => ({ status, postings: [] as JobPosting[] }));
  const columnsByStatus = new Map(
    columns.map((column) => [column.status.code, column]),
  );

  for (const posting of postings) {
    let column = columnsByStatus.get(posting.workflowStatus);
    if (!column) {
      column = {
        status: {
          code: posting.workflowStatus,
          label: posting.workflowStatus,
        },
        postings: [],
      };
      columns.push(column);
      columnsByStatus.set(posting.workflowStatus, column);
    }

    column.postings.push(posting);
  }

  return columns;
}
