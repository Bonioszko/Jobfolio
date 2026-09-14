import type { InterviewProcess } from "../types/interviewDashboard";
import { displayValue } from "../../job-postings/utils/displayValue";
import { formatInterviewDate } from "../utils/interviewDashboard";

type InterviewProcessCardProps = {
  process: InterviewProcess;
};

export function InterviewProcessCard({ process }: InterviewProcessCardProps) {
  const { latestInterview, notes, posting } = process;
  const company = displayValue(posting.parsedData.company);
  const location = displayValue(posting.parsedData.location);

  return (
    <article className="interview-process-card">
      <header className="interview-process-card__header">
        <div className="company-monogram" aria-hidden="true">
          {companyInitials(company)}
        </div>
        <div>
          <span>{company}</span>
          <h2>{posting.displayTitle}</h2>
        </div>
        <span className="interviewing-label">Interviewing</span>
      </header>

      <div className="interview-process-card__body">
        <section aria-label={`Latest interview for ${posting.displayTitle}`}>
          <span className="dashboard-label">LAST INTERVIEW</span>
          {latestInterview ? (
            <>
              <div className="last-interview-heading">
                <strong>{latestInterview.stage}</strong>
                <time dateTime={latestInterview.interviewDate}>
                  {formatInterviewDate(latestInterview.interviewDate)}
                </time>
              </div>
              <p>{latestInterview.notes}</p>
            </>
          ) : (
            <div className="no-interview-history">
              <strong>No interview recorded yet</strong>
              <span>Add the first stage from the job details.</span>
            </div>
          )}
        </section>

        <dl className="interview-process-facts">
          <div>
            <dt>Location</dt>
            <dd>{location}</dd>
          </div>
          <div>
            <dt>Source</dt>
            <dd>{posting.sourceKey}</dd>
          </div>
          <div>
            <dt>Interviews</dt>
            <dd>{notes.length}</dd>
          </div>
        </dl>
      </div>

      <a
        className="interview-process-link"
        href={`/jobs?job=${encodeURIComponent(posting.id)}`}
      >
        Open job and interview history
        <span aria-hidden="true">→</span>
      </a>
    </article>
  );
}

function companyInitials(company: string) {
  const initials = company
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toLocaleUpperCase();

  return initials || "—";
}
