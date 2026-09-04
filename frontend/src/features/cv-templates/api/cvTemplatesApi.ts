import { httpClient } from "../../../lib/api/httpClient";
import type { CvTemplate } from "../types/cvTemplate";

export function getCvTemplates() {
  return httpClient.get<CvTemplate[]>("/api/templates");
}
