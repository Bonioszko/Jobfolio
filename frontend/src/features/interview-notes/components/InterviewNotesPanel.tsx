import { type FormEvent, useState } from "react";
import { ErrorMessage } from "../../../components/common/ErrorMessage";
import { useInterviewNotes } from "../hooks/useInterviewNotes";

type InterviewNotesPanelProps = {
  jobPostingId: string;
};

export function InterviewNotesPanel({ jobPostingId }: InterviewNotesPanelProps) {
  const interviewNotes = useInterviewNotes(jobPostingId);
  const [stage, setStage] = useState("");
  const [interviewDate, setInterviewDate] = useState("");
  const [notes, setNotes] = useState("");

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const saved = await interviewNotes.add({ stage, interviewDate, notes });
    if (!saved) return;

    setStage("");
    setInterviewDate("");
    setNotes("");
  };

  return (
    <section className="interview-notes-panel" aria-labelledby="interview-notes-heading">
      <span className="section-kicker">INTERVIEW HISTORY</span>
      <h3 id="interview-notes-heading">Stages and notes</h3>

      <form
        autoComplete="off"
        className="interview-note-form"
        onSubmit={(event) => void submit(event)}
      >
        <div className="interview-note-form__row">
          <label className="form-field">
            <span>Stage</span>
            <input
              autoComplete="off"
              maxLength={100}
              onChange={(event) => setStage(event.target.value)}
              placeholder="Technical interview"
              required
              type="text"
              value={stage}
            />
          </label>
          <label className="form-field">
            <span>Interview date</span>
            <input
              onChange={(event) => setInterviewDate(event.target.value)}
              required
              type="date"
              value={interviewDate}
            />
          </label>
        </div>
        <label className="form-field">
          <span>Notes</span>
          <textarea
            maxLength={10_000}
            onChange={(event) => setNotes(event.target.value)}
            placeholder="Questions, feedback, people met, and next steps…"
            required
            rows={4}
            value={notes}
          />
        </label>
        <button className="primary-action interview-note-submit" disabled={interviewNotes.isSaving}>
          {interviewNotes.isSaving ? "Saving…" : "Add interview stage"}
        </button>
      </form>

      <ErrorMessage message={interviewNotes.error} />
      {interviewNotes.isLoading ? (
        <p className="muted interview-notes-message">Loading interview history…</p>
      ) : interviewNotes.data.length === 0 ? (
        <p className="muted interview-notes-message">No interviews recorded yet.</p>
      ) : (
        <ol className="interview-note-list">
          {interviewNotes.data.map((note) => (
            <li key={note.id}>
              <div>
                <strong>{note.stage}</strong>
                <time dateTime={note.interviewDate}>
                  {formatInterviewDate(note.interviewDate)}
                </time>
              </div>
              <p>{note.notes}</p>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

function formatInterviewDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(`${value}T00:00:00Z`));
}
