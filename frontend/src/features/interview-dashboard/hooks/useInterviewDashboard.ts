import { useCallback, useEffect, useState } from "react";
import { getInterviewNotes } from "../../interview-notes/api/interviewNotesApi";
import {
  getAllJobPostings,
  updateJobPostingStatus,
} from "../../job-postings/api/jobPostingsApi";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import type { InterviewProcess } from "../types/interviewDashboard";
import type { JobPosting } from "../../job-postings/types/jobPosting";
import {
  createInterviewProcess,
  sortInterviewProcesses,
} from "../utils/interviewDashboard";

export function useInterviewDashboard() {
  const [data, setData] = useState<InterviewProcess[]>([]);
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [postings, setPostings] = useState<JobPosting[]>([]);
  const [statusError, setStatusError] = useState<string>();
  const [updatingIds, setUpdatingIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    const abortController = new AbortController();

    async function load() {
      const loadedPostings = await getAllJobPostings(abortController.signal);
      const interviewPostings = loadedPostings.filter(
        (posting) => posting.workflowStatus === "INTERVIEWING",
      );
      const processes = await Promise.all(
        interviewPostings.map(async (posting) =>
          createInterviewProcess(
            posting,
            await getInterviewNotes(posting.id, abortController.signal),
          ),
        ),
      );

      if (!abortController.signal.aborted) {
        setPostings(loadedPostings);
        setData(sortInterviewProcesses(processes));
      }
    }

    load()
      .catch((requestError: unknown) => {
        if (!isAbortError(requestError)) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (!abortController.signal.aborted) setIsLoading(false);
      });

    return () => abortController.abort();
  }, []);

  const changeStatus = useCallback(async (id: string, status: string) => {
    const posting = postings.find((item) => item.id === id);
    if (!posting || posting.workflowStatus === status) return true;

    setStatusError(undefined);
    setUpdatingIds((current) => new Set(current).add(id));

    try {
      const notes = status === "INTERVIEWING"
        ? await getInterviewNotes(id)
        : undefined;

      await updateJobPostingStatus(id, status);
      const updatedPosting = { ...posting, workflowStatus: status };

      setPostings((current) =>
        current.map((item) => item.id === id ? updatedPosting : item),
      );
      setData((current) => {
        const remaining = current.filter((process) => process.posting.id !== id);
        if (status !== "INTERVIEWING") return remaining;

        return sortInterviewProcesses([
          ...remaining,
          createInterviewProcess(updatedPosting, notes ?? []),
        ]);
      });
      return true;
    } catch (requestError) {
      setStatusError(getErrorMessage(requestError));
      return false;
    } finally {
      setUpdatingIds((current) => {
        const next = new Set(current);
        next.delete(id);
        return next;
      });
    }
  }, [postings]);

  return {
    changeStatus,
    data,
    error,
    isLoading,
    postings,
    statusError,
    updatingIds,
  };
}
