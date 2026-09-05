import { ErrorMessage } from "../../../components/common/ErrorMessage";

type DemoLandingProps = {
  error?: string;
  googleSignInUrl: string;
  isStarting: boolean;
  onStartDemo: () => Promise<void>;
};

export function DemoLanding({ error, googleSignInUrl, isStarting, onStartDemo }: DemoLandingProps) {
  return (
    <main className="landing">
      <div>
        <span className="eyebrow">JOB EMAIL → TAILORED CV → PDF</span>
        <h1>Turn job alerts into focused applications.</h1>
        <p>
          Parse job posts from multiple email providers, track every opportunity, and tailor a
          versioned CV for the role.
        </p>
        <div className="landing-actions">
          <a className="google-sign-in" href={googleSignInUrl}>Sign in with Google</a>
          <button disabled={isStarting} onClick={() => void onStartDemo()}>
            {isStarting ? "Starting demo…" : "Try Demo"}
          </button>
        </div>
        <small>Google sign-in is limited to approved accounts · demo sessions last 6 hours</small>
        <ErrorMessage message={error} />
      </div>
    </main>
  );
}
