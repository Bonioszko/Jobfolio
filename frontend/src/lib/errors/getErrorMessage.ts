export function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}

export function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === "AbortError";
}
