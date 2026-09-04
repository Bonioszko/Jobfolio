export function displayValue(value: unknown) {
  if (value === null || value === undefined || value === "") {
    return "—";
  }

  return typeof value === "object" ? JSON.stringify(value) : String(value);
}
