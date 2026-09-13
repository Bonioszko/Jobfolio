export type WorkflowStatus = {
  code: string;
  label: string;
};

export type DomainField = {
  key: string;
  label: string;
  type: string;
  showInDashboard: boolean;
};

export type DomainConfig = {
  sourceItem: { singular: string; plural: string };
  generatedDocument: { singular: string; plural: string };
  fields: DomainField[];
  statuses: WorkflowStatus[];
  features: {
    cv: boolean;
    cvGeneration: boolean;
    demo: boolean;
  };
  demoPolicy: {
    sessionLifetimeHours: number;
    maxCompilationJobsPerWindow: number;
    compilationWindowMinutes: number;
  };
};
