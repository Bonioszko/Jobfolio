export type JobPosting = {
  id: string;
  sourceKey: string;
  displayTitle: string;
  parsedData: Record<string, unknown>;
  workflowStatus: string;
  parserKey: string;
  parserVersion: number;
  sourceReceivedAt: string;
  appliedAt: string | null;
  demoEmailHtml?: string;
};

export type JobPostingPage = {
  items: JobPosting[];
  nextCursor: string | null;
};

export type CreateManualJobPostingInput = {
  title: string;
  company: string;
  location?: string;
  employmentType?: string;
  salary?: string;
  description: string;
  url?: string;
};
