export type InterviewNote = {
  id: string;
  jobPostingId: string;
  stage: string;
  interviewDate: string;
  notes: string;
  createdAt: string;
};

export type CreateInterviewNoteInput = {
  stage: string;
  interviewDate: string;
  notes: string;
};
