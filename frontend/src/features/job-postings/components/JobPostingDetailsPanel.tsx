import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { CandidateRules } from "../../candidate-rules/types/candidateRules";
import type { CvTemplate, SaveCvTemplateInput } from "../../cv-templates/types/cvTemplate";
import type { DomainField, WorkflowStatus } from "../../domain-config/types/domainConfig";
import { useJobPosting } from "../hooks/useJobPosting";
import { JobPostingDetails } from "./JobPostingDetails";

type JobPostingDetailsPanelProps = {
  cvEnabled: boolean;
  cvGenerationEnabled: boolean;
  documentName: string;
  fields: DomainField[];
  isStatusUpdating: boolean;
  isTailoringDataLoading: boolean;
  isTemplateSaving: boolean;
  onStatusChange: (status: string) => Promise<void>;
  onTemplateSave: (input: SaveCvTemplateInput) => Promise<CvTemplate | undefined>;
  postingId?: string;
  rules?: CandidateRules;
  sourceItemName: string;
  statuses: WorkflowStatus[];
  templates: CvTemplate[];
  templateSaveError?: string;
  workflowStatus?: string;
};

export function JobPostingDetailsPanel({
  cvEnabled,
  cvGenerationEnabled,
  documentName,
  fields,
  isStatusUpdating,
  isTailoringDataLoading,
  isTemplateSaving,
  onStatusChange,
  onTemplateSave,
  postingId,
  rules,
  sourceItemName,
  statuses,
  templates,
  templateSaveError,
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
          cvEnabled={cvEnabled}
          cvGenerationEnabled={cvGenerationEnabled}
          documentName={documentName}
          fields={fields}
          isStatusUpdating={isStatusUpdating}
          isTailoringDataLoading={isTailoringDataLoading}
          isTemplateSaving={isTemplateSaving}
          onStatusChange={onStatusChange}
          onTemplateSave={onTemplateSave}
          posting={posting.data}
          rules={rules}
          statuses={statuses}
          templates={templates}
          templateSaveError={templateSaveError}
          workflowStatus={workflowStatus ?? posting.data.workflowStatus}
        />
      )}
    </aside>
  );
}
