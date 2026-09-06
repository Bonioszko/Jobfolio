import { useCallback, useEffect, useState } from "react";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import {
  createInterviewNote,
  getInterviewNotes,
} from "../api/interviewNotesApi";
import type {
  CreateInterviewNoteInput,
  InterviewNote,
} from "../types/interviewNote";

export function useInterviewNotes(jobPostingId: string) {
  const [data, setData] = useState<InterviewNote[]>([]);
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    const abortController = new AbortController();
    setData([]);
    setError(undefined);
    setIsLoading(true);

    getInterviewNotes(jobPostingId, abortController.signal)
      .then(setData)
      .catch((requestError: unknown) => {
        if (!isAbortError(requestError)) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (!abortController.signal.aborted) setIsLoading(false);
      });

    return () => abortController.abort();
  }, [jobPostingId]);

  const add = useCallback(
    async (input: CreateInterviewNoteInput) => {
      setError(undefined);
      setIsSaving(true);

      try {
        const created = await createInterviewNote(jobPostingId, input);
        setData((current) =>
          [created, ...current].sort((left, right) =>
            right.interviewDate.localeCompare(left.interviewDate),
          ),
        );
        return true;
      } catch (requestError) {
        setError(getErrorMessage(requestError));
        return false;
      } finally {
        setIsSaving(false);
      }
    },
    [jobPostingId],
  );

  return { add, data, error, isLoading, isSaving };
}
