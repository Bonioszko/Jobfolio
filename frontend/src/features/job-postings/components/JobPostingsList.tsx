import { useMemo, useState } from "react";
import type { WorkflowStatus } from "../../domain-config/types/domainConfig";
import type { JobPosting } from "../types/jobPosting";
import { displayValue } from "../utils/displayValue";
import { statusTone } from "../utils/statusPresentation";

type JobPostingsListProps = {
  activeId?: string;
  hasMore: boolean;
  isLoadingMore: boolean;
  postings: JobPosting[];
  statuses: WorkflowStatus[];
  onLoadMore: () => Promise<void>;
  onSelect: (id: string) => void;
};

export function JobPostingsList({
  activeId,
  hasMore,
  isLoadingMore,
  postings,
  statuses,
  onLoadMore,
  onSelect,
}: JobPostingsListProps) {
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("ALL");
  const statusLabels = new Map(statuses.map((item) => [item.code, item.label]));
  const visiblePostings = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase();

    return postings.filter((posting) => {
      if (status !== "ALL" && posting.workflowStatus !== status) return false;
      if (!normalizedQuery) return true;

      const searchableText = [
        posting.displayTitle,
        displayValue(posting.parsedData.company),
        displayValue(posting.parsedData.location),
      ]
        .join(" ")
        .toLocaleLowerCase();

      return searchableText.includes(normalizedQuery);
    });
  }, [postings, query, status]);

  return (
    <section className="jobs-index" aria-label="Job postings">
      <div className="jobs-index__header">
        <div>
          <span className="section-kicker">OPPORTUNITIES</span>
          <h2>All jobs</h2>
        </div>
        <span className="result-count">{visiblePostings.length}</span>
      </div>

      <div className="jobs-toolbar">
        <label className="search-field">
          <span className="visually-hidden">Search jobs</span>
          <svg aria-hidden="true" viewBox="0 0 24 24">
            <circle cx="11" cy="11" r="6.5" />
            <path d="m16 16 4 4" />
          </svg>
          <input
            type="search"
            placeholder="Search role or company"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </label>
        <label className="filter-field">
          <span className="visually-hidden">Filter by status</span>
          <select value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="ALL">All states</option>
            {statuses.map((item) => (
              <option key={item.code} value={item.code}>
                {item.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {postings.length === 0 ? (
        <div className="index-empty">
          <strong>No job postings yet</strong>
          <span>Imported opportunities will appear here.</span>
        </div>
      ) : visiblePostings.length === 0 ? (
        <div className="index-empty">
          <strong>No matching jobs</strong>
          <span>Try a different search or state.</span>
        </div>
      ) : (
        <ol className="job-list">
          {visiblePostings.map((posting) => {
            const company = displayValue(posting.parsedData.company);
            const location = displayValue(posting.parsedData.location);

            return (
              <li key={posting.id}>
                <button
                  className="job-list-item"
                  data-active={posting.id === activeId}
                  onClick={() => onSelect(posting.id)}
                  type="button"
                >
                  <span className="job-list-item__topline">
                    <span className="company-name">{company}</span>
                    <span
                      className="status-mark"
                      data-tone={statusTone(posting.workflowStatus)}
                    >
                      {statusLabels.get(posting.workflowStatus) ?? posting.workflowStatus}
                    </span>
                  </span>
                  <strong>{posting.displayTitle}</strong>
                  <span className="job-list-item__meta">
                    <span>{location}</span>
                    <span aria-hidden="true">·</span>
                    <time dateTime={posting.sourceReceivedAt}>
                      {formatReceivedDate(posting.sourceReceivedAt)}
                    </time>
                  </span>
                </button>
              </li>
            );
          })}
        </ol>
      )}
      {hasMore ? (
        <div className="job-pagination">
          <button
            disabled={isLoadingMore}
            onClick={() => void onLoadMore()}
            type="button"
          >
            {isLoadingMore ? "Loading…" : "Load more"}
          </button>
        </div>
      ) : null}
    </section>
  );
}

function formatReceivedDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    day: "numeric",
    month: "short",
  }).format(new Date(value));
}
