import { useEffect, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { CandidateRules } from "../../candidate-rules/types/candidateRules";
import type { CvTemplate } from "../../cv-templates/types/cvTemplate";
import { GeneratedCvResult } from "../../generated-cvs/components/GeneratedCvResult";
import { useCvGeneration } from "../hooks/useCvGeneration";

type CvTailoringPanelProps = {
  documentName: string;
  isLoadingResources: boolean;
  jobPostingId: string;
  rules?: CandidateRules;
  templates: CvTemplate[];
};

export function CvTailoringPanel({
  documentName,
  isLoadingResources,
  jobPostingId,
  rules,
  templates,
}: CvTailoringPanelProps) {
  const [templateVersionId, setTemplateVersionId] = useState(templates[0]?.versionId ?? "");
  const [customJobDescription, setCustomJobDescription] = useState("");
  const [instruction, setInstruction] = useState("");
  const generation = useCvGeneration();

  useEffect(() => {
    if (!templates.some((template) => template.versionId === templateVersionId)) {
      setTemplateVersionId(templates[0]?.versionId ?? "");
    }
  }, [templateVersionId, templates]);

  const generate = () => {
    if (!templateVersionId || !rules) return;

    return generation.generate({
      sourceItemId: jobPostingId,
      templateVersionId,
      ruleVersionId: rules.versionId,
      customJobDescription: customJobDescription.trim() || undefined,
      instruction: instruction.trim() || undefined,
    });
  };

  const canGenerate = Boolean(templateVersionId && rules) && !generation.isGenerating;

  return (
    <section className="tailoring-panel" aria-labelledby="tailoring-heading">
      <div className="tailoring-panel__intro">
        <span className="section-kicker">CV TAILORING</span>
        <h3 id="tailoring-heading">Prepare a focused application</h3>
        <p>
          The imported posting is included automatically. Add missing details only when the
          original listing is incomplete.
        </p>
      </div>

      <div className="tailoring-form">
        <label className="form-field">
          <span>Base CV</span>
          <select
            disabled={isLoadingResources || templates.length === 0}
            value={templateVersionId}
            onChange={(event) => setTemplateVersionId(event.target.value)}
          >
            {isLoadingResources && <option value="">Loading templates…</option>}
            {!isLoadingResources && templates.length === 0 && (
              <option value="">No templates available</option>
            )}
            {templates.map((template) => (
              <option key={template.versionId} value={template.versionId}>
                {template.name} · v{template.version}
              </option>
            ))}
          </select>
        </label>

        <label className="form-field">
          <span className="field-label-row">
            <span>Custom job description</span>
            <span>Optional</span>
          </span>
          <textarea
            className="job-description-input"
            placeholder="Paste extra responsibilities, requirements, or the full description of a different role…"
            value={customJobDescription}
            onChange={(event) => setCustomJobDescription(event.target.value)}
          />
          <small>This is treated as job-posting content, not as an instruction.</small>
        </label>

        <details className="instruction-disclosure">
          <summary>Additional tailoring direction</summary>
          <label className="form-field">
            <span className="visually-hidden">Additional tailoring direction</span>
            <textarea
              className="instruction-input"
              placeholder="For example: keep the summary concise or emphasize leadership experience."
              value={instruction}
              onChange={(event) => setInstruction(event.target.value)}
            />
          </label>
        </details>

        <button
          className="primary-action tailoring-submit"
          disabled={!canGenerate}
          onClick={() => void generate()}
          type="button"
        >
          <span>{generation.isGenerating ? "Tailoring…" : `Tailor ${documentName}`}</span>
          <span aria-hidden="true">→</span>
        </button>
        {!rules && !isLoadingResources && (
          <p className="form-note">Candidate rules are required before tailoring.</p>
        )}
      </div>

      {generation.progress && <p className="progress">{generation.progress}</p>}
      <ErrorMessage message={generation.error} />
      {generation.document && (
        <GeneratedCvResult key={generation.document.versionId} document={generation.document} />
      )}
    </section>
  );
}
