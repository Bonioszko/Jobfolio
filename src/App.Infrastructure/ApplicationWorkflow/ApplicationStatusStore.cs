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
        if (string.Equals(posting.ApplicationStatus, status, StringComparison.Ordinal)) return true;

        var previousStatus = posting.ApplicationStatus;
        posting.ApplicationStatus = status;
        posting.UpdatedAt = changedAt;
        db.ApplicationStatusHistory.Add(new ApplicationStatusHistory
        {
            WorkspaceKey = workspaceKey,
            JobPostingId = jobPostingId,
            PreviousStatus = previousStatus,
            NewStatus = status
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
