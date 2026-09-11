import { useCallback, useEffect, useRef, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import { AppHeader } from "../../../components/layout/AppHeader";
import type { Session } from "../../auth/types/session";
import { useCandidateRules } from "../../candidate-rules/hooks/useCandidateRules";
import { useCvTemplates } from "../../cv-templates/hooks/useCvTemplates";
import type { DomainConfig } from "../../domain-config/types/domainConfig";
import { JobPostingDetailsPanel } from "../components/JobPostingDetailsPanel";
import { JobPostingsList } from "../components/JobPostingsList";
import { useJobPostings } from "../hooks/useJobPostings";

type JobPostingsPageProps = {
  authenticationError?: string;
  domain: DomainConfig;
  googleSignInUrl: string;
  isLoggingOut: boolean;
  onLogout: () => Promise<void>;
  session?: Session;
};

export function JobPostingsPage({
  authenticationError,
  domain,
  googleSignInUrl,
  isLoggingOut,
  onLogout,
  session,
}: JobPostingsPageProps) {
  const [selectedPostingId, setSelectedPostingId] = useState<string | undefined>(() =>
    new URLSearchParams(window.location.search).get("job") ?? undefined,
  );
  const visiblePostingIds = useRef<string[]>([]);
  const postings = useJobPostings();
  const templates = useCvTemplates(domain.features.cv);
  const rules = useCandidateRules(domain.features.cv);

  const error = postings.error ?? templates.error ?? rules.error ?? authenticationError;
  const selectedPosting = postings.data.find((posting) => posting.id === selectedPostingId);

  useEffect(() => {
    if (postings.isLoading) return;

    if (postings.data.length === 0) {
      setSelectedPostingId(undefined);
    } else if (!postings.data.some((posting) => posting.id === selectedPostingId)) {
      setSelectedPostingId(
        postings.data.find((posting) => posting.workflowStatus === "NEW")?.id ??
          postings.data.find((posting) => posting.workflowStatus === "TO_APPLY")?.id ??
          postings.data[0].id,
      );
    }
  }, [postings.data, postings.isLoading, selectedPostingId]);

  const handleVisibleOrderChange = useCallback((ids: string[]) => {
    visiblePostingIds.current = ids;
  }, []);

  const handleStatusChange = async (status: string) => {
    const postingId = selectedPostingId;
    if (!postingId) return;

    const currentIndex = visiblePostingIds.current.indexOf(postingId);
    const nextPostingId = currentIndex >= 0
      ? visiblePostingIds.current[currentIndex + 1]
      : undefined;
    const succeeded = await postings.changeStatus(postingId, status);

    if (succeeded && nextPostingId) {
      setSelectedPostingId((current) => current === postingId ? nextPostingId : current);
    }
  };

  return (
    <main className="app-shell">
      <AppHeader
        activePage="jobs"
        googleSignInUrl={googleSignInUrl}
        isLoggingOut={isLoggingOut}
        onLogout={onLogout}
        session={session}
        summary={`${postings.data.length} opportunities`}
      />
      <ErrorMessage className="banner" message={error} />
      <section className="workspace-layout">
        {postings.isLoading ? (
          <div className="jobs-index index-loading">Loading opportunities…</div>
        ) : (
          <JobPostingsList
            activeId={selectedPostingId}
            onSelect={setSelectedPostingId}
            onVisibleOrderChange={handleVisibleOrderChange}
            postings={postings.data}
            statuses={domain.statuses}
          />
        )}
        <JobPostingDetailsPanel
          cvEnabled={domain.features.cv}
          documentName={domain.generatedDocument.singular}
          fields={domain.fields}
          isStatusUpdating={Boolean(
            selectedPostingId && postings.updatingIds.has(selectedPostingId)
          )}
          isTailoringDataLoading={templates.isLoading || rules.isLoading}
          isTemplateSaving={templates.isSaving}
          onStatusChange={handleStatusChange}
          onTemplateSave={templates.saveTemplate}
          postingId={selectedPostingId}
          rules={rules.data}
          sourceItemName={domain.sourceItem.singular}
          statuses={domain.statuses}
          templates={templates.data}
          templateSaveError={templates.saveError}
          workflowStatus={selectedPosting?.workflowStatus}
        />
      </section>
    </main>
  );
}
