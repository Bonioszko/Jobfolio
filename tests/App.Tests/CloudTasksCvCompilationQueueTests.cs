using System.Text.Json;
using App.Infrastructure;
using Google.Cloud.Tasks.V2;
using Microsoft.Extensions.Configuration;

namespace App.Tests;

public sealed class CloudTasksCvCompilationQueueTests
{
    [Fact]
    public void Creates_id_only_authenticated_http_task()
    {
        var jobId = Guid.NewGuid();
        var options = new CloudTasksCvCompilationQueueOptions(
            "test-project",
            "us-central1",
            "cv-compilation",
            "https://compiler.example/internal/cv-compilation-jobs",
            "task-invoker@test-project.iam.gserviceaccount.com",
            "https://compiler.example");

        var request = CloudTasksCvCompilationQueue.CreateRequest(jobId, options);

        Assert.Equal(
            "projects/test-project/locations/us-central1/queues/cv-compilation",
            request.Parent);
        Assert.EndsWith($"/tasks/cvcompile-{jobId:N}", request.Task.Name);
        Assert.Equal(
            Google.Cloud.Tasks.V2.HttpMethod.Post,
            request.Task.HttpRequest.HttpMethod);
        Assert.Equal(options.TargetUrl, request.Task.HttpRequest.Url);
        Assert.Equal("application/json", request.Task.HttpRequest.Headers["Content-Type"]);
        Assert.Equal(
            options.OidcServiceAccountEmail,
            request.Task.HttpRequest.OidcToken.ServiceAccountEmail);
        Assert.Equal(options.OidcAudience, request.Task.HttpRequest.OidcToken.Audience);

        using var body = JsonDocument.Parse(request.Task.HttpRequest.Body.ToByteArray());
        var property = Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal("cvCompileJobId", property.Name);
        Assert.Equal(jobId, property.Value.GetGuid());
    }

    [Fact]
    public void Cloud_tasks_configuration_requires_https_worker_urls()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Queues:CvCompilation:ProjectId"] = "test-project",
                ["Queues:CvCompilation:Location"] = "us-central1",
                ["Queues:CvCompilation:QueueId"] = "cv-compilation",
                ["Queues:CvCompilation:TargetUrl"] =
                    "http://compiler/internal/cv-compilation-jobs",
                ["Queues:CvCompilation:OidcServiceAccountEmail"] =
                    "task-invoker@test-project.iam.gserviceaccount.com",
                ["Queues:CvCompilation:OidcAudience"] = "https://compiler.example"
            }).Build();

        Assert.Throws<InvalidOperationException>(() =>
            CloudTasksCvCompilationQueueOptions.FromConfiguration(configuration));
    }
}
