using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using App.Application;
using App.Domain;
using App.Parsers;
using App.Parsers.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Postgres") ?? "Host=localhost;Port=5432;Database=jobparser;Username=jobparser;Password=jobparser";
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connection));
        services.AddScoped<IDemoWorkspaceSeeder, DemoWorkspaceSeeder>();
        services.AddScoped<IWorkspaceApplicationService, WorkspaceApplicationService>();
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddScoped<JobProcessor>();
        services.AddSingleton<IGenerationQueue, PostgresGenerationQueue>();
        services.AddSingleton<ICompilationQueue, PostgresCompilationQueue>();
        services.AddSingleton<ISourceParser, LinkedInJobParser>();
        services.AddSingleton<ISourceParser, JustJoinItJobParser>();
        services.AddSingleton<ISourceParser, NoFluffJobsParser>();
        services.AddSingleton<ISourceParserRegistry, SourceParserRegistry>();
        services.AddSingleton<IAiDocumentGenerator, DemoDocumentGenerator>();
        services.AddSingleton<ITexCompiler, TectonicCompiler>();
        services.AddSingleton<IArtifactStorage, LocalArtifactStorage>();
        return services;
    }
}

public sealed class DemoWorkspaceSeeder(AppDbContext db, ISourceParserRegistry registry) : IDemoWorkspaceSeeder
{
    private static readonly (string File, string Sender)[] Fixtures =
    {
        ("linkedin-job-01.html", "jobs-noreply@linkedin.com"), ("linkedin-job-02.html", "jobs-noreply@linkedin.com"),
        ("justjoinit-job-01.html", "alerts@justjoin.it"), ("justjoinit-job-02.html", "alerts@justjoin.it"),
        ("nofluffjobs-job-01.html", "jobs@nofluffjobs.com"), ("nofluffjobs-job-02.html", "jobs@nofluffjobs.com")
    };

    public async Task SeedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var workspace = $"demo:{sessionId}";
        var fixtureRoot = FindRepoFile("demo-fixtures", "emails");
        foreach (var (file, sender) in Fixtures)
        {
            var html = await File.ReadAllTextAsync(Path.Combine(fixtureRoot, file), cancellationToken);
            var email = new EmailMessage(file, sender, "New job matching your alert", html, DateTimeOffset.UtcNow.AddMinutes(-Array.IndexOf(Fixtures, (file, sender)) * 17));
            var selection = registry.Select(email);
            if (selection.Match != ParserMatch.Matched) throw new InvalidOperationException($"Fixture {file} did not match exactly one parser.");
            var result = await selection.Parser!.ParseAsync(email, cancellationToken);
            db.SourceItems.Add(new SourceItem
            {
                WorkspaceKey = workspace, SourceKey = result.SourceKey, SourceExternalId = result.SourceExternalId,
                DisplayTitle = result.DisplayTitle, ParsedDataJson = JsonSerializer.Serialize(result.ParsedData, JsonDefaults.Web),
                SearchDataJson = JsonSerializer.Serialize(result.SearchData, JsonDefaults.Web), WorkflowStatus = "NEW",
                ParserKey = selection.Parser.Key, ParserVersion = selection.Parser.Version, SourceReceivedAt = email.ReceivedAt,
                DemoEmailHtml = html
            });
        }
        var templateRoot = FindRepoFile("example-templates");
        foreach (var file in Directory.GetFiles(templateRoot, "*.tex").Order())
        {
            var template = new DocumentTemplate { WorkspaceKey = workspace, Name = Path.GetFileNameWithoutExtension(file), CurrentVersion = 1 };
            db.DocumentTemplates.Add(template);
            db.DocumentTemplateVersions.Add(new DocumentTemplateVersion { WorkspaceKey = workspace, TemplateId = template.Id, Version = 1, Tex = await File.ReadAllTextAsync(file, cancellationToken) });
        }
        var rules = new UserRuleDocument { WorkspaceKey = workspace, CurrentVersion = 1 };
        db.UserRuleDocuments.Add(rules);
        db.UserRuleVersions.Add(new UserRuleVersion { WorkspaceKey = workspace, RuleDocumentId = rules.Id, Version = 1, Markdown = "- Use a concise, professional tone.\n- Include producer and price.\n- Do not invent product facts." });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string FindRepoFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = segments.Aggregate(directory.FullName, Path.Combine);
            if (Directory.Exists(path)) return path;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException(string.Join(Path.DirectorySeparatorChar, segments));
    }
}

public sealed class DemoDocumentGenerator : IAiDocumentGenerator
{
    public Task<string> GenerateAsync(string sourceSnapshotJson, string template, string rules, string? instruction, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var json = JsonDocument.Parse(sourceSnapshotJson);
        var root = json.RootElement;
        var title = Escape(root.GetProperty("displayTitle").GetString() ?? "Item");
        var parsed = root.GetProperty("parsedData");
        var producer = Escape(parsed.GetProperty("producer").GetString() ?? "Unknown");
        var price = parsed.GetProperty("price");
        var amount = price.GetProperty("amount").GetDecimal().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var content = $"\\textbf{{Producer:}} {producer}\\\\\n\\textbf{{Price:}} {amount} PLN\\\\\n\nA concise demo description generated deterministically from the parsed email.";
        return Task.FromResult(template.Replace("{{title}}", title, StringComparison.Ordinal).Replace("{{content}}", content, StringComparison.Ordinal));
    }
    private static string Escape(string value) => Regex.Replace(value, @"([#$%&_{}])", @"\$1");
}

public static partial class TexSafety
{
    public static void Validate(string tex)
    {
        if (tex.Length > 200_000) throw new InvalidOperationException("TeX input exceeds 200 KB.");
        if (UnsafePattern().IsMatch(tex)) throw new InvalidOperationException("Unsafe TeX command or path detected.");
    }
    [GeneratedRegex(@"\\(write18|immediate|openout|read|input|include)\b|\.\.[\\/]|(?:^|\s)[A-Za-z]:[\\/]", RegexOptions.IgnoreCase)]
    private static partial Regex UnsafePattern();
}

public sealed class TectonicCompiler : ITexCompiler
{
    public async Task<byte[]> CompileAsync(string tex, CancellationToken cancellationToken)
    {
        TexSafety.Validate(tex);
        var directory = Path.Combine(Path.GetTempPath(), "jobparser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var input = Path.Combine(directory, "main.tex");
            await File.WriteAllTextAsync(input, tex, cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(25));
            var start = new ProcessStartInfo("tectonic", $"-X compile --untrusted --only-cached --outdir \"{directory}\" \"{input}\"")
            { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start Tectonic.");
            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(timeout.Token);
                throw new InvalidOperationException(error[..Math.Min(2000, error.Length)]);
            }
            return await File.ReadAllBytesAsync(Path.Combine(directory, "main.pdf"), timeout.Token);
        }
        catch (System.ComponentModel.Win32Exception ex) { throw new InvalidOperationException("Tectonic is not installed or not on PATH.", ex); }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}

public sealed class LocalArtifactStorage : IArtifactStorage
{
    private readonly string _root = Path.GetFullPath(Environment.GetEnvironmentVariable("ARTIFACT_ROOT") ?? Path.Combine(AppContext.BaseDirectory, "artifacts"));
    public async Task<(string Key, string Sha256, long Size)> SaveAsync(string workspaceKey, Guid artifactId, byte[] data, CancellationToken cancellationToken)
    {
        var safeWorkspace = workspaceKey.Replace(':', '-');
        var key = Path.Combine(safeWorkspace, artifactId + ".pdf");
        var fullPath = Path.GetFullPath(Path.Combine(_root, key));
        if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid artifact path.");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, data, cancellationToken);
        return (key.Replace('\\', '/'), Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(), data.LongLength);
    }
    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, key));
        if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }
    public Task DeleteWorkspaceAsync(string workspaceKey, CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(Path.Combine(_root, workspaceKey.Replace(':', '-')));
        if (path.StartsWith(_root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(path)) Directory.Delete(path, true);
        return Task.CompletedTask;
    }
}
