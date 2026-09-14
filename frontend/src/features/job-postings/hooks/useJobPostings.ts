import { useCallback, useEffect, useState } from "react";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import {
  createManualJobPosting,
  getAllJobPostings,
  updateJobPostingStatus,
} from "../api/jobPostingsApi";
import type { CreateManualJobPostingInput, JobPosting } from "../types/jobPosting";
import { updateWorkflowStatus } from "../utils/updateWorkflowStatus";

export function useJobPostings() {
  const [data, setData] = useState<JobPosting[]>([]);
  const [createError, setCreateError] = useState<string>();
  const [error, setError] = useState<string>();
  const [isCreating, setIsCreating] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [updatingIds, setUpdatingIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    const abortController = new AbortController();

    getAllJobPostings(abortController.signal)
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
      setData((current) => updateWorkflowStatus(current, id, status));
      return true;
    } catch (requestError) {
      setError(getErrorMessage(requestError));
      return false;
    } finally {
      setUpdatingIds((current) => {
        const next = new Set(current);
        next.delete(id);
        return next;
      });
    }
  }, []);

  const createPosting = useCallback(async (input: CreateManualJobPostingInput) => {
    setCreateError(undefined);
    setIsCreating(true);

    try {
      const created = await createManualJobPosting(input);
      setData((current) => [created, ...current.filter((posting) => posting.id !== created.id)]);
      return created;
    } catch (requestError) {
      setCreateError(getErrorMessage(requestError));
      return undefined;
    } finally {
      setIsCreating(false);
    }
  }, []);

  return {
    data,
    createError,
    error,
    isCreating,
    isLoading,
    updatingIds,
    changeStatus,
    clearCreateError: () => setCreateError(undefined),
    createPosting,
  };
}
