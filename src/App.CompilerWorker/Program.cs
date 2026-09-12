using App.CompilerWorker;
using App.Application;
using App.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLocalInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
if (builder.Configuration.GetValue("CompilationWorker:PollingEnabled", true))
{
    builder.Services.AddHostedService<Worker>();
}

var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost(
    "/internal/cv-compilation-jobs",
    async (
        CvCompilationTaskRequest request,
        ICvCompilationJobProcessor processor,
        CancellationToken cancellationToken) =>
    {
        if (request.CvCompileJobId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "cvCompileJobId is required." });
        }

        var outcome = await processor.ProcessAsync(
            request.CvCompileJobId,
            cancellationToken);
        return outcome switch
        {
            CvCompilationProcessingOutcome.Processed => Results.NoContent(),
            CvCompilationProcessingOutcome.AlreadyTerminal => Results.NoContent(),
            CvCompilationProcessingOutcome.NotFound => Results.NoContent(),
            CvCompilationProcessingOutcome.Deferred => Results.StatusCode(
                StatusCodes.Status503ServiceUnavailable),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    });
app.Run();

public sealed record CvCompilationTaskRequest(Guid CvCompileJobId);
