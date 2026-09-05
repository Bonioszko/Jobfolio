namespace App.Domain;

public enum JobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    TimedOut
}
