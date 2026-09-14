import type { Session } from "../../features/auth/types/session";

type AppHeaderProps = {
  activePage: "dashboard" | "jobs" | "kanban" | "interviews";
  googleSignInUrl: string;
  isLoggingOut: boolean;
  onLogout: () => Promise<void>;
  session?: Session;
  summary: string;
};

export function AppHeader({
  activePage,
  googleSignInUrl,
  isLoggingOut,
  onLogout,
  session,
  summary,
}: AppHeaderProps) {
  const sessionLabel = session?.mode.toLowerCase() === "demo" ? "Demo session" : "Workspace";

  return (
    <header className="app-header">
      <a className="brand-lockup" href="/" aria-label="Jobfolio home">
        <span className="brand-mark" aria-hidden="true">J</span>
        <div>
          <span className="brand-name">Jobfolio</span>
          <span className="brand-context">Application workspace</span>
        </div>
      </a>

      <nav className="app-navigation" aria-label="Primary navigation">
        <a
          aria-current={activePage === "dashboard" ? "page" : undefined}
          href="/"
        >
          Dashboard
        </a>
        <a aria-current={activePage === "jobs" ? "page" : undefined} href="/jobs">
          Jobs
        </a>
        <a
          aria-current={activePage === "kanban" ? "page" : undefined}
          href="/kanban"
        >
          Kanban
        </a>
        <a
          aria-current={activePage === "interviews" ? "page" : undefined}
          href="/interviews"
        >
          Interviews
        </a>
      </nav>

      <div className="workspace-meta">
        <span>{summary}</span>
        <span className="workspace-badge">{sessionLabel}</span>
        {session?.mode.toLowerCase() === "demo" ? (
          <a className="workspace-auth-link" href={googleSignInUrl}>Sign in</a>
        ) : (
          <button
            className="workspace-auth-link"
            disabled={isLoggingOut}
            onClick={() => void onLogout()}
          >
            {isLoggingOut ? "Signing out…" : "Sign out"}
          </button>
        )}
      </div>
    </header>
  );
}
