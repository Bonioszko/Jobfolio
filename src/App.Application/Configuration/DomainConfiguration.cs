namespace App.Application;

public sealed record DomainLabel(string Singular, string Plural);

public sealed record DomainField(string Key, string Label, string Type, bool ShowInDashboard);

public sealed record WorkflowStatusDefinition(string Code, string Label);

public sealed record DomainConfiguration(
    DomainLabel SourceItem,
    DomainLabel GeneratedDocument,
    IReadOnlyList<DomainField> Fields,
    IReadOnlyList<WorkflowStatusDefinition> Statuses)
{
    public void Validate()
    {
        ValidateLabel(SourceItem, nameof(SourceItem));
        ValidateLabel(GeneratedDocument, nameof(GeneratedDocument));
        ValidateFields();
        ValidateStatuses();
    }

    private static void ValidateLabel(DomainLabel label, string name)
    {
        if (string.IsNullOrWhiteSpace(label.Singular) || string.IsNullOrWhiteSpace(label.Plural))
        {
            throw new InvalidOperationException($"{name} labels are required.");
        }
    }

    private void ValidateFields()
    {
        if (Fields.Count == 0 || Fields.Any(field =>
                string.IsNullOrWhiteSpace(field.Key) || string.IsNullOrWhiteSpace(field.Label)))
        {
            throw new InvalidOperationException("Domain fields must have keys and labels.");
        }

        if (Fields.Select(field => field.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Fields.Count)
        {
            throw new InvalidOperationException("Domain field keys must be unique.");
        }
    }

    private void ValidateStatuses()
    {
        if (Statuses.Count == 0 || Statuses.Any(status =>
                string.IsNullOrWhiteSpace(status.Code) || string.IsNullOrWhiteSpace(status.Label)))
        {
            throw new InvalidOperationException("Workflow statuses must have codes and labels.");
        }

        if (Statuses.Select(status => status.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Statuses.Count)
        {
            throw new InvalidOperationException("Workflow status codes must be unique.");
        }
    }
}

public interface IDomainConfigurationProvider
{
    DomainConfiguration Current { get; }
    bool IsWorkflowStatusAllowed(string status);
}

public interface ISystemRulesProvider
{
    string GenerationRulesMarkdown { get; }
    string GenerationRulesHash { get; }
}
