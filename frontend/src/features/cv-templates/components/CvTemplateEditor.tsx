import { useEffect, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { CvTemplate, SaveCvTemplateInput } from "../types/cvTemplate";

type CvTemplateEditorProps = {
  error?: string;
  isSaving: boolean;
  onSave: (input: SaveCvTemplateInput) => Promise<CvTemplate | undefined>;
  onSaved: (template: CvTemplate) => void;
  templates: CvTemplate[];
};

export function CvTemplateEditor({
  error,
  isSaving,
  onSave,
  onSaved,
  templates,
}: CvTemplateEditorProps) {
  const [editingId, setEditingId] = useState("");
  const [name, setName] = useState("");
  const [tex, setTex] = useState("");
  const [validationError, setValidationError] = useState<string>();

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
      return;
    }

    setValidationError(undefined);
    const saved = await onSave({ id: editingId || undefined, name: name.trim(), tex });
    if (saved) {
      setEditingId(saved.id);
      onSaved(saved);
    }
  };

  return (
    <details className="template-editor">
      <summary>
        <span>Add or edit a TeX CV template</span>
        <span aria-hidden="true">+</span>
      </summary>
      <div className="template-editor__body">
        <label className="form-field">
          <span>Template to edit</span>
          <select value={editingId} onChange={(event) => setEditingId(event.target.value)}>
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
            onChange={(event) => setName(event.target.value)}
          />
        </label>
        <label className="form-field">
          <span className="field-label-row">
            <span>TeX source</span>
            <span>{editingId ? "Saves a new version" : "New template"}</span>
          </span>
          <textarea
            className="template-tex-input"
            placeholder="Paste your complete \\documentclass… TeX CV here"
            spellCheck={false}
            value={tex}
            onChange={(event) => setTex(event.target.value)}
          />
        </label>
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
