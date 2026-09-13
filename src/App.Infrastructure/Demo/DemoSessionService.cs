using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class DemoSessionService(
    AppDbContext db,
    IDemoWorkspaceSeeder seeder,
    TimeProvider timeProvider,
    DemoSettings settings) : IDemoSessionService
{
    public async Task<DemoSessionView> CreateAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var session = new DemoSession
        {
            CreatedAt = now,
            ExpiresAt = now.Add(settings.SessionLifetime)
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.DemoSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await seeder.SeedAsync(session.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DemoSessionView(session.Id, session.ExpiresAt);
    }

    public Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return db.DemoSessions
            .AsNoTracking()
            .AnyAsync(session => session.Id == sessionId && session.ExpiresAt > now, cancellationToken);
    }
}
