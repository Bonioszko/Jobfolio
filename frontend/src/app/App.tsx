import { ErrorMessage } from "../components/common/ErrorMessage";
import { useSession } from "../features/auth/hooks/useSession";
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
        error={demoSession.error ?? session.error}
        isStarting={demoSession.isStarting}
        onStartDemo={demoSession.start}
      />
    );
  }

  return <JobPostingsPage domain={domainConfig.data} session={session.session} />;
}
