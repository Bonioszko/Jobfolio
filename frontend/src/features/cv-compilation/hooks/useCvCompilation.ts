import { useCallback, useEffect, useRef, useState } from "react";
import { pollAsyncJob } from "../../../lib/api/asyncJob";
import { getErrorMessage, isAbortError } from "../../../lib/errors/getErrorMessage";
import { createCvCompileJob, getCvCompileJob } from "../api/cvCompilationApi";

export function useCvCompilation() {
  const abortController = useRef<AbortController | null>(null);
  const [pdfArtifactId, setPdfArtifactId] = useState<string>();
  const [error, setError] = useState<string>();
  const [progress, setProgress] = useState<string>();

  useEffect(() => () => abortController.current?.abort(), []);

  const compile = useCallback(async (documentVersionId: string) => {
    abortController.current?.abort();
    abortController.current = new AbortController();
    const { signal } = abortController.current;

    setPdfArtifactId(undefined);
    setError(undefined);
    setProgress("Queued for compilation");

    try {
      const createdJob = await createCvCompileJob(documentVersionId, signal);
      const completedJob = await pollAsyncJob(createdJob, getCvCompileJob, {
        signal,
        onUpdate: (job) =>
          setProgress(job.status === "Queued" ? "Queued for compilation" : "Compiling…"),
      });

      if (completedJob.status !== "Succeeded" || !completedJob.outputId) {
        throw new Error(completedJob.error || "Compilation failed.");
      }

      setPdfArtifactId(completedJob.outputId);
      setProgress("PDF ready");
    } catch (requestError) {
      if (isAbortError(requestError)) return;
      setError(getErrorMessage(requestError));
      setProgress(undefined);
    }
  }, []);

  return {
    pdfArtifactId,
    error,
    progress,
    isCompiling: progress === "Queued for compilation" || progress === "Compiling…",
    compile,
  };
}
