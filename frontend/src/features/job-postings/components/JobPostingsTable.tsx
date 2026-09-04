import type {
  DomainField,
  WorkflowStatus,
} from "../../domain-config/types/domainConfig";
import type { JobPosting } from "../types/jobPosting";
import { displayValue } from "../utils/displayValue";

type JobPostingsTableProps = {
  fields: DomainField[];
  postings: JobPosting[];
  statuses: WorkflowStatus[];
  updatingIds: Set<string>;
  onSelect: (id: string) => void;
  onStatusChange: (id: string, status: string) => Promise<void>;
};

export function JobPostingsTable({
  fields,
  postings,
  statuses,
  updatingIds,
  onSelect,
  onStatusChange,
}: JobPostingsTableProps) {
  const dashboardFields = fields.filter((field) => field.showInDashboard);

  if (postings.length === 0) {
    return <div className="card empty-state">No job postings have been imported yet.</div>;
  }

  return (
    <div className="card table">
      <table>
        <thead>
          <tr>
            <th>Received</th>
            <th>Source</th>
            <th>Title</th>
            {dashboardFields.map((field) => (
              <th key={field.key}>{field.label}</th>
            ))}
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {postings.map((posting) => (
            <tr key={posting.id} onClick={() => onSelect(posting.id)}>
              <td>{new Date(posting.sourceReceivedAt).toLocaleDateString()}</td>
              <td>{posting.sourceKey}</td>
              <td>{posting.displayTitle}</td>
              {dashboardFields.map((field) => (
                <td key={field.key}>{displayValue(posting.parsedData[field.key])}</td>
              ))}
              <td onClick={(event) => event.stopPropagation()}>
                <select
                  aria-label={`Status for ${posting.displayTitle}`}
                  disabled={updatingIds.has(posting.id)}
                  value={posting.workflowStatus}
                  onChange={(event) => void onStatusChange(posting.id, event.target.value)}
                >
                  {statuses.map((status) => (
                    <option key={status.code} value={status.code}>
                      {status.label}
                    </option>
                  ))}
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
