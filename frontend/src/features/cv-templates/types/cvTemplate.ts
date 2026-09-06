export type CvTemplate = {
  id: string;
  name: string;
  version: number;
  versionId: string;
  tex: string;
};

export type SaveCvTemplateInput = {
  id?: string;
  name: string;
  tex: string;
};
