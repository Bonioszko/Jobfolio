import { httpClient } from "../../../lib/api/httpClient";
import type { CandidateRules } from "../types/candidateRules";

export function getCandidateRules() {
  return httpClient.get<CandidateRules>("/api/rules");
}
