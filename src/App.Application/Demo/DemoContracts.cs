namespace App.Application;

public sealed record DemoSettings(
    int SessionLifetimeHours,
    int MaxCompilationJobsPerWindow,
    int CompilationWindowMinutes)
{
    public TimeSpan SessionLifetime => TimeSpan.FromHours(SessionLifetimeHours);
    public TimeSpan CompilationWindow => TimeSpan.FromMinutes(CompilationWindowMinutes);
}

public sealed record DemoSessionView(Guid Id, DateTimeOffset ExpiresAt);

public interface IDemoWorkspaceSeeder
{
    Task SeedAsync(Guid sessionId, CancellationToken cancellationToken);
}

public interface IDemoSessionService
{
    Task<DemoSessionView> CreateAsync(CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken);
}
