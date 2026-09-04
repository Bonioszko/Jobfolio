import { useEffect, useState } from "react";
import { getErrorMessage } from "../../../lib/errors/getErrorMessage";
import { getDomainConfig } from "../api/domainConfigApi";
import type { DomainConfig } from "../types/domainConfig";

export function useDomainConfig() {
  const [data, setData] = useState<DomainConfig>();
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isActive = true;

    getDomainConfig()
      .then((config) => {
        if (isActive) setData(config);
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
