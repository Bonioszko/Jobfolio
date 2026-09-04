export type AsyncJobStatus = "Queued" | "Running" | "Succeeded" | "Failed" | "TimedOut";

export type AsyncJob = {
  id: string;
  status: AsyncJobStatus;
  error: string | null;
  outputId: string | null;
};

export type AsyncJobResponse = Omit<AsyncJob, "status"> & {
  status: AsyncJobStatus | number;
};

const statuses: AsyncJobStatus[] = ["Queued", "Running", "Succeeded", "Failed", "TimedOut"];

export function normalizeAsyncJob(response: AsyncJobResponse): AsyncJob {
  const status =
    typeof response.status === "number" ? statuses[response.status] : response.status;

  if (!status || !statuses.includes(status)) {
    throw new Error("The server returned an unknown job status.");
  }

  return { ...response, status };
}

type PollAsyncJobOptions = {
  signal: AbortSignal;
  onUpdate?: (job: AsyncJob) => void;
  intervalMs?: number;
};

function wait(ms: number, signal: AbortSignal) {
  return new Promise<void>((resolve, reject) => {
    if (signal.aborted) {
      reject(new DOMException("Polling was cancelled.", "AbortError"));
      return;
    }

    const onAbort = () => {
      window.clearTimeout(timeoutId);
      reject(new DOMException("Polling was cancelled.", "AbortError"));
    };
    const timeoutId = window.setTimeout(() => {
      signal.removeEventListener("abort", onAbort);
      resolve();
    }, ms);

    signal.addEventListener("abort", onAbort, { once: true });
  });
}

export async function pollAsyncJob(
  initialJob: AsyncJob,
  getJob: (id: string, signal: AbortSignal) => Promise<AsyncJob>,
  { signal, onUpdate, intervalMs = 1_000 }: PollAsyncJobOptions,
) {
  let job = initialJob;
  onUpdate?.(job);

  while (job.status === "Queued" || job.status === "Running") {
    await wait(intervalMs, signal);
    job = await getJob(job.id, signal);
    onUpdate?.(job);
  }

  return job;
}
