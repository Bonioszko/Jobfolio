import { useEffect, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { CandidateRules } from "../../candidate-rules/types/candidateRules";
import { useCvGeneration } from "../../cv-generation/hooks/useCvGeneration";
import type { CvTemplate } from "../../cv-templates/types/cvTemplate";
import { GeneratedCvResult } from "../../generated-cvs/components/GeneratedCvResult";
import type { JobPosting } from "../types/jobPosting";
import { displayValue } from "../utils/displayValue";

type JobPostingDetailsProps = {
  documentName: string;
  posting: JobPosting;
  rules?: CandidateRules;
  templates: CvTemplate[];
};

export function JobPostingDetails({
  documentName,
  posting,
  rules,
  templates,
}: JobPostingDetailsProps) {
  const [templateVersionId, setTemplateVersionId] = useState(templates[0]?.versionId ?? "");
  const generation = useCvGeneration();

  useEffect(() => {
    if (!templates.some((template) => template.versionId === templateVersionId)) {
      setTemplateVersionId(templates[0]?.versionId ?? "");
    }
  }, [templateVersionId, templates]);

  const generate = () => {
    if (!templateVersionId || !rules) return;

    return generation.generate({
      sourceItemId: posting.id,
      templateVersionId,
      ruleVersionId: rules.versionId,
    });
  };

  return (
    <>
      <span className="eyebrow">
        {posting.parserKey} · parser v{posting.parserVersion}
      </span>
      <h2>{posting.displayTitle}</h2>
      <p>
        <strong>{displayValue(posting.parsedData.company)}</strong> ·{" "}
        {displayValue(posting.parsedData.location)}
      </p>
      <p className="description">{displayValue(posting.parsedData.description)}</p>
      {typeof posting.parsedData.url === "string" && (
        <a href={posting.parsedData.url} target="_blank" rel="noreferrer">
          Open job post
        </a>
      )}
      <details>
        <summary>View parsed source email</summary>
        <iframe
          className="email-preview"
          sandbox=""
          srcDoc={posting.demoEmailHtml ?? ""}
          title="Parsed source email"
        />
      </details>
      <label>
        Base CV template
        <select
          disabled={templates.length === 0}
          value={templateVersionId}
          onChange={(event) => setTemplateVersionId(event.target.value)}
        >
          {templates.length === 0 && <option value="">No templates available</option>}
          {templates.map((template) => (
            <option key={template.versionId} value={template.versionId}>
              {template.name} · v{template.version}
            </option>
          ))}
        </select>
      </label>
      <button
        disabled={!templateVersionId || !rules || generation.isGenerating}
        onClick={() => void generate()}
      >
        {generation.isGenerating ? "Generating…" : `Tailor ${documentName}`}
      </button>
      {generation.progress && <p className="progress">{generation.progress}</p>}
      <ErrorMessage message={generation.error} />
      {generation.document && (
        <GeneratedCvResult key={generation.document.versionId} document={generation.document} />
      )}
    </>
  );
}
