import { useCallback, useEffect, useState } from "react";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import { getJobPostings, updateJobPostingStatus } from "../api/jobPostingsApi";
import type { JobPosting } from "../types/jobPosting";

export function useJobPostings() {
  const [data, setData] = useState<JobPosting[]>([]);
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [updatingIds, setUpdatingIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    const abortController = new AbortController();

    getJobPostings(undefined, abortController.signal)
      .then((page) => {
        setData(page.items);
        setNextCursor(page.nextCursor);
      })
      .catch((requestError: unknown) => {
        if (!isAbortError(requestError)) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (!abortController.signal.aborted) setIsLoading(false);
      });

    return () => abortController.abort();
  }, []);

  const loadMore = useCallback(async () => {
    if (!nextCursor || isLoadingMore) return;

    setError(undefined);
    setIsLoadingMore(true);
    try {
      const page = await getJobPostings(nextCursor);
      setData((current) => {
        const loadedIds = new Set(current.map((posting) => posting.id));
        return [
          ...current,
          ...page.items.filter((posting) => !loadedIds.has(posting.id)),
        ];
      });
      setNextCursor(page.nextCursor);
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    } finally {
      setIsLoadingMore(false);
    }
  }, [isLoadingMore, nextCursor]);

  const changeStatus = useCallback(async (id: string, status: string) => {
    setError(undefined);
    setUpdatingIds((current) => new Set(current).add(id));

    try {
      await updateJobPostingStatus(id, status);
      setData((current) =>
        current.map((posting) =>
          posting.id === id ? { ...posting, workflowStatus: status } : posting,
        ),
      );
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    } finally {
      setUpdatingIds((current) => {
        const next = new Set(current);
        next.delete(id);
        return next;
      });
    }
  }, []);

  return {
    data,
    error,
    hasMore: Boolean(nextCursor),
    isLoading,
    isLoadingMore,
    updatingIds,
    changeStatus,
    loadMore,
  };
}
