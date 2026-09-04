import { useEffect, useState } from "react";
import { getErrorMessage } from "../../../lib/errors/getErrorMessage";
import { getCandidateRules } from "../api/candidateRulesApi";
import type { CandidateRules } from "../types/candidateRules";

export function useCandidateRules() {
  const [data, setData] = useState<CandidateRules>();
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isActive = true;

    getCandidateRules()
      .then((rules) => {
        if (isActive) setData(rules);
      })
      .catch((requestError: unknown) => {
        if (isActive) setError(getErrorMessage(requestError));
      })
      .finally(() => {
        if (isActive) setIsLoading(false);
      });

    return () => {
      isActive = false;
    };
  }, []);

  return { data, error, isLoading };
}
