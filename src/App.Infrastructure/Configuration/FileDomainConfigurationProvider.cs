using System.Text.Json;
using App.Application;
using App.Domain;

namespace App.Infrastructure;

public sealed class FileDomainConfigurationProvider : IDomainConfigurationProvider
{
    private readonly HashSet<string> _workflowStatuses;

    public FileDomainConfigurationProvider()
    {
        var path = RepositoryFileLocator.FindFile("config", "domain.json");
        var json = File.ReadAllText(path);
        Current = JsonSerializer.Deserialize<DomainConfiguration>(json, JsonDefaults.Web)
            ?? throw new InvalidOperationException("Domain configuration is empty.");
        Current.Validate();
        _workflowStatuses = Current.Statuses
            .Select(status => status.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public DomainConfiguration Current { get; }

    public bool IsWorkflowStatusAllowed(string status) =>
        !string.IsNullOrWhiteSpace(status) && _workflowStatuses.Contains(status);
}
