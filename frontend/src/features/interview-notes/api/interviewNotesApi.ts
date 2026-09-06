import { httpClient } from "../../../lib/api/httpClient";
import type {
  CreateInterviewNoteInput,
  InterviewNote,
} from "../types/interviewNote";

export function getInterviewNotes(jobPostingId: string, signal?: AbortSignal) {
  return httpClient.get<InterviewNote[]>(
    `/api/source-items/${jobPostingId}/interview-notes`,
    { signal },
  );
}

export function createInterviewNote(
  jobPostingId: string,
  input: CreateInterviewNoteInput,
) {
  return httpClient.post<InterviewNote>(
    `/api/source-items/${jobPostingId}/interview-notes`,
    input,
  );
}
