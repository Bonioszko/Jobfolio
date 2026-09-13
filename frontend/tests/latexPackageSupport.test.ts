import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import {
  findUnsupportedLatexPackages,
  getLatexSupportError,
  supportedLatexPackages,
} from "../src/features/cv-templates/utils/latexPackageSupport.ts";

test("accepts the packages baked into the compiler image", async () => {
  const primer = await readFile(
    new URL("../../infrastructure/compiler/tectonic-cache-primer.tex", import.meta.url),
    "utf8",
  );

  assert.deepEqual(findUnsupportedLatexPackages(primer), []);
  assert.equal(getLatexSupportError(primer), undefined);
  assert.deepEqual(extractPackages(primer).sort(), [...supportedLatexPackages].sort());
});

test("reports distinct unsupported packages from single and grouped declarations", () => {
  const tex = String.raw`
    \usepackage{geometry, unsupported-one}
    \usepackage[option=value]{unsupported-two}
    \usepackage{unsupported-one}
  `;

  assert.deepEqual(findUnsupportedLatexPackages(tex), [
    "unsupported-one",
    "unsupported-two",
  ]);
  assert.equal(
    getLatexSupportError(tex),
    "Unsupported TeX packages: unsupported-one, unsupported-two.",
  );
});

test("ignores commented declarations but reads declarations after escaped percent signs", () => {
  const tex = String.raw`
    % \usepackage{commented-out}
    content \% \usepackage{visible-and-unsupported}
  `;

  assert.deepEqual(findUnsupportedLatexPackages(tex), ["visible-and-unsupported"]);
});

test("explains that pdfTeX glyph mapping directives are incompatible with Tectonic", () => {
  const tex = String.raw`
    \input{glyphtounicode}
    \pdfgentounicode=1
  `;

  assert.equal(
    getLatexSupportError(tex),
    "Remove \\input{glyphtounicode} and \\pdfgentounicode: they require pdfTeX, but the compiler uses Tectonic/XeTeX.",
  );
});

test("does not flag commented pdfTeX directives", () => {
  const tex = String.raw`
    % \input{glyphtounicode}
    % \pdfgentounicode=1
    \usepackage{geometry}
  `;

  assert.equal(getLatexSupportError(tex), undefined);
});

function extractPackages(tex: string): string[] {
  return [...tex.matchAll(/\\usepackage(?:\s*\[[^\]]*\])?\s*\{([^}]*)\}/g)]
    .flatMap((match) => match[1].split(","))
    .map((packageName) => packageName.trim())
    .filter(Boolean);
}
