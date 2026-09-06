import { useEffect } from "react";
import type { WorkflowStatus } from "../../domain-config/types/domainConfig";
import { matchesUnmodifiedShortcut } from "../utils/keyboardShortcut";
import { statusTone } from "../utils/statusPresentation";

const statusShortcuts: Readonly<Record<string, string>> = {
  TO_APPLY: "T",
  SKIP: "S",
};

type JobStatusActionsProps = {
  currentStatus: string;
  isUpdating: boolean;
  statuses: WorkflowStatus[];
  onChange: (status: string) => Promise<void>;
};

export function JobStatusActions({
  currentStatus,
  isUpdating,
  statuses,
  onChange,
}: JobStatusActionsProps) {
  useEffect(() => {
    const changeStatus = (event: KeyboardEvent) => {
      const shortcut = Object.values(statusShortcuts).find((key) =>
        matchesUnmodifiedShortcut(event, key));
      if (!shortcut || isUpdating) return;

      const statusCode = Object.entries(statusShortcuts).find(
        ([, key]) => key === shortcut,
      )?.[0];
      const targetStatus = statuses.find((status) => status.code === statusCode);
      if (!targetStatus || targetStatus.code === currentStatus) return;

      event.preventDefault();
      void onChange(targetStatus.code);
    };

    window.addEventListener("keydown", changeStatus);
    return () => window.removeEventListener("keydown", changeStatus);
  }, [currentStatus, isUpdating, onChange, statuses]);

  return (
    <section className="status-section" aria-labelledby="status-heading">
      <div className="section-heading-row">
        <div>
          <span className="section-kicker">APPLICATION STATE</span>
          <h3 id="status-heading">Move this job</h3>
        </div>
        {isUpdating && <span className="saving-label">Saving…</span>}
      </div>
      <div className="status-actions">
        {statuses.map((status) => {
          const isCurrent = status.code === currentStatus;
          const shortcut = statusShortcuts[status.code];

          return (
            <button
              aria-keyshortcuts={shortcut}
              aria-pressed={isCurrent}
              className="status-action"
              data-tone={statusTone(status.code)}
              disabled={isUpdating || isCurrent}
              key={status.code}
              onClick={() => void onChange(status.code)}
              type="button"
            >
              <span className="status-action__dot" aria-hidden="true" />
              {status.label}
              {shortcut && <kbd className="shortcut-key">{shortcut}</kbd>}
            </button>
          );
        })}
      </div>
    </section>
  );
}
