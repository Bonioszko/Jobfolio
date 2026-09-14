import type { JobPosting } from "../../job-postings/types/jobPosting";
import { displayValue } from "../../job-postings/utils/displayValue";
import { statusTone } from "../../job-postings/utils/statusPresentation";
import { formatApplicationDate } from "../utils/applicationDashboard";

type AppliedJobCardProps = {
  posting: JobPosting;
  statusLabel: string;
};

export function AppliedJobCard({ posting, statusLabel }: AppliedJobCardProps) {
  const company = displayValue(posting.parsedData.company);
  const location = displayValue(posting.parsedData.location);
  const employmentType = displayValue(posting.parsedData.employmentType);
  const salary = displayValue(posting.parsedData.salary);
  const detailsUrl = `/jobs?job=${encodeURIComponent(posting.id)}`;

  return (
    <article className="applied-job-card">
      <div className="applied-job-card__topline">
        <span>{posting.sourceKey}</span>
        <span className="status-mark" data-tone={statusTone(posting.workflowStatus)}>
          {statusLabel}
        </span>
      </div>

      <h2>
        <a href={detailsUrl}>{posting.displayTitle}</a>
      </h2>
      <p className="applied-job-card__company">
        <strong>{company}</strong>
        <span aria-hidden="true">·</span>
        <span>{location}</span>
      </p>

      <dl className="applied-job-card__facts">
        <div>
          <dt>Employment</dt>
          <dd>{employmentType}</dd>
        </div>
        <div>
          <dt>Salary</dt>
          <dd>{salary}</dd>
        </div>
        <div>
          <dt>Added</dt>
          <dd>
            <time dateTime={posting.sourceReceivedAt}>
              {formatApplicationDate(posting.sourceReceivedAt)}
            </time>
          </dd>
        </div>
      </dl>

      <a className="applied-job-card__link" href={detailsUrl}>
        Open job details
        <span aria-hidden="true">→</span>
      </a>
    </article>
  );
}
