export function matchesUnmodifiedShortcut(event: KeyboardEvent, key: string) {
  return !event.defaultPrevented &&
    !event.repeat &&
    !event.ctrlKey &&
    !event.altKey &&
    !event.metaKey &&
    event.key.toLowerCase() === key.toLowerCase() &&
    !isEditableTarget(event.target);
}

function isEditableTarget(target: EventTarget | null) {
  return target instanceof HTMLElement &&
    (target.isContentEditable || ["INPUT", "SELECT", "TEXTAREA"].includes(target.tagName));
}
