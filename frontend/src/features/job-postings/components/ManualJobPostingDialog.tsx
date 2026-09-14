import { useEffect, useRef, useState, type FormEvent } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { CreateManualJobPostingInput } from "../types/jobPosting";

type ManualJobPostingDialogProps = {
  error?: string;
  isOpen: boolean;
  isSaving: boolean;
  onClose: () => void;
  onCreate: (input: CreateManualJobPostingInput) => Promise<boolean>;
};

const emptyForm: CreateManualJobPostingInput = {
  title: "",
  company: "",
  location: "",
  employmentType: "",
  salary: "",
  description: "",
  url: "",
};

export function ManualJobPostingDialog({
  error,
  isOpen,
  isSaving,
  onClose,
  onCreate,
}: ManualJobPostingDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [form, setForm] = useState<CreateManualJobPostingInput>(emptyForm);
  const [validationError, setValidationError] = useState<string>();

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (isOpen && !dialog.open) {
      setForm(emptyForm);
      setValidationError(undefined);
      dialog.showModal();
    }
    if (!isOpen && dialog.open) dialog.close();
  }, [isOpen]);

  const update = (field: keyof CreateManualJobPostingInput, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const title = form.title.trim();
    const company = form.company.trim();
    const description = form.description.trim();
    if (!title || !company || !description) {
      setValidationError("Enter a job title, company, and job description.");
      return;
    }

    const url = form.url?.trim();
    if (url && !isHttpUrl(url)) {
      setValidationError("Enter a complete HTTP or HTTPS job URL.");
      return;
    }

    setValidationError(undefined);
    const created = await onCreate({
      title,
      company,
      location: optionalValue(form.location),
      employmentType: optionalValue(form.employmentType),
      salary: optionalValue(form.salary),
      description,
      url: optionalValue(url),
    });
    if (created) onClose();
  };

  return (
    <dialog
      aria-labelledby="manual-job-title"
      className="manual-job-dialog"
      onCancel={(event) => {
        event.preventDefault();
        if (!isSaving) onClose();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget && !isSaving) onClose();
      }}
      ref={dialogRef}
    >
      <form className="manual-job-dialog__surface" onSubmit={(event) => void submit(event)}>
        <header className="manual-job-dialog__header">
          <div>
            <span className="section-kicker">NEW OPPORTUNITY</span>
            <h2 id="manual-job-title">Add a job posting</h2>
            <p>Paste an opportunity that did not arrive through an email alert.</p>
          </div>
          <button
            aria-label="Close job posting form"
            className="dialog-close"
            disabled={isSaving}
            onClick={onClose}
            type="button"
          >
            ×
          </button>
        </header>

        <div className="manual-job-form">
          <div className="manual-job-form__row">
            <label className="form-field">
              <span>Job title *</span>
              <input
                autoFocus
                maxLength={200}
                onChange={(event) => update("title", event.target.value)}
                placeholder="Senior .NET Developer"
                required
                value={form.title}
              />
            </label>
            <label className="form-field">
              <span>Company *</span>
              <input
                autoComplete="organization"
                maxLength={200}
                onChange={(event) => update("company", event.target.value)}
                placeholder="Example Company"
                required
                value={form.company}
              />
            </label>
          </div>

          <div className="manual-job-form__row manual-job-form__row--three">
            <label className="form-field">
              <span>Location</span>
              <input
                maxLength={300}
                onChange={(event) => update("location", event.target.value)}
                placeholder="Warsaw / Remote"
                value={form.location}
              />
            </label>
            <label className="form-field">
              <span>Employment type</span>
              <input
                maxLength={100}
                onChange={(event) => update("employmentType", event.target.value)}
                placeholder="B2B or employment"
                value={form.employmentType}
              />
            </label>
            <label className="form-field">
              <span>Salary</span>
              <input
                maxLength={100}
                onChange={(event) => update("salary", event.target.value)}
                placeholder="20 000–26 000 PLN"
                value={form.salary}
              />
            </label>
          </div>

          <label className="form-field">
            <span>Original posting URL</span>
            <input
              inputMode="url"
              maxLength={2048}
              onChange={(event) => update("url", event.target.value)}
              placeholder="https://company.example/jobs/role"
              type="url"
              value={form.url}
            />
          </label>

          <label className="form-field">
            <span>Job description *</span>
            <textarea
              maxLength={50000}
              onChange={(event) => update("description", event.target.value)}
              placeholder="Paste the responsibilities, requirements, and role details here."
              required
              rows={12}
              value={form.description}
            />
            <small>{form.description.length.toLocaleString()} / 50,000 characters</small>
          </label>

          <ErrorMessage message={validationError ?? error} />

          <div className="manual-job-form__actions">
            <button
              className="secondary-action"
              disabled={isSaving}
              onClick={onClose}
              type="button"
            >
              Cancel
            </button>
            <button className="primary-action" disabled={isSaving} type="submit">
              {isSaving ? "Adding job…" : "Add job posting"}
            </button>
          </div>
        </div>
      </form>
    </dialog>
  );
}

function optionalValue(value?: string) {
  const normalized = value?.trim();
  return normalized || undefined;
}

function isHttpUrl(value: string) {
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}
