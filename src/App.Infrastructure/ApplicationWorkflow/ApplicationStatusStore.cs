using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class ApplicationStatusStore(AppDbContext db) : IApplicationStatusStore
{
    public async Task<bool> ChangeAsync(
        string workspaceKey,
        Guid jobPostingId,
        string status,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var posting = await db.JobPostings.SingleOrDefaultAsync(
            candidate => candidate.WorkspaceKey == workspaceKey && candidate.Id == jobPostingId,
            cancellationToken);

        if (posting is null) return false;

        var statusChanged = !string.Equals(
            posting.ApplicationStatus,
            status,
            StringComparison.Ordinal);
        var recordsApplication = string.Equals(status, "APPLIED", StringComparison.Ordinal) &&
                                 posting.AppliedAt is null;

        if (!statusChanged && !recordsApplication) return true;

        if (recordsApplication)
        {
            posting.AppliedAt = changedAt;
        }

        posting.UpdatedAt = changedAt;
        if (statusChanged)
        {
            var previousStatus = posting.ApplicationStatus;
            posting.ApplicationStatus = status;
            db.ApplicationStatusHistory.Add(new ApplicationStatusHistory
            {
                WorkspaceKey = workspaceKey,
                JobPostingId = jobPostingId,
                PreviousStatus = previousStatus,
                NewStatus = status,
                CreatedAt = changedAt
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
