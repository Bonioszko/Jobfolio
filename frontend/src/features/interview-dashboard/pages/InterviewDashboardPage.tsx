import { useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import { AppHeader } from "../../../components/layout/AppHeader";
import type { Session } from "../../auth/types/session";
import type { DomainConfig } from "../../domain-config/types/domainConfig";
import { InterviewProcessCard } from "../components/InterviewProcessCard";
import { ManageInterviewProcessesDialog } from "../components/ManageInterviewProcessesDialog";
import { useInterviewDashboard } from "../hooks/useInterviewDashboard";
import { formatInterviewDate } from "../utils/interviewDashboard";

type InterviewDashboardPageProps = {
  authenticationError?: string;
  domain: DomainConfig;
  googleSignInUrl: string;
  isLoggingOut: boolean;
  onLogout: () => Promise<void>;
  session?: Session;
};

export function InterviewDashboardPage({
  authenticationError,
  domain,
  googleSignInUrl,
  isLoggingOut,
  onLogout,
  session,
}: InterviewDashboardPageProps) {
  const dashboard = useInterviewDashboard();
  const [isManagerOpen, setIsManagerOpen] = useState(false);
  const interviewCount = dashboard.data.reduce(
    (total, process) => total + process.notes.length,
    0,
  );
  const latestInterview = dashboard.data.find((process) => process.latestInterview)
    ?.latestInterview;

  return (
    <main className="app-shell interview-dashboard-shell">
      <AppHeader
        activePage="interviews"
        googleSignInUrl={googleSignInUrl}
        isLoggingOut={isLoggingOut}
        onLogout={onLogout}
        session={session}
        summary={dashboard.isLoading
          ? "Loading processes…"
          : `${dashboard.data.length} active processes`}
      />
      <ErrorMessage
        className="banner"
        message={dashboard.error ?? authenticationError}
      />

      <section className="interview-dashboard" aria-labelledby="interview-dashboard-title">
        <header className="interview-dashboard__intro">
          <div>
            <span className="section-kicker">CURRENT PIPELINE</span>
            <h1 id="interview-dashboard-title">Ongoing interviews</h1>
          </div>
          <div className="interview-dashboard__actions">
            <p>Recent conversations and notes from every active interview process.</p>
            <button
              className="primary-action manage-processes-button"
              disabled={dashboard.isLoading || Boolean(dashboard.error)}
              onClick={() => setIsManagerOpen(true)}
              type="button"
            >
              Manage processes
            </button>
          </div>
        </header>

        {!dashboard.isLoading && dashboard.data.length > 0 && (
          <dl className="interview-dashboard-summary">
            <div>
              <dt>Active processes</dt>
              <dd>{dashboard.data.length}</dd>
            </div>
            <div>
              <dt>Interviews recorded</dt>
              <dd>{interviewCount}</dd>
            </div>
            <div>
              <dt>Latest interview</dt>
              <dd>
                {latestInterview
                  ? formatInterviewDate(latestInterview.interviewDate)
                  : "Not recorded"}
              </dd>
            </div>
          </dl>
        )}

        {dashboard.isLoading ? (
          <div className="interview-dashboard-loading" aria-label="Loading interview dashboard">
            <span />
            <span />
            <span />
          </div>
        ) : dashboard.error ? null : dashboard.data.length === 0 ? (
          <div className="interview-dashboard-empty">
            <span className="interview-dashboard-empty__mark" aria-hidden="true">✓</span>
            <strong>No ongoing interview processes</strong>
            <p>Jobs moved to Interviewing will appear here automatically.</p>
            <button
              className="primary-action empty-manage-button"
              onClick={() => setIsManagerOpen(true)}
              type="button"
            >
              Add interview process
            </button>
          </div>
        ) : (
          <div className="interview-process-grid">
            {dashboard.data.map((process) => (
              <InterviewProcessCard key={process.posting.id} process={process} />
            ))}
          </div>
        )}
      </section>

      <ManageInterviewProcessesDialog
        error={dashboard.statusError}
        isOpen={isManagerOpen}
        onClose={() => setIsManagerOpen(false)}
        onStatusChange={dashboard.changeStatus}
        postings={dashboard.postings}
        processes={dashboard.data}
        statuses={domain.statuses}
        updatingIds={dashboard.updatingIds}
      />
    </main>
  );
}
