import { useMemo } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import { AppHeader } from "../../../components/layout/AppHeader";
import type { Session } from "../../auth/types/session";
import type { DomainConfig } from "../../domain-config/types/domainConfig";
import { useJobPostings } from "../../job-postings/hooks/useJobPostings";
import { AppliedJobCard } from "../components/AppliedJobCard";
import {
  getAppliedJobPostings,
  summarizeApplications,
} from "../utils/applicationDashboard";

type ApplicationDashboardPageProps = {
  authenticationError?: string;
  domain: DomainConfig;
  googleSignInUrl: string;
  isLoggingOut: boolean;
  onLogout: () => Promise<void>;
  session?: Session;
};

export function ApplicationDashboardPage({
  authenticationError,
  domain,
  googleSignInUrl,
  isLoggingOut,
  onLogout,
  session,
}: ApplicationDashboardPageProps) {
  const postings = useJobPostings();
  const applications = useMemo(
    () => getAppliedJobPostings(postings.data),
    [postings.data],
  );
  const summary = useMemo(
    () => summarizeApplications(applications),
    [applications],
  );
  const statusLabels = new Map(
    domain.statuses.map((status) => [status.code, status.label]),
  );

  return (
    <main className="app-shell application-dashboard-shell">
      <AppHeader
        activePage="dashboard"
        googleSignInUrl={googleSignInUrl}
        isLoggingOut={isLoggingOut}
        onLogout={onLogout}
        session={session}
        summary={postings.isLoading
          ? "Loading applications…"
          : `${summary.total} submitted applications`}
      />
      <ErrorMessage
        className="banner"
        message={postings.error ?? authenticationError}
      />

      <section className="application-dashboard" aria-labelledby="application-dashboard-title">
        <header className="application-dashboard__intro">
          <div>
            <span className="section-kicker">AT A GLANCE</span>
            <h1 id="application-dashboard-title">Your applications</h1>
            <p>
              Every job you’ve submitted, including applications that moved to
              interviews or have closed.
            </p>
          </div>
          <a className="primary-action application-dashboard__browse" href="/jobs">
            Browse all jobs
          </a>
        </header>

        {!postings.isLoading && !postings.error && (
          <dl className="application-dashboard__summary">
            <div>
              <dt>Total applied</dt>
              <dd>{summary.total}</dd>
            </div>
            <div>
              <dt>Awaiting response</dt>
              <dd>{summary.awaitingResponse}</dd>
            </div>
            <div>
              <dt>Interviewing</dt>
              <dd>{summary.interviewing}</dd>
            </div>
            <div>
              <dt>Closed</dt>
              <dd>{summary.closed}</dd>
            </div>
          </dl>
        )}

        {postings.isLoading ? (
          <div className="application-dashboard__loading" aria-label="Loading applications">
            <span />
            <span />
            <span />
          </div>
        ) : postings.error ? null : applications.length === 0 ? (
          <div className="application-dashboard__empty">
            <span aria-hidden="true">✓</span>
            <strong>No submitted applications yet</strong>
            <p>Move a job to Applied and it will appear on this dashboard.</p>
            <a className="primary-action" href="/jobs">Find a job to apply to</a>
          </div>
        ) : (
          <section className="application-dashboard__applications" aria-labelledby="all-applications-title">
            <header>
              <div>
                <span className="section-kicker">APPLICATIONS</span>
                <h2 id="all-applications-title">All submitted jobs</h2>
              </div>
              <span className="result-count">{applications.length}</span>
            </header>
            <div className="applied-job-grid">
              {applications.map((posting) => (
                <AppliedJobCard
                  key={posting.id}
                  posting={posting}
                  statusLabel={statusLabels.get(posting.workflowStatus) ?? posting.workflowStatus}
                />
              ))}
            </div>
          </section>
        )}
      </section>
    </main>
  );
}
