import { useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { Session } from "../../auth/types/session";
import { useCandidateRules } from "../../candidate-rules/hooks/useCandidateRules";
import { useCvTemplates } from "../../cv-templates/hooks/useCvTemplates";
import type { DomainConfig } from "../../domain-config/types/domainConfig";
import { JobPostingDetailsPanel } from "../components/JobPostingDetailsPanel";
import { JobPostingsTable } from "../components/JobPostingsTable";
import { useJobPostings } from "../hooks/useJobPostings";

type JobPostingsPageProps = {
  domain: DomainConfig;
  session?: Session;
};

export function JobPostingsPage({ domain, session }: JobPostingsPageProps) {
  const [selectedPostingId, setSelectedPostingId] = useState<string>();
  const postings = useJobPostings();
  const templates = useCvTemplates();
  const rules = useCandidateRules();

  const isLoading = postings.isLoading || templates.isLoading || rules.isLoading;
  const error = postings.error ?? templates.error ?? rules.error;
  const sessionLabel = session?.mode.toLowerCase() === "demo" ? "Demo session" : "Workspace";

  return (
    <main>
      <header>
        <div>
          <span className="eyebrow">WORKSPACE</span>
          <h1>{domain.sourceItem.plural}</h1>
        </div>
        <span className="demo">{sessionLabel}</span>
      </header>
      <ErrorMessage className="banner" message={error} />
      <section className="grid">
        {isLoading ? (
          <div className="card empty-state">Loading workspace…</div>
        ) : (
          <JobPostingsTable
            fields={domain.fields}
            onSelect={setSelectedPostingId}
            onStatusChange={postings.changeStatus}
            postings={postings.data}
            statuses={domain.statuses}
            updatingIds={postings.updatingIds}
          />
        )}
        <JobPostingDetailsPanel
          documentName={domain.generatedDocument.singular}
          postingId={selectedPostingId}
          rules={rules.data}
          sourceItemName={domain.sourceItem.singular}
          templates={templates.data}
        />
      </section>
    </main>
  );
}
