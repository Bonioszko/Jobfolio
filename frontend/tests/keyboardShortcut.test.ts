import assert from "node:assert/strict";
import test from "node:test";
import { matchesPrimaryShortcut } from "../src/features/job-postings/utils/keyboardShortcut.ts";

function keyboardEvent(overrides: Partial<KeyboardEvent> = {}) {
  return {
    altKey: false,
    ctrlKey: false,
    defaultPrevented: false,
    key: "s",
    metaKey: false,
    repeat: false,
    shiftKey: false,
    target: null,
    ...overrides,
  } as KeyboardEvent;
}

test("does not match a bare shortcut key", () => {
  assert.equal(matchesPrimaryShortcut(keyboardEvent(), "s"), false);
});

test("matches Command plus the shortcut key", () => {
  assert.equal(matchesPrimaryShortcut(keyboardEvent({ metaKey: true }), "s"), true);
});

test("matches Control plus the shortcut key", () => {
  assert.equal(matchesPrimaryShortcut(keyboardEvent({ ctrlKey: true }), "s"), true);
});

test("rejects shortcuts with additional modifiers", () => {
  assert.equal(
    matchesPrimaryShortcut(keyboardEvent({ metaKey: true, shiftKey: true }), "s"),
    false,
  );
});

test("rejects shortcuts originating inside the Monaco editor", () => {
  const monacoTarget = {
    closest: (selector: string) => selector.includes(".monaco-editor") ? {} : null,
  } as unknown as EventTarget;

  assert.equal(
    matchesPrimaryShortcut(keyboardEvent({ metaKey: true, target: monacoTarget }), "s"),
    false,
  );
});
