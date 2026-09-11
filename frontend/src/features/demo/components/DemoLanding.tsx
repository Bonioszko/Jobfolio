import { ErrorMessage } from "../../../components/common/ErrorMessage";

type DemoLandingProps = {
  demoEnabled: boolean;
  error?: string;
  googleSignInUrl: string;
  isStarting: boolean;
  onStartDemo: () => Promise<void>;
};

export function DemoLanding({
  demoEnabled,
  error,
  googleSignInUrl,
  isStarting,
  onStartDemo,
}: DemoLandingProps) {
  return (
    <main className="landing">
      <div>
        <span className="eyebrow">JOB EMAIL → APPLICATION TRACKING</span>
        <h1>Turn job alerts into an organized job search.</h1>
        <p>
          Parse job posts from multiple email providers and track every opportunity in one place.
        </p>
        <div className="landing-actions">
          <a className="google-sign-in" href={googleSignInUrl}>Sign in with Google</a>
          {demoEnabled && (
            <button disabled={isStarting} onClick={() => void onStartDemo()}>
              {isStarting ? "Starting demo…" : "Try Demo"}
            </button>
          )}
        </div>
        <small>
          Google sign-in is limited to approved accounts
          {demoEnabled ? " · demo sessions last 6 hours" : ""}
        </small>
        <ErrorMessage message={error} />
      </div>
    </main>
  );
}
