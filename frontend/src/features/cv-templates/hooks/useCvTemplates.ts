import { useEffect, useState } from "react";
import { getErrorMessage } from "../../../lib/errors/getErrorMessage";
import { getCvTemplates, saveCvTemplate } from "../api/cvTemplatesApi";
import type { CvTemplate, SaveCvTemplateInput } from "../types/cvTemplate";

export function useCvTemplates() {
  const [data, setData] = useState<CvTemplate[]>([]);
  const [error, setError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string>();

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

  const saveTemplate = async (input: SaveCvTemplateInput) => {
    setSaveError(undefined);
    setIsSaving(true);
    try {
      const saved = await saveCvTemplate(input);
      setData((current) =>
        [...current.filter((template) => template.id !== saved.id), saved].sort((left, right) =>
          left.name.localeCompare(right.name),
        ),
      );
      return saved;
    } catch (requestError) {
      setSaveError(getErrorMessage(requestError));
      return undefined;
    } finally {
      setIsSaving(false);
    }
  };

  return { data, error, isLoading, isSaving, saveError, saveTemplate };
}
