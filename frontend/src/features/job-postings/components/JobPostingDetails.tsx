import type { CandidateRules } from "../../candidate-rules/types/candidateRules";
import { CvTailoringPanel } from "../../cv-generation/components/CvTailoringPanel";
import type { CvTemplate } from "../../cv-templates/types/cvTemplate";
import type { DomainField, WorkflowStatus } from "../../domain-config/types/domainConfig";
import type { JobPosting } from "../types/jobPosting";
import { displayValue } from "../utils/displayValue";
import { JobStatusActions } from "./JobStatusActions";

type JobPostingDetailsProps = {
  documentName: string;
  fields: DomainField[];
  isStatusUpdating: boolean;
  isTailoringDataLoading: boolean;
  onStatusChange: (status: string) => Promise<void>;
  posting: JobPosting;
  rules?: CandidateRules;
  statuses: WorkflowStatus[];
  templates: CvTemplate[];
  workflowStatus: string;
};

export function JobPostingDetails({
  documentName,
  fields,
  isStatusUpdating,
  isTailoringDataLoading,
  onStatusChange,
  posting,
  rules,
  statuses,
  templates,
  workflowStatus,
}: JobPostingDetailsProps) {
  const detailFields = fields.filter(
    (field) => field.key !== "description" && field.key !== "url",
  );
  const company = displayValue(posting.parsedData.company);
  const location = displayValue(posting.parsedData.location);

  return (
    <div className="job-detail">
      <div className="detail-hero">
        <div className="detail-hero__meta">
          <span>{posting.sourceKey}</span>
          <span aria-hidden="true">·</span>
          <time dateTime={posting.sourceReceivedAt}>
            Received {new Date(posting.sourceReceivedAt).toLocaleDateString()}
          </time>
        </div>
        <h1>{posting.displayTitle}</h1>
        <p className="detail-company">
          <strong>{company}</strong>
          <span aria-hidden="true">·</span>
          <span>{location}</span>
        </p>
        {typeof posting.parsedData.url === "string" && (
          <a className="external-link" href={posting.parsedData.url} target="_blank" rel="noreferrer">
            View original posting <span aria-hidden="true">↗</span>
          </a>
        )}
      </div>

      <JobStatusActions
        currentStatus={workflowStatus}
        isUpdating={isStatusUpdating}
        onChange={onStatusChange}
        statuses={statuses}
      />

      <section className="detail-section" aria-labelledby="overview-heading">
        <span className="section-kicker">ROLE OVERVIEW</span>
        <h3 id="overview-heading">What they are looking for</h3>
        <p className="job-description">{displayValue(posting.parsedData.description)}</p>
      </section>

      {detailFields.length > 0 && (
        <dl className="job-facts">
          {detailFields.map((field) => (
            <div key={field.key}>
              <dt>{field.label}</dt>
              <dd>{displayValue(posting.parsedData[field.key])}</dd>
            </div>
          ))}
          <div>
            <dt>Parser</dt>
            <dd>{posting.parserKey} · v{posting.parserVersion}</dd>
          </div>
        </dl>
      )}

      <details className="source-disclosure">
        <summary>
          <span>Parsed source email</span>
          <span aria-hidden="true">+</span>
        </summary>
        <iframe
          className="email-preview"
          sandbox=""
          srcDoc={posting.demoEmailHtml ?? ""}
          title="Parsed source email"
        />
      </details>

      <CvTailoringPanel
        documentName={documentName}
        isLoadingResources={isTailoringDataLoading}
        jobPostingId={posting.id}
        rules={rules}
        templates={templates}
      />
    </div>
  );
}
