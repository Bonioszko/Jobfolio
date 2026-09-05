const authenticationErrors: Record<string, string> = {
  "not-configured": "Google sign-in has not been configured on this server yet.",
  "sign-in-failed": "Google sign-in failed or that account is not allowed.",
};

export function getAuthenticationError(): string | undefined {
  const code = new URLSearchParams(window.location.search).get("authError");
  return code ? (authenticationErrors[code] ?? "Google sign-in could not be completed.") : undefined;
}
