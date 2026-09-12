import { useMemo, useState, type DragEvent } from "react";
import type { WorkflowStatus } from "../../domain-config/types/domainConfig";
import type { JobPosting } from "../../job-postings/types/jobPosting";
import { displayValue } from "../../job-postings/utils/displayValue";
import { statusTone } from "../../job-postings/utils/statusPresentation";
import { groupApplicationsByStatus } from "../utils/applicationKanban";

type ApplicationKanbanBoardProps = {
  postings: JobPosting[];
  statuses: WorkflowStatus[];
  updatingIds: Set<string>;
  onStatusChange: (id: string, status: string) => Promise<boolean>;
};

export function ApplicationKanbanBoard({
  postings,
  statuses,
  updatingIds,
  onStatusChange,
}: ApplicationKanbanBoardProps) {
  const [draggingId, setDraggingId] = useState<string>();
  const [dragOverStatus, setDragOverStatus] = useState<string>();
  const columns = useMemo(
    () => groupApplicationsByStatus(postings, statuses),
    [postings, statuses],
  );
  const availableStatuses = columns.map((column) => column.status);

  const movePosting = async (id: string, status: string) => {
    const posting = postings.find((item) => item.id === id);
    if (!posting || posting.workflowStatus === status || updatingIds.has(id)) {
      return;
    }

    await onStatusChange(id, status);
  };

  const handleDrop = (event: DragEvent<HTMLElement>, status: string) => {
    event.preventDefault();
    const postingId = event.dataTransfer.getData("text/plain");
    setDraggingId(undefined);
    setDragOverStatus(undefined);
    void movePosting(postingId, status);
  };

  return (
    <div className="application-kanban" aria-label="Application Kanban board">
      {columns.map((column) => (
        <section
          className="kanban-column"
          data-drag-over={dragOverStatus === column.status.code}
          key={column.status.code}
          onDragOver={(event) => {
            event.preventDefault();
            event.dataTransfer.dropEffect = "move";
            setDragOverStatus(column.status.code);
          }}
          onDrop={(event) => handleDrop(event, column.status.code)}
        >
          <header className="kanban-column__header">
            <span
              aria-hidden="true"
              className="kanban-column__dot"
              data-tone={statusTone(column.status.code)}
            />
            <h2>{column.status.label}</h2>
            <span className="kanban-column__count">{column.postings.length}</span>
          </header>

          <div className="kanban-column__cards">
            {column.postings.length === 0 ? (
              <p className="kanban-column__empty">No applications in this stage.</p>
            ) : (
              column.postings.map((posting) => {
                const isUpdating = updatingIds.has(posting.id);

                return (
                  <article
                    className="kanban-card"
                    data-dragging={draggingId === posting.id}
                    draggable={!isUpdating}
                    key={posting.id}
                    onDragEnd={() => {
                      setDraggingId(undefined);
                      setDragOverStatus(undefined);
                    }}
                    onDragStart={(event) => {
                      event.dataTransfer.effectAllowed = "move";
                      event.dataTransfer.setData("text/plain", posting.id);
                      setDraggingId(posting.id);
                    }}
                  >
                    <div className="kanban-card__source">
                      <span>{posting.sourceKey}</span>
                      {isUpdating && <span role="status">Moving…</span>}
                    </div>
                    <a href={`/?job=${encodeURIComponent(posting.id)}`}>
                      {posting.displayTitle}
                    </a>
                    <p>{displayValue(posting.parsedData.company)}</p>
                    <p className="kanban-card__location">
                      {displayValue(posting.parsedData.location)}
                    </p>
                    <label className="kanban-card__status">
                      <span>Stage</span>
                      <select
                        aria-label={`Move ${posting.displayTitle} to stage`}
                        disabled={isUpdating}
                        onChange={(event) =>
                          void movePosting(posting.id, event.target.value)
                        }
                        value={posting.workflowStatus}
                      >
                        {availableStatuses.map((status) => (
                          <option key={status.code} value={status.code}>
                            {status.label}
                          </option>
                        ))}
                      </select>
                    </label>
                  </article>
                );
              })
            )}
          </div>
        </section>
      ))}
    </div>
  );
}
