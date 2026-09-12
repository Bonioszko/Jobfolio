import Editor, { type BeforeMount } from "@monaco-editor/react";
import "../../../lib/monaco/configureMonaco";

type LatexSourceEditorProps = {
  onChange: (value: string) => void;
  value: string;
};

const configureLatex: BeforeMount = (monaco) => {
  if (!monaco.languages.getLanguages().some((language: { id: string }) => language.id === "latex")) {
    monaco.languages.register({ id: "latex" });
    monaco.languages.setMonarchTokensProvider("latex", {
      tokenizer: {
        root: [
          [/%.*/, "comment"],
          [/\\[a-zA-Z@]+/, "keyword"],
          [/[{}\[\]]/, "delimiter"],
          [/\$[^$]*\$/, "string"],
        ],
      },
    });
  }
};

export function LatexSourceEditor({ onChange, value }: LatexSourceEditorProps) {
  return (
    <Editor
      beforeMount={configureLatex}
      defaultLanguage="latex"
      height="100%"
      options={{
        ariaLabel: "TeX source editor",
        automaticLayout: true,
        fontFamily: '"SFMono-Regular", Consolas, monospace',
        fontSize: 12,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        tabSize: 2,
        wordWrap: "on",
      }}
      value={value}
      onChange={(nextValue) => onChange(nextValue ?? "")}
    />
  );
}
