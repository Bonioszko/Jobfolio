import { useCallback, useEffect, useState } from "react";
import { ApiError } from "../../../lib/api/httpClient";
import { getErrorMessage } from "../../../lib/errors/getErrorMessage";
import { getSession, logout as logoutRequest } from "../api/sessionApi";
import type { Session } from "../types/session";

type SessionStatus = "checking" | "authenticated" | "anonymous";

export function useSession() {
  const [status, setStatus] = useState<SessionStatus>("checking");
  const [session, setSession] = useState<Session>();
  const [error, setError] = useState<string>();
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  useEffect(() => {
    let isActive = true;

    getSession()
      .then((currentSession) => {
        if (!isActive) return;
        setSession(currentSession);
        setStatus("authenticated");
      })
      .catch((requestError: unknown) => {
        if (!isActive) return;
        if (!(requestError instanceof ApiError) || requestError.status !== 401) {
          setError(getErrorMessage(requestError));
        }
        setStatus("anonymous");
      });

    return () => {
      isActive = false;
    };
  }, []);

  const authenticate = useCallback((authenticatedSession: Session) => {
    setSession(authenticatedSession);
    setError(undefined);
    setStatus("authenticated");
  }, []);

  const logout = useCallback(async () => {
    setIsLoggingOut(true);
    try {
      await logoutRequest();
      setSession(undefined);
      setError(undefined);
      setStatus("anonymous");
    } catch (requestError: unknown) {
      setError(getErrorMessage(requestError));
    } finally {
      setIsLoggingOut(false);
    }
  }, []);

  return { status, session, error, authenticate, isLoggingOut, logout };
}
