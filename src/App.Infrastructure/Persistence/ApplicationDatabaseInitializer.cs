using App.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure;

public sealed class ApplicationDatabaseInitializer(
    AppDbContext db,
    ILogger<ApplicationDatabaseInitializer> logger) : IApplicationDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var migrations = db.Database.GetMigrations();

        if (migrations.Any())
        {
            // This repository originally used EnsureCreated without migration history.
            // On a fresh database this creates the complete current model; on an existing
            // database it is a no-op. The idempotent baseline migration can then safely run.
            await db.Database.EnsureCreatedAsync(cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
            return;
        }

        logger.LogWarning(
            "No EF Core migrations were found; creating the local development schema directly.");
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
