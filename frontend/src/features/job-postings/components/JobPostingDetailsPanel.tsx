import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { CandidateRules } from "../../candidate-rules/types/candidateRules";
import type { CvTemplate } from "../../cv-templates/types/cvTemplate";
import { useJobPosting } from "../hooks/useJobPosting";
import { JobPostingDetails } from "./JobPostingDetails";

type JobPostingDetailsPanelProps = {
  documentName: string;
  postingId?: string;
  rules?: CandidateRules;
  sourceItemName: string;
  templates: CvTemplate[];
};

export function JobPostingDetailsPanel({
  documentName,
  postingId,
  rules,
  sourceItemName,
  templates,
}: JobPostingDetailsPanelProps) {
  const posting = useJobPosting(postingId);

  return (
    <aside className="card">
      {!postingId && (
        <p className="muted">
          Select a {sourceItemName.toLowerCase()} to inspect the parsed job post.
        </p>
      )}
      {postingId && posting.isLoading && <p className="muted">Loading job details…</p>}
      <ErrorMessage message={posting.error} />
      {posting.data && (
        <JobPostingDetails
          key={posting.data.id}
          documentName={documentName}
          posting={posting.data}
          rules={rules}
          templates={templates}
        />
      )}
    </aside>
  );
}
