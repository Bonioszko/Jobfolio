import type { InterviewNote } from "../../interview-notes/types/interviewNote";
import type { JobPosting } from "../../job-postings/types/jobPosting";

export type InterviewProcess = {
  latestInterview?: InterviewNote;
  notes: InterviewNote[];
  posting: JobPosting;
};
