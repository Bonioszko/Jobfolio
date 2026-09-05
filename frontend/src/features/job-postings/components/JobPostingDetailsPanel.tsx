import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { CandidateRules } from "../../candidate-rules/types/candidateRules";
import type { CvTemplate } from "../../cv-templates/types/cvTemplate";
import type { DomainField, WorkflowStatus } from "../../domain-config/types/domainConfig";
import { useJobPosting } from "../hooks/useJobPosting";
import { JobPostingDetails } from "./JobPostingDetails";

type JobPostingDetailsPanelProps = {
  documentName: string;
  fields: DomainField[];
  isStatusUpdating: boolean;
  isTailoringDataLoading: boolean;
  onStatusChange: (status: string) => Promise<void>;
  postingId?: string;
  rules?: CandidateRules;
  sourceItemName: string;
  statuses: WorkflowStatus[];
  templates: CvTemplate[];
  workflowStatus?: string;
};

export function JobPostingDetailsPanel({
  documentName,
  fields,
  isStatusUpdating,
  isTailoringDataLoading,
  onStatusChange,
  postingId,
  rules,
  sourceItemName,
  statuses,
  templates,
  workflowStatus,
}: JobPostingDetailsPanelProps) {
  const posting = useJobPosting(postingId);

  return (
    <aside className="job-detail-panel">
      {!postingId && (
        <div className="detail-placeholder">
          <span className="detail-placeholder__mark" aria-hidden="true">↗</span>
          <strong>Select a {sourceItemName.toLowerCase()}</strong>
          <span>Its details and application tools will open here.</span>
        </div>
      )}
      {postingId && posting.isLoading && (
        <div className="detail-loading" aria-label="Loading job details">
          <span />
          <span />
          <span />
        </div>
      )}
      <ErrorMessage className="detail-error" message={posting.error} />
      {posting.data && (
        <JobPostingDetails
          key={posting.data.id}
          documentName={documentName}
          fields={fields}
          isStatusUpdating={isStatusUpdating}
          isTailoringDataLoading={isTailoringDataLoading}
          onStatusChange={onStatusChange}
          posting={posting.data}
          rules={rules}
          statuses={statuses}
          templates={templates}
          workflowStatus={workflowStatus ?? posting.data.workflowStatus}
        />
      )}
    </aside>
  );
}
