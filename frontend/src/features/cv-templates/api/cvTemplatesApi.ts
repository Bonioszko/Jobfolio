import { httpClient } from "../../../lib/api/httpClient";
import type { CvTemplate, SaveCvTemplateInput } from "../types/cvTemplate";

export function getCvTemplates() {
  return httpClient.get<CvTemplate[]>("/api/templates");
}

export function saveCvTemplate({ id, name, tex }: SaveCvTemplateInput) {
  const body = { name, tex };
  return id
    ? httpClient.put<CvTemplate>(`/api/templates/${id}`, body)
    : httpClient.post<CvTemplate>("/api/templates", body);
}
