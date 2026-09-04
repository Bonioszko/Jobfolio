import { useEffect, useState } from "react";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import { getJobPosting } from "../api/jobPostingsApi";
import type { JobPosting } from "../types/jobPosting";

export function useJobPosting(id?: string) {
  const [data, setData] = useState<JobPosting>();
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    setData(undefined);
    setError(undefined);

    if (!id) {
      setIsLoading(false);
      return;
    }

    const abortController = new AbortController();
    setIsLoading(true);

    getJobPosting(id, abortController.signal)
      .then(setData)
      .catch((requestError: unknown) => {
        if (!isAbortError(requestError)) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (!abortController.signal.aborted) setIsLoading(false);
      });

    return () => abortController.abort();
  }, [id]);

  return { data, error, isLoading };
}
