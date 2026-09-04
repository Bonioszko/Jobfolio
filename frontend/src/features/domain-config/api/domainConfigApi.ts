import { httpClient } from "../../../lib/api/httpClient";
import type { DomainConfig } from "../types/domainConfig";

export function getDomainConfig() {
  return httpClient.get<DomainConfig>("/api/config/domain");
}
