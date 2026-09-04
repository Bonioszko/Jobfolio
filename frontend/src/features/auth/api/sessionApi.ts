import { httpClient } from "../../../lib/api/httpClient";
import type { Session } from "../types/session";

export function getSession() {
  return httpClient.get<Session>("/api/me");
}
