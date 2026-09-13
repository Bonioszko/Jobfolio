import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { DomainConfig } from "../../domain-config/types/domainConfig";

type DemoLandingProps = {
  demoEnabled: boolean;
  demoPolicy: DomainConfig["demoPolicy"];
  error?: string;
  googleSignInUrl: string;
  isStarting: boolean;
  onStartDemo: () => Promise<void>;
};

export function DemoLanding({
  demoEnabled,
  demoPolicy,
  error,
  googleSignInUrl,
  isStarting,
  onStartDemo,
}: DemoLandingProps) {
  return (
    <main className="landing">
      <section className="landing-shell">
        <header className="landing-intro">
          <span className="eyebrow">JOB EMAIL → APPLICATION TRACKING → TAILORED CV</span>
          <h1>Run your job search from one focused workspace.</h1>
          <p>
            Jobfolio turns job-alert emails into a searchable application pipeline, then helps
            tailor and compile a versioned CV for the role.
          </p>
        </header>

        <div className="landing-options">
          {demoEnabled && (
            <article className="access-card access-card--demo">
              <div className="access-card__heading">
                <span className="access-card__label">Portfolio demo</span>
                <span className="access-card__badge">No account needed</span>
              </div>
              <h2>Explore the working product</h2>
              <p>
                Start with realistic sample job alerts in a private, temporary workspace.
              </p>
              <ul>
                <li>Browse jobs and manage the application and interview workflow</li>
                <li>Edit CV templates and verified candidate rules</li>
                <li>Tailor a CV with cost-free deterministic demo generation</li>
                <li>Compile and download a real PDF while shared capacity is available</li>
              </ul>
              <div className="demo-limits">
                <strong>Cost-safe public demo</strong>
                <span>
                  Sessions last {demoPolicy.sessionLifetimeHours} hours. PDF compilation is
                  limited to {demoPolicy.maxCompilationJobsPerWindow} attempts across the demo
                  every {demoPolicy.compilationWindowMinutes} minutes. Gmail stays disconnected
                  and no paid AI is called.
                </span>
              </div>
              <button
                className="landing-primary-action"
                disabled={isStarting}
                onClick={() => void onStartDemo()}
                type="button"
              >
                {isStarting ? "Preparing your workspace…" : "Explore live demo"}
              </button>
            </article>
          )}

          <article className="access-card">
            <div className="access-card__heading">
              <span className="access-card__label">Personal workspace</span>
            </div>
            <h2>Continue with your own data</h2>
            <p>
              Sign in as an approved owner to use your private job history, Gmail imports, and
              configured CV generation workflow.
            </p>
            <ul>
              <li>Private, persistent workspace</li>
              <li>Gmail ingestion configured outside the browser</li>
              <li>Real CV generation and compilation when enabled</li>
            </ul>
            <a className="google-sign-in" href={googleSignInUrl}>Sign in with Google</a>
            <small>Access is restricted to allowlisted Google accounts.</small>
          </article>
        </div>

        <div aria-live="polite" className="landing-error">
          <ErrorMessage message={error} />
        </div>
      </section>
    </main>
  );
}
