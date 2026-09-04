import {
  normalizeAsyncJob,
  type AsyncJobResponse,
} from "../../../lib/api/asyncJob";
import { httpClient } from "../../../lib/api/httpClient";

export type CreateCvGenerationRequest = {
  sourceItemId: string;
  templateVersionId: string;
  ruleVersionId: string;
  instruction?: string;
};

export function createCvGenerationJob(request: CreateCvGenerationRequest, signal?: AbortSignal) {
  return httpClient
    .post<AsyncJobResponse>("/api/generation-jobs", request, { signal })
    .then(normalizeAsyncJob);
}

export function getCvGenerationJob(id: string, signal?: AbortSignal) {
  return httpClient
    .get<AsyncJobResponse>(`/api/generation-jobs/${id}`, { signal })
    .then(normalizeAsyncJob);
}
