using System.Text.Json;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class WorkspaceApplicationService(AppDbContext db) : IWorkspaceApplicationService
{
    public async Task<IReadOnlyList<SourceItemView>> GetSourceItemsAsync(string workspaceKey, string? status, string? source, int limit, CancellationToken cancellationToken)
    {
        var query = db.SourceItems.AsNoTracking().Where(x => x.WorkspaceKey == workspaceKey);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.WorkflowStatus == status);
        if (!string.IsNullOrWhiteSpace(source)) query = query.Where(x => x.SourceKey == source);
        return (await query.OrderByDescending(x => x.SourceReceivedAt).Take(Math.Clamp(limit, 1, 100)).ToListAsync(cancellationToken)).Select(x => Map(x, false)).ToArray();
    }

    public async Task<SourceItemView?> GetSourceItemAsync(string workspaceKey, Guid id, CancellationToken cancellationToken)
    {
        var item = await db.SourceItems.AsNoTracking().SingleOrDefaultAsync(x => x.WorkspaceKey == workspaceKey && x.Id == id, cancellationToken);
        return item is null ? null : Map(item, true);
    }

    public async Task<bool> ChangeStatusAsync(string workspaceKey, Guid id, string status, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var item = await db.SourceItems.SingleOrDefaultAsync(x => x.WorkspaceKey == workspaceKey && x.Id == id, cancellationToken);
        if (item is null) return false;
        var previous = item.WorkflowStatus;
        item.WorkflowStatus = status;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        db.StatusHistory.Add(new StatusHistory { WorkspaceKey = workspaceKey, SourceItemId = id, PreviousStatus = previous, NewStatus = status });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static SourceItemView Map(SourceItem item, bool includeEmail) => new(item.Id, item.SourceKey, item.DisplayTitle,
        JsonSerializer.Deserialize<object>(item.ParsedDataJson, JsonDefaults.Web), JsonSerializer.Deserialize<object>(item.SearchDataJson, JsonDefaults.Web),
        item.WorkflowStatus, item.ParserKey, item.ParserVersion, item.SourceReceivedAt, includeEmail ? item.DemoEmailHtml : null);
}
