import { httpClient } from "../../../lib/api/httpClient";
import type { GeneratedCv } from "../types/generatedCv";

export function getGeneratedCv(id: string, signal?: AbortSignal) {
  return httpClient.get<GeneratedCv>(`/api/documents/${id}`, { signal });
}
