import { useEffect, useMemo, useRef, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import type { WorkflowStatus } from "../../domain-config/types/domainConfig";
import type { JobPosting } from "../../job-postings/types/jobPosting";
import { displayValue } from "../../job-postings/utils/displayValue";
import type { InterviewProcess } from "../types/interviewDashboard";
import { getInterviewCandidates } from "../utils/interviewDashboard";

type ManageInterviewProcessesDialogProps = {
  error?: string;
  isOpen: boolean;
  onClose: () => void;
  onStatusChange: (id: string, status: string) => Promise<boolean>;
  postings: JobPosting[];
  processes: InterviewProcess[];
  statuses: WorkflowStatus[];
  updatingIds: Set<string>;
};

export function ManageInterviewProcessesDialog({
  error,
  isOpen,
  onClose,
  onStatusChange,
  postings,
  processes,
  statuses,
  updatingIds,
}: ManageInterviewProcessesDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const candidates = useMemo(
    () => getInterviewCandidates(postings),
    [postings],
  );
  const [selectedPostingId, setSelectedPostingId] = useState("");

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (isOpen && !dialog.open) dialog.showModal();
    if (!isOpen && dialog.open) dialog.close();
  }, [isOpen]);

  useEffect(() => {
    if (!candidates.some((posting) => posting.id === selectedPostingId)) {
      setSelectedPostingId(candidates[0]?.id ?? "");
    }
  }, [candidates, selectedPostingId]);

  const addProcess = async () => {
    if (!selectedPostingId) return;
    await onStatusChange(selectedPostingId, "INTERVIEWING");
  };

  return (
    <dialog
      aria-labelledby="manage-interviews-title"
      className="manage-interviews-dialog"
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
      ref={dialogRef}
    >
      <div className="manage-interviews-dialog__surface">
        <header className="manage-interviews-dialog__header">
          <div>
            <span className="section-kicker">INTERVIEW PIPELINE</span>
            <h2 id="manage-interviews-title">Manage processes</h2>
          </div>
          <button
            aria-label="Close process manager"
            className="dialog-close"
            onClick={onClose}
            type="button"
          >
            ×
          </button>
        </header>

        <ErrorMessage className="manage-interviews-error" message={error} />

        <section className="manage-interviews-section" aria-labelledby="add-process-title">
          <div className="manage-interviews-section__heading">
            <div>
              <h3 id="add-process-title">Add an interview process</h3>
              <p>Choose an existing job to move it into the interview pipeline.</p>
            </div>
          </div>

          {candidates.length === 0 ? (
            <p className="manage-interviews-message">Every job is already in Interviews.</p>
          ) : (
            <div className="add-process-control">
              <label>
                <span className="visually-hidden">Job to add</span>
                <select
                  onChange={(event) => setSelectedPostingId(event.target.value)}
                  value={selectedPostingId}
                >
                  {candidates.map((posting) => (
                    <option key={posting.id} value={posting.id}>
                      {displayValue(posting.parsedData.company)} — {posting.displayTitle}
                    </option>
                  ))}
                </select>
              </label>
              <button
                className="primary-action"
                disabled={!selectedPostingId || updatingIds.has(selectedPostingId)}
                onClick={() => void addProcess()}
                type="button"
              >
                {updatingIds.has(selectedPostingId) ? "Adding…" : "Add process"}
              </button>
            </div>
          )}
        </section>

        <section className="manage-interviews-section" aria-labelledby="current-processes-title">
          <div className="manage-interviews-section__heading">
            <div>
              <h3 id="current-processes-title">Current processes</h3>
              <p>Move a process to another status when its interview phase changes.</p>
            </div>
            <span>{processes.length}</span>
          </div>

          {processes.length === 0 ? (
            <p className="manage-interviews-message">No active interview processes yet.</p>
          ) : (
            <div className="managed-process-list">
              {processes.map((process) => (
                <ManagedProcessRow
                  key={process.posting.id}
                  isUpdating={updatingIds.has(process.posting.id)}
                  onStatusChange={onStatusChange}
                  posting={process.posting}
                  statuses={statuses}
                />
              ))}
            </div>
          )}
        </section>
      </div>
    </dialog>
  );
}

type ManagedProcessRowProps = {
  isUpdating: boolean;
  onStatusChange: (id: string, status: string) => Promise<boolean>;
  posting: JobPosting;
  statuses: WorkflowStatus[];
};

function ManagedProcessRow({
  isUpdating,
  onStatusChange,
  posting,
  statuses,
}: ManagedProcessRowProps) {
  const [nextStatus, setNextStatus] = useState(posting.workflowStatus);

  useEffect(() => setNextStatus(posting.workflowStatus), [posting.workflowStatus]);

  return (
    <div className="managed-process-row">
      <div>
        <strong>{posting.displayTitle}</strong>
        <span>{displayValue(posting.parsedData.company)}</span>
      </div>
      <label>
        <span className="visually-hidden">Status for {posting.displayTitle}</span>
        <select
          disabled={isUpdating}
          onChange={(event) => setNextStatus(event.target.value)}
          value={nextStatus}
        >
          {statuses.map((status) => (
            <option key={status.code} value={status.code}>{status.label}</option>
          ))}
        </select>
      </label>
      <button
        className="secondary-action"
        disabled={isUpdating || nextStatus === posting.workflowStatus}
        onClick={() => void onStatusChange(posting.id, nextStatus)}
        type="button"
      >
        {isUpdating ? "Saving…" : "Update"}
      </button>
    </div>
  );
}
