import { lazy, Suspense, useEffect, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import { getPdfDownloadUrl } from "../../cv-compilation/api/cvCompilationApi";
import { useCvCompilation } from "../../cv-compilation/hooks/useCvCompilation";
import type { CvTemplate, SaveCvTemplateInput } from "../types/cvTemplate";
import { getLatexSupportError } from "../utils/latexPackageSupport";

type CvTemplateEditorProps = {
  error?: string;
  isSaving: boolean;
  onSave: (input: SaveCvTemplateInput) => Promise<CvTemplate | undefined>;
  onSaved: (template: CvTemplate) => void;
  templates: CvTemplate[];
};

const LatexSourceEditor = lazy(() =>
  import("./LatexSourceEditor").then((module) => ({ default: module.LatexSourceEditor })),
);

export function CvTemplateEditor({
  error,
  isSaving,
  onSave,
  onSaved,
  templates,
}: CvTemplateEditorProps) {
  const compilation = useCvCompilation();
  const [editingId, setEditingId] = useState("");
  const [name, setName] = useState("");
  const [tex, setTex] = useState("");
  const [validationError, setValidationError] = useState<string>();
  const [hasOpened, setHasOpened] = useState(false);

  useEffect(() => {
    const selected = templates.find((template) => template.id === editingId);
    if (selected) {
      setName(selected.name);
      setTex(selected.tex);
    } else {
      setName("");
      setTex("");
    }
    setValidationError(undefined);
  }, [editingId, templates]);

  const save = async () => {
    if (!name.trim() || !tex.trim()) {
      setValidationError("Enter a template name and paste a complete TeX document.");
      return undefined;
    }

    const latexSupportError = getLatexSupportError(tex);
    if (latexSupportError) {
      setValidationError(latexSupportError);
      return undefined;
    }

    setValidationError(undefined);
    const saved = await onSave({ id: editingId || undefined, name: name.trim(), tex });
    if (saved) {
      setEditingId(saved.id);
      onSaved(saved);
    }
    return saved;
  };

  const selectedTemplate = templates.find((template) => template.id === editingId);
  const isDirty = !selectedTemplate ||
    selectedTemplate.name !== name.trim() ||
    selectedTemplate.tex !== tex;
  const pdfUrl = compilation.pdfArtifactId
    ? getPdfDownloadUrl(compilation.pdfArtifactId)
    : undefined;

  const compile = async () => {
    const template = isDirty ? await save() : selectedTemplate;
    if (template) await compilation.compileTemplate(template.versionId);
  };

  return (
    <details
      className="template-editor"
      onToggle={(event) => {
        if (event.currentTarget.open) setHasOpened(true);
      }}
    >
      <summary>
        <span>Add or edit a TeX CV template</span>
        <span aria-hidden="true">+</span>
      </summary>
      <div className="template-editor__body">
        <label className="form-field">
          <span>Template to edit</span>
          <select
            value={editingId}
            onChange={(event) => {
              compilation.reset();
              setEditingId(event.target.value);
            }}
          >
            <option value="">Create a new template</option>
            {templates.map((template) => (
              <option key={template.id} value={template.id}>
                {template.name} · v{template.version}
              </option>
            ))}
          </select>
        </label>
        <label className="form-field">
          <span>Template name</span>
          <input
            maxLength={120}
            placeholder="For example: Backend focused"
            value={name}
            onChange={(event) => {
              compilation.reset();
              setName(event.target.value);
            }}
          />
        </label>
        <div className="form-field">
          <span className="field-label-row">
            <span>TeX source</span>
            <span>{editingId ? "Saves a new version" : "New template"}</span>
          </span>
          <div className="template-tex-input">
            {hasOpened && (
              <Suspense fallback={<div className="template-editor-loading">Loading editor…</div>}>
                <LatexSourceEditor
                  value={tex}
                  onChange={(value) => {
                    compilation.reset();
                    setTex(value);
                  }}
                />
              </Suspense>
            )}
          </div>
        </div>
        <div className="template-compile-actions">
          <button
            className="secondary-action"
            disabled={isSaving || compilation.isCompiling || !name.trim() || !tex.trim()}
            onClick={() => void compile()}
            type="button"
          >
            {compilation.isCompiling
              ? "Compiling PDF…"
              : isDirty
                ? "Save and compile PDF"
                : "Compile current version"}
          </button>
          {compilation.progress && <p className="progress">{compilation.progress}</p>}
          <ErrorMessage message={compilation.error} />
          {pdfUrl && (
            <>
              <iframe className="pdf-preview" title="Compiled CV preview" src={pdfUrl} />
              <a className="download primary-action" href={pdfUrl}>
                Download PDF
              </a>
            </>
          )}
        </div>
        <ErrorMessage message={validationError ?? error} />
        <button
          className="secondary-action template-save"
          disabled={isSaving}
          onClick={() => void save()}
          type="button"
        >
          {isSaving ? "Saving template…" : editingId ? "Save new version" : "Save template"}
        </button>
      </div>
    </details>
  );
}
