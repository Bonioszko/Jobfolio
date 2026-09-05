import type { WorkflowStatus } from "../../domain-config/types/domainConfig";
import { statusTone } from "../utils/statusPresentation";

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

          return (
            <button
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
            </button>
          );
        })}
      </div>
    </section>
  );
}
