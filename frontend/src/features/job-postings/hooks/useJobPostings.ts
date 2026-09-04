import { useCallback, useEffect, useState } from "react";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import { getJobPostings, updateJobPostingStatus } from "../api/jobPostingsApi";
import type { JobPosting } from "../types/jobPosting";

export function useJobPostings() {
  const [data, setData] = useState<JobPosting[]>([]);
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [updatingIds, setUpdatingIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    const abortController = new AbortController();

    getJobPostings(abortController.signal)
      .then(setData)
      .catch((requestError: unknown) => {
        if (!isAbortError(requestError)) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (!abortController.signal.aborted) setIsLoading(false);
      });

    return () => abortController.abort();
  }, []);

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

  return { data, error, isLoading, updatingIds, changeStatus };
}
