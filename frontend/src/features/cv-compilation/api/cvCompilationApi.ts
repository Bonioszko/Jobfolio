import {
  normalizeAsyncJob,
  type AsyncJobResponse,
} from "../../../lib/api/asyncJob";
import { httpClient } from "../../../lib/api/httpClient";

export function createCvCompileJob(documentVersionId: string, signal?: AbortSignal) {
  return httpClient
    .post<AsyncJobResponse>("/api/compile-jobs", { documentVersionId }, { signal })
    .then(normalizeAsyncJob);
}

export function createTemplateCompileJob(templateVersionId: string, signal?: AbortSignal) {
  return httpClient
    .post<AsyncJobResponse>("/api/template-compile-jobs", { documentVersionId: templateVersionId }, { signal })
    .then(normalizeAsyncJob);
}

export function getCvCompileJob(id: string, signal?: AbortSignal) {
  return httpClient
    .get<AsyncJobResponse>(`/api/compile-jobs/${id}`, { signal })
    .then(normalizeAsyncJob);
}

export function getPdfDownloadUrl(pdfArtifactId: string) {
  return `/api/pdf-artifacts/${pdfArtifactId}/download`;
}
