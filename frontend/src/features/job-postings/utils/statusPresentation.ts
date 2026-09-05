export function statusTone(status: string) {
  switch (status.toUpperCase()) {
    case "APPLIED":
    case "INTERVIEWING":
      return "active";
    case "REJECTED":
    case "SKIP":
      return "closed";
    case "TO_APPLY":
      return "queued";
    default:
      return "new";
  }
}
