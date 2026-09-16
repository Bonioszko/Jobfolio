using System.Text.Json;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class JobPostingQueryService(AppDbContext db) : IJobPostingQueryService
{
    public async Task<JobPostingPage> ListAsync(
        string workspaceKey,
        string? status,
        string? source,
        JobPostingCursor? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = db.JobPostings
            .AsNoTracking()
            .Where(item => item.WorkspaceKey == workspaceKey);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.ApplicationStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(item => item.ProviderKey == source);
        }

        if (cursor is not null)
        {
            query = db.Database.IsNpgsql()
                ? query.Where(item => EF.Functions.LessThan(
                    ValueTuple.Create(item.SourceReceivedAt, item.Id),
                    ValueTuple.Create(cursor.SourceReceivedAt, cursor.Id)))
                : query.Where(item =>
                    item.SourceReceivedAt < cursor.SourceReceivedAt ||
                    (item.SourceReceivedAt == cursor.SourceReceivedAt &&
                     item.Id.CompareTo(cursor.Id) < 0));
        }

        var pageSize = Math.Clamp(limit, 1, 100);
        var items = await query
            .OrderByDescending(item => item.SourceReceivedAt)
            .ThenByDescending(item => item.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = hasMore
            ? new JobPostingCursor(items[^1].SourceReceivedAt, items[^1].Id)
            : null;
        return new JobPostingPage(
            items.Select(item => Map(item, includeEmail: false)).ToArray(),
            nextCursor);
    }

    public async Task<JobPostingView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await db.JobPostings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.WorkspaceKey == workspaceKey && candidate.Id == id,
                cancellationToken);

        return item is null ? null : Map(item, includeEmail: true);
    }

    private static JobPostingView Map(JobPosting item, bool includeEmail) =>
        new(
            item.Id,
            item.ProviderKey,
            item.Title,
            JsonSerializer.Deserialize<object>(item.NormalizedDataJson, JsonDefaults.Web),
            JsonSerializer.Deserialize<object>(item.SearchDataJson, JsonDefaults.Web),
            item.ApplicationStatus,
            item.ParserKey,
            item.ParserVersion,
            item.SourceReceivedAt,
            item.AppliedAt,
            includeEmail ? item.DemoEmailHtml : null);
}
