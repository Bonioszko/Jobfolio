using System.Text.Json;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class JobPostingQueryService(AppDbContext db) : IJobPostingQueryService
{
    public async Task<IReadOnlyList<JobPostingView>> ListAsync(
        string workspaceKey,
        string? status,
        string? source,
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

        var items = await query
            .OrderByDescending(item => item.SourceReceivedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        return items.Select(item => Map(item, includeEmail: false)).ToArray();
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
            includeEmail ? item.DemoEmailHtml : null);
}
