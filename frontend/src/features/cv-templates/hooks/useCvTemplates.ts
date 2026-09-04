import { useEffect, useState } from "react";
import { getErrorMessage } from "../../../lib/errors/getErrorMessage";
import { getCvTemplates } from "../api/cvTemplatesApi";
import type { CvTemplate } from "../types/cvTemplate";

export function useCvTemplates() {
  const [data, setData] = useState<CvTemplate[]>([]);
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isActive = true;

    getCvTemplates()
      .then((templates) => {
        if (isActive) setData(templates);
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
