using App.Application;

namespace App.Tests;

public sealed class CvGeneratorResolverTests
{
    [Fact]
    public void Resolves_generator_case_insensitively()
    {
        var generator = new StubGenerator("local-codex");
        var resolver = new CvGeneratorResolver([generator]);

        Assert.Same(generator, resolver.Resolve("LOCAL-CODEX"));
    }

    [Fact]
    public void Rejects_unregistered_generator()
    {
        var resolver = new CvGeneratorResolver([]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.Resolve("openai-api"));

        Assert.Contains("not registered", exception.Message);
    }

    [Fact]
    public void Rejects_duplicate_generator_names()
    {
        Assert.Throws<InvalidOperationException>(() => new CvGeneratorResolver(
            [new StubGenerator("same"), new StubGenerator("SAME")]));
    }

    private sealed class StubGenerator(string name) : IAiCvGenerator
    {
        public string Name => name;

        public Task<AiCvGenerationResult> GenerateAsync(
            AiCvGenerationRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
