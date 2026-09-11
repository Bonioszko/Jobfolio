import { ErrorMessage } from "../components/common/ErrorMessage";
import { useSession } from "../features/auth/hooks/useSession";
import { googleSignInUrl } from "../features/auth/api/sessionApi";
import { getAuthenticationError } from "../features/auth/utils/authenticationError";
import { useDemoSession } from "../features/demo/hooks/useDemoSession";
import { DemoLanding } from "../features/demo/components/DemoLanding";
import { useDomainConfig } from "../features/domain-config/hooks/useDomainConfig";
import { JobPostingsPage } from "../features/job-postings/pages/JobPostingsPage";

export function App() {
  const domainConfig = useDomainConfig();
  const session = useSession();
  const demoSession = useDemoSession(session.authenticate);

  if (domainConfig.isLoading || session.status === "checking") {
    return <main className="center">Loading…</main>;
  }

  if (!domainConfig.data) {
    return (
      <main className="center">
        <ErrorMessage
          message={domainConfig.error ?? "Unable to load application configuration."}
        />
      </main>
    );
  }

  if (session.status === "anonymous") {
    return (
      <DemoLanding
        demoEnabled={domainConfig.data.features.demo}
        error={demoSession.error ?? session.error ?? getAuthenticationError()}
        googleSignInUrl={googleSignInUrl}
        isStarting={demoSession.isStarting}
        onStartDemo={demoSession.start}
      />
    );
  }

  return (
    <JobPostingsPage
      authenticationError={session.error}
      domain={domainConfig.data}
      googleSignInUrl={googleSignInUrl}
      isLoggingOut={session.isLoggingOut}
      onLogout={session.logout}
      session={session.session}
    />
  );
}
