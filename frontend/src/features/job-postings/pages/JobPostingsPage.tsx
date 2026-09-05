import { useEffect, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { Session } from "../../auth/types/session";
import { useCandidateRules } from "../../candidate-rules/hooks/useCandidateRules";
import { useCvTemplates } from "../../cv-templates/hooks/useCvTemplates";
import type { DomainConfig } from "../../domain-config/types/domainConfig";
import { JobPostingDetailsPanel } from "../components/JobPostingDetailsPanel";
import { JobPostingsList } from "../components/JobPostingsList";
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

  const error = postings.error ?? templates.error ?? rules.error;
  const sessionLabel = session?.mode.toLowerCase() === "demo" ? "Demo session" : "Workspace";
  const selectedPosting = postings.data.find((posting) => posting.id === selectedPostingId);

  useEffect(() => {
    if (postings.isLoading) return;

    if (postings.data.length === 0) {
      setSelectedPostingId(undefined);
    } else if (!postings.data.some((posting) => posting.id === selectedPostingId)) {
      setSelectedPostingId(postings.data[0].id);
    }
  }, [postings.data, postings.isLoading, selectedPostingId]);

  return (
    <main className="app-shell">
      <header className="app-header">
        <div className="brand-lockup">
          <span className="brand-mark" aria-hidden="true">J</span>
          <div>
            <span className="brand-name">Jobfolio</span>
            <span className="brand-context">Application workspace</span>
          </div>
        </div>
        <div className="workspace-meta">
          <span>{postings.data.length} opportunities</span>
          <span className="workspace-badge">{sessionLabel}</span>
        </div>
      </header>
      <ErrorMessage className="banner" message={error} />
      <section className="workspace-layout">
        {postings.isLoading ? (
          <div className="jobs-index index-loading">Loading opportunities…</div>
        ) : (
          <JobPostingsList
            activeId={selectedPostingId}
            onSelect={setSelectedPostingId}
            postings={postings.data}
            statuses={domain.statuses}
          />
        )}
        <JobPostingDetailsPanel
          documentName={domain.generatedDocument.singular}
          fields={domain.fields}
          isStatusUpdating={Boolean(
            selectedPostingId && postings.updatingIds.has(selectedPostingId)
          )}
          isTailoringDataLoading={templates.isLoading || rules.isLoading}
          onStatusChange={(status) =>
            selectedPostingId ? postings.changeStatus(selectedPostingId, status) : Promise.resolve()
          }
          postingId={selectedPostingId}
          rules={rules.data}
          sourceItemName={domain.sourceItem.singular}
          statuses={domain.statuses}
          templates={templates.data}
          workflowStatus={selectedPosting?.workflowStatus}
        />
      </section>
    </main>
  );
}
