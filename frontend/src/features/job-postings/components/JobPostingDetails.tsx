import { useEffect, useRef } from "react";
import type { CandidateRules } from "../../candidate-rules/types/candidateRules";
import { CvTailoringPanel } from "../../cv-generation/components/CvTailoringPanel";
import type {
  CvTemplate,
  SaveCvTemplateInput,
} from "../../cv-templates/types/cvTemplate";
import type {
  DomainField,
  WorkflowStatus,
} from "../../domain-config/types/domainConfig";
import { InterviewNotesPanel } from "../../interview-notes/components/InterviewNotesPanel";
import type { JobPosting } from "../types/jobPosting";
import { displayValue } from "../utils/displayValue";
import { matchesUnmodifiedShortcut } from "../utils/keyboardShortcut";
import { JobStatusActions } from "./JobStatusActions";

type JobPostingDetailsProps = {
  documentName: string;
  fields: DomainField[];
  isStatusUpdating: boolean;
  isTailoringDataLoading: boolean;
  isTemplateSaving: boolean;
  onStatusChange: (status: string) => Promise<void>;
  onTemplateSave: (
    input: SaveCvTemplateInput,
  ) => Promise<CvTemplate | undefined>;
  posting: JobPosting;
  rules?: CandidateRules;
  statuses: WorkflowStatus[];
  templates: CvTemplate[];
  templateSaveError?: string;
  workflowStatus: string;
};

export function JobPostingDetails({
  documentName,
  fields,
  isStatusUpdating,
  isTailoringDataLoading,
  isTemplateSaving,
  onStatusChange,
  onTemplateSave,
  posting,
  rules,
  statuses,
  templates,
  templateSaveError,
  workflowStatus,
}: JobPostingDetailsProps) {
  const detailFields = fields.filter(
    (field) => field.key !== "description" && field.key !== "url",
  );
  const company = displayValue(posting.parsedData.company);
  const location = displayValue(posting.parsedData.location);
  const originalPostingUrl =
    typeof posting.parsedData.url === "string" && posting.parsedData.url.trim()
      ? posting.parsedData.url
      : undefined;
  const originalPostingLink = useRef<HTMLAnchorElement>(null);

  useEffect(() => {
    if (!originalPostingUrl) return;

    const openOriginalPosting = (event: KeyboardEvent) => {
      if (!matchesUnmodifiedShortcut(event, "w")) {
        return;
      }

      event.preventDefault();
      originalPostingLink.current?.click();
    };

    window.addEventListener("keydown", openOriginalPosting);
    return () => window.removeEventListener("keydown", openOriginalPosting);
  }, [originalPostingUrl]);

  return (
    <div className="job-detail">
      <div className="detail-hero">
        <div className="detail-hero__topline">
          {originalPostingUrl && (
            <a
              aria-keyshortcuts="W"
              className="external-link"
              href={originalPostingUrl}
              ref={originalPostingLink}
              rel="noreferrer"
              target="_blank"
            >
              <span>View original posting</span>
              <kbd className="shortcut-key">W</kbd>
              <span aria-hidden="true">↗</span>
            </a>
          )}
          <div className="detail-hero__meta">
            <span>{posting.sourceKey}</span>
            <span aria-hidden="true">·</span>
            <time dateTime={posting.sourceReceivedAt}>
              Received {new Date(posting.sourceReceivedAt).toLocaleDateString()}
            </time>
          </div>
        </div>
        <h1>{posting.displayTitle}</h1>
        <p className="detail-company">
          <strong>{company}</strong>
          <span aria-hidden="true">·</span>
          <span>{location}</span>
        </p>
      </div>

      <JobStatusActions
        currentStatus={workflowStatus}
        isUpdating={isStatusUpdating}
        onChange={onStatusChange}
        statuses={statuses}
      />



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
            <dd>
              {posting.parserKey} · v{posting.parserVersion}
            </dd>
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

      <section className="detail-section" aria-labelledby="overview-heading">
        <span className="section-kicker">ROLE OVERVIEW</span>
        <h3 id="overview-heading">What they are looking for</h3>
        <p className="job-description">
          {displayValue(posting.parsedData.description)}
        </p>
      </section>
      <InterviewNotesPanel jobPostingId={posting.id} />

      <CvTailoringPanel
        documentName={documentName}
        isLoadingResources={isTailoringDataLoading}
        isTemplateSaving={isTemplateSaving}
        jobPostingId={posting.id}
        onTemplateSave={onTemplateSave}
        rules={rules}
        templateSaveError={templateSaveError}
        templates={templates}
      />
    </div>
  );
}
