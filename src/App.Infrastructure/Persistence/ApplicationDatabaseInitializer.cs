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
            await db.Database.MigrateAsync(cancellationToken);
            return;
        }

        logger.LogWarning(
            "No EF Core migrations were found; creating the local development schema directly.");
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
