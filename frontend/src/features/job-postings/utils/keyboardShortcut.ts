export const OPEN_JOB_POST_SHORTCUT = "E";

export function matchesPrimaryShortcut(event: KeyboardEvent, key: string) {
  return !event.defaultPrevented &&
    !event.repeat &&
    !event.altKey &&
    !event.shiftKey &&
    (event.metaKey || event.ctrlKey) &&
    event.key.toLowerCase() === key.toLowerCase() &&
    !isEditableTarget(event.target);
}

export function primaryShortcutLabel(key: string) {
  const isApplePlatform = typeof navigator !== "undefined" &&
    /Mac|iPhone|iPad|iPod/i.test(navigator.platform);
  return isApplePlatform ? `⌘${key}` : `Ctrl+${key}`;
}

export function primaryShortcutAriaLabel(key: string) {
  return `Meta+${key} Control+${key}`;
}

function isEditableTarget(target: EventTarget | null) {
  const element = target as (EventTarget & { closest?: (selector: string) => Element | null }) | null;
  return typeof element?.closest === "function" &&
    element.closest(
      "input, select, textarea, [contenteditable='true'], [role='textbox'], .monaco-editor",
    ) !== null;
}
