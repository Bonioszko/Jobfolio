import { ErrorMessage } from "../../../components/common/ErrorMessage";
import { AppHeader } from "../../../components/layout/AppHeader";
import type { Session } from "../../auth/types/session";
import type { DomainConfig } from "../../domain-config/types/domainConfig";
import { useJobPostings } from "../../job-postings/hooks/useJobPostings";
import { ApplicationKanbanBoard } from "../components/ApplicationKanbanBoard";

type ApplicationKanbanPageProps = {
  authenticationError?: string;
  domain: DomainConfig;
  googleSignInUrl: string;
  isLoggingOut: boolean;
  onLogout: () => Promise<void>;
  session?: Session;
};

export function ApplicationKanbanPage({
  authenticationError,
  domain,
  googleSignInUrl,
  isLoggingOut,
  onLogout,
  session,
}: ApplicationKanbanPageProps) {
  const postings = useJobPostings();
  const error = postings.error ?? authenticationError;

  return (
    <main className="app-shell kanban-shell">
      <AppHeader
        activePage="kanban"
        googleSignInUrl={googleSignInUrl}
        isLoggingOut={isLoggingOut}
        onLogout={onLogout}
        session={session}
        summary={postings.isLoading
          ? "Loading applications…"
          : `${postings.data.length} applications`}
      />
      <ErrorMessage className="banner" message={error} />

      <section className="kanban-page" aria-labelledby="kanban-page-title">
        <header className="kanban-page__intro">
          <div>
            <span className="section-kicker">APPLICATION PIPELINE</span>
            <h1 id="kanban-page-title">Application Kanban</h1>
          </div>
          <p>Drag an opportunity to a new stage, or use its stage selector.</p>
        </header>

        {postings.isLoading ? (
          <div className="kanban-loading" aria-label="Loading application board">
            Loading application board…
          </div>
        ) : postings.error ? null : postings.data.length === 0 ? (
          <div className="kanban-empty">
            <strong>No applications to track yet</strong>
            <p>Imported job opportunities will appear here automatically.</p>
          </div>
        ) : (
          <ApplicationKanbanBoard
            onStatusChange={postings.changeStatus}
            postings={postings.data}
            statuses={domain.statuses}
            updatingIds={postings.updatingIds}
          />
        )}
      </section>
    </main>
  );
}
