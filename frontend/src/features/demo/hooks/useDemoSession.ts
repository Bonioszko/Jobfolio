import { useCallback, useState } from "react";
import type { Session } from "../../auth/types/session";
import { getErrorMessage } from "../../../lib/errors/getErrorMessage";
import { createDemoSession } from "../api/demoApi";

export function useDemoSession(onAuthenticated: (session: Session) => void) {
  const [error, setError] = useState<string>();
  const [isStarting, setIsStarting] = useState(false);

  const start = useCallback(async () => {
    setError(undefined);
    setIsStarting(true);

    try {
      const demoSession = await createDemoSession();
      onAuthenticated({ userId: "demo", mode: demoSession.mode });
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    } finally {
      setIsStarting(false);
    }
  }, [onAuthenticated]);

  return { error, isStarting, start };
}
