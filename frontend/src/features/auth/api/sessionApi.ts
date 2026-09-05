import { httpClient } from "../../../lib/api/httpClient";
import type { Session } from "../types/session";

export function getSession() {
  return httpClient.get<Session>("/api/me");
}

export function logout() {
  return httpClient.post<void>("/api/auth/logout");
}

export const googleSignInUrl = "/api/auth/google";
