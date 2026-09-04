using System.Security.Claims;
using System.Text.Json;
using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace App.Api;

public static class ApiEndpoints
{
    public static WebApplication MapLocalApi(this WebApplication app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/api/config/domain", () => Results.File(FindFile("config", "domain.json"), "application/json"));
        app.MapPost("/api/auth/demo", CreateDemoAsync).RequireRateLimiting("demo-session");
        app.MapPost("/api/auth/logout", async (HttpContext http) => { await http.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
        app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new { userId = user.FindFirstValue(ClaimTypes.NameIdentifier), mode = user.FindFirstValue("mode") })).RequireAuthorization();
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/source-items", GetItemsAsync);
        api.MapGet("/source-items/{id:guid}", GetItemAsync);
        api.MapPut("/source-items/{id:guid}/status", ChangeStatusAsync);
        api.MapGet("/templates", async (ClaimsPrincipal user, IDocumentWorkflowService service, CancellationToken ct) => Results.Ok(await service.GetTemplatesAsync(Workspace(user), ct)));
        api.MapGet("/rules", async (ClaimsPrincipal user, IDocumentWorkflowService service, CancellationToken ct) => Results.Ok(await service.GetRulesAsync(Workspace(user), ct)));
        api.MapPost("/generation-jobs", CreateGenerationAsync);
        api.MapGet("/generation-jobs/{id:guid}", async (Guid id, ClaimsPrincipal user, IDocumentWorkflowService service, CancellationToken ct) => JobResult(await service.GetGenerationAsync(Workspace(user), id, ct)));
        api.MapGet("/documents/{id:guid}", async (Guid id, ClaimsPrincipal user, IDocumentWorkflowService service, CancellationToken ct) => DocumentResult(await service.GetDocumentAsync(Workspace(user), id, ct)));
        api.MapPost("/compile-jobs", CreateCompileAsync);
        api.MapGet("/compile-jobs/{id:guid}", async (Guid id, ClaimsPrincipal user, IDocumentWorkflowService service, CancellationToken ct) => JobResult(await service.GetCompileAsync(Workspace(user), id, ct)));
        api.MapGet("/pdf-artifacts/{id:guid}/download", DownloadPdfAsync);
        return app;
    }

    private static async Task<IResult> CreateDemoAsync(HttpContext http, AppDbContext db, IDemoWorkspaceSeeder seeder, CancellationToken cancellationToken)
    {
        var session = new DemoSession { ExpiresAt = DateTimeOffset.UtcNow.AddHours(6) };
        db.DemoSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await seeder.SeedAsync(session.Id, cancellationToken);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "demo"), new Claim("mode", "Demo"), new Claim("demo_session_id", session.Id.ToString()) };
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Results.Ok(new { mode = "demo", expiresAt = session.ExpiresAt });
    }

    private static async Task<IResult> GetItemsAsync(ClaimsPrincipal user, IWorkspaceApplicationService service, string? status, string? source, int? limit, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetSourceItemsAsync(Workspace(user), status, source, limit ?? 50, cancellationToken));

    private static async Task<IResult> GetItemAsync(Guid id, ClaimsPrincipal user, IWorkspaceApplicationService service, CancellationToken cancellationToken)
    {
        var item = await service.GetSourceItemAsync(Workspace(user), id, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> ChangeStatusAsync(Guid id, StatusRequest request, ClaimsPrincipal user, IWorkspaceApplicationService service, CancellationToken cancellationToken)
    {
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(FindFile("config", "domain.json"), cancellationToken));
        var allowed = json.RootElement.GetProperty("statuses").EnumerateArray().Select(x => x.GetProperty("code").GetString()).ToHashSet();
        if (!allowed.Contains(request.Status)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Unknown workflow status."] });
        return await service.ChangeStatusAsync(Workspace(user), id, request.Status, cancellationToken) ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> CreateGenerationAsync(GenerationRequest request, ClaimsPrincipal user, IDocumentWorkflowService service, CancellationToken ct)
    {
        var job = await service.CreateGenerationAsync(Workspace(user), request.SourceItemId, request.TemplateVersionId, request.RuleVersionId, request.Instruction, ct);
        return job is null ? Results.BadRequest("Invalid generation inputs or demo quota reached.") : Results.Accepted($"/api/generation-jobs/{job.Id}", job);
    }

    private static async Task<IResult> CreateCompileAsync(CompileRequest request, ClaimsPrincipal user, IDocumentWorkflowService service, CancellationToken ct)
    {
        var job = await service.CreateCompileAsync(Workspace(user), request.DocumentVersionId, ct);
        return job is null ? Results.BadRequest("Invalid document version or demo quota reached.") : Results.Accepted($"/api/compile-jobs/{job.Id}", job);
    }

    private static async Task<IResult> DownloadPdfAsync(Guid id, ClaimsPrincipal user, AppDbContext db, IArtifactStorage storage, CancellationToken ct)
    {
        var artifact = await db.PdfArtifacts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceKey == Workspace(user), ct);
        if (artifact is null) return Results.NotFound(); var stream = await storage.OpenReadAsync(artifact.ObjectKey, ct);
        return stream is null ? Results.NotFound() : Results.File(stream, "application/pdf", $"document-{id}.pdf", enableRangeProcessing: true);
    }

    private static IResult JobResult(JobView? job) => job is null ? Results.NotFound() : Results.Ok(job);
    private static IResult DocumentResult(DocumentView? document) => document is null ? Results.NotFound() : Results.Ok(document);

    private static string Workspace(ClaimsPrincipal user) => user.FindFirstValue("mode") == "Demo"
        ? $"demo:{user.FindFirstValue("demo_session_id") ?? throw new UnauthorizedAccessException()}"
        : $"user:{user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException()}";

    private static string FindFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(path)) return path;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(string.Join('/', segments));
    }

    private sealed record StatusRequest(string Status);
    private sealed record GenerationRequest(Guid SourceItemId, Guid TemplateVersionId, Guid RuleVersionId, string? Instruction);
    private sealed record CompileRequest(Guid DocumentVersionId);
}
