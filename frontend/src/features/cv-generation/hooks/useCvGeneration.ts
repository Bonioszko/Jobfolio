import { useCallback, useEffect, useRef, useState } from "react";
import { pollAsyncJob } from "../../../lib/api/asyncJob";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import { getGeneratedCv } from "../../generated-cvs/api/generatedCvsApi";
import type { GeneratedCv } from "../../generated-cvs/types/generatedCv";
import {
  createCvGenerationJob,
  getCvGenerationJob,
  type CreateCvGenerationRequest,
} from "../api/cvGenerationApi";

export function useCvGeneration() {
  const abortController = useRef<AbortController | null>(null);
  const [document, setDocument] = useState<GeneratedCv>();
  const [error, setError] = useState<string>();
  const [progress, setProgress] = useState<string>();

  useEffect(() => () => abortController.current?.abort(), []);

  const generate = useCallback(async (request: CreateCvGenerationRequest) => {
    abortController.current?.abort();
    abortController.current = new AbortController();
    const { signal } = abortController.current;

    setDocument(undefined);
    setError(undefined);
    setProgress("Queued for generation");

    try {
      const createdJob = await createCvGenerationJob(request, signal);
      const completedJob = await pollAsyncJob(createdJob, getCvGenerationJob, {
        signal,
        onUpdate: (job) =>
          setProgress(job.status === "Queued" ? "Queued for generation" : "Generating…"),
      });

      if (completedJob.status !== "Succeeded" || !completedJob.outputId) {
        throw new Error(completedJob.error || "Generation failed.");
      }

      const generatedCv = await getGeneratedCv(completedJob.outputId, signal);
      setDocument(generatedCv);
      setProgress("Generated");
    } catch (requestError) {
      if (isAbortError(requestError)) return;
      setError(getErrorMessage(requestError));
      setProgress(undefined);
    }
  }, []);

  return {
    document,
    error,
    progress,
    isGenerating: progress === "Queued for generation" || progress === "Generating…",
    generate,
  };
}
