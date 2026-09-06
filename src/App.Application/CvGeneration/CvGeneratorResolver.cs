namespace App.Application;

public sealed class CvGeneratorResolver : IAiCvGeneratorResolver
{
    private readonly IReadOnlyDictionary<string, IAiCvGenerator> generators;

    public CvGeneratorResolver(IEnumerable<IAiCvGenerator> generators)
    {
        ArgumentNullException.ThrowIfNull(generators);

        try
        {
            this.generators = generators.ToDictionary(
                generator => generator.Name,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "CV generator names must be unique.",
                exception);
        }
    }

    public IAiCvGenerator Resolve(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !generators.TryGetValue(name, out var generator))
        {
            throw new InvalidOperationException(
                $"CV generator '{name}' is not registered.");
        }

        return generator;
    }
}
