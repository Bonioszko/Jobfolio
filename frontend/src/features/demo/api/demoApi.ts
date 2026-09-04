import { httpClient } from "../../../lib/api/httpClient";
import type { DemoSession } from "../types/demoSession";

export function createDemoSession() {
  return httpClient.post<DemoSession>("/api/auth/demo");
}
