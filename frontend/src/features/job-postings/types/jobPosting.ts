export type JobPosting = {
  id: string;
  sourceKey: string;
  displayTitle: string;
  parsedData: Record<string, unknown>;
  workflowStatus: string;
  parserKey: string;
  parserVersion: number;
  sourceReceivedAt: string;
  demoEmailHtml?: string;
};
