export const supportedLatexPackages = [
  "babel",
  "color",
  "enumitem",
  "fancyhdr",
  "fontawesome5",
  "fullpage",
  "geometry",
  "hyperref",
  "latexsym",
  "marvosym",
  "multicol",
  "tabularx",
  "titlesec",
  "verbatim",
] as const;

const supportedPackageSet = new Set<string>(supportedLatexPackages);
const usePackagePattern = /\\usepackage(?:\s*\[[^\]]*\])?\s*\{([^}]*)\}/g;
const pdfTexGlyphMappingPattern =
  /\\input\s*\{\s*glyphtounicode(?:\.tex)?\s*\}|\\pdfgentounicode\b/;

export function getLatexSupportError(tex: string): string | undefined {
  const uncommentedTex = stripComments(tex);
  if (pdfTexGlyphMappingPattern.test(uncommentedTex)) {
    return "Remove \\input{glyphtounicode} and \\pdfgentounicode: they require pdfTeX, but the compiler uses Tectonic/XeTeX.";
  }

  const unsupportedPackages = findUnsupportedLatexPackages(uncommentedTex);
  if (unsupportedPackages.length === 0) return undefined;

  return `Unsupported TeX package${unsupportedPackages.length === 1 ? "" : "s"}: ${unsupportedPackages.join(", ")}.`;
}

export function findUnsupportedLatexPackages(tex: string): string[] {
  const unsupported = new Set<string>();

  for (const match of stripComments(tex).matchAll(usePackagePattern)) {
    for (const packageName of match[1].split(",").map((name) => name.trim())) {
      if (packageName && !supportedPackageSet.has(packageName)) {
        unsupported.add(packageName);
      }
    }
  }

  return [...unsupported].sort();
}

function stripComments(tex: string): string {
  return tex
    .split("\n")
    .map((line) => {
      for (let index = 0; index < line.length; index += 1) {
        if (line[index] !== "%") continue;

        let precedingBackslashes = 0;
        for (let cursor = index - 1; cursor >= 0 && line[cursor] === "\\"; cursor -= 1) {
          precedingBackslashes += 1;
        }

        if (precedingBackslashes % 2 === 0) return line.slice(0, index);
      }

      return line;
    })
    .join("\n");
}
