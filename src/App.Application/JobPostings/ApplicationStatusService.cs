namespace App.Application;

public sealed class ApplicationStatusService(
    IApplicationStatusStore store,
    IDomainConfigurationProvider domainConfiguration,
    TimeProvider timeProvider) : IApplicationStatusService
{
    public async Task<ChangeApplicationStatusOutcome> ChangeAsync(
        string workspaceKey,
        Guid jobPostingId,
        string status,
        CancellationToken cancellationToken)
    {
        if (!domainConfiguration.IsWorkflowStatusAllowed(status))
        {
            return ChangeApplicationStatusOutcome.InvalidStatus;
        }

        var changed = await store.ChangeAsync(
            workspaceKey,
            jobPostingId,
            status,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return changed
            ? ChangeApplicationStatusOutcome.Updated
            : ChangeApplicationStatusOutcome.NotFound;
    }
}
