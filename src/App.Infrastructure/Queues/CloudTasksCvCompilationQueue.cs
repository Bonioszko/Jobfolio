using System.Text.Json;
using App.Application;
using Google.Cloud.Tasks.V2;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using CloudTask = Google.Cloud.Tasks.V2.Task;

namespace App.Infrastructure;

public sealed record CloudTasksCvCompilationQueueOptions(
    string ProjectId,
    string Location,
    string QueueId,
    string TargetUrl,
    string OidcServiceAccountEmail,
    string OidcAudience)
{
    internal static CloudTasksCvCompilationQueueOptions FromConfiguration(
        IConfiguration configuration)
    {
        const string section = "Queues:CvCompilation";
        var projectId = GetRequired(configuration, $"{section}:ProjectId");
        var location = GetRequired(configuration, $"{section}:Location");
        var queueId = GetRequired(configuration, $"{section}:QueueId");
        var targetUrl = GetRequiredHttpsUrl(configuration, $"{section}:TargetUrl");
        var serviceAccount = GetRequired(configuration, $"{section}:OidcServiceAccountEmail");
        var audience = GetRequiredHttpsUrl(configuration, $"{section}:OidcAudience");
        return new CloudTasksCvCompilationQueueOptions(
            projectId,
            location,
            queueId,
            targetUrl,
            serviceAccount,
            audience);
    }

    private static string GetRequired(IConfiguration configuration, string key) =>
        string.IsNullOrWhiteSpace(configuration[key])
            ? throw new InvalidOperationException($"Configuration value '{key}' is required.")
            : configuration[key]!;

    private static string GetRequiredHttpsUrl(IConfiguration configuration, string key)
    {
        var value = GetRequired(configuration, key);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be an absolute HTTPS URL.");
        }

        return uri.AbsoluteUri.TrimEnd('/');
    }
}

public sealed class CloudTasksCvCompilationQueue(
    CloudTasksClient client,
    CloudTasksCvCompilationQueueOptions options) : ICvCompilationQueue
{
    public async System.Threading.Tasks.Task EnqueueAsync(
        Guid cvCompileJobId,
        CancellationToken cancellationToken)
    {
        if (cvCompileJobId == Guid.Empty)
        {
            throw new ArgumentException("A compilation job ID is required.", nameof(cvCompileJobId));
        }

        var request = CreateRequest(cvCompileJobId, options);
        try
        {
            await client.CreateTaskAsync(request, cancellationToken);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.AlreadyExists)
        {
            // A retried API request may enqueue the same persistent job more than once.
        }
    }

    internal static CreateTaskRequest CreateRequest(
        Guid cvCompileJobId,
        CloudTasksCvCompilationQueueOptions options)
    {
        var queueName = new QueueName(options.ProjectId, options.Location, options.QueueId);
        var taskName = new TaskName(
            options.ProjectId,
            options.Location,
            options.QueueId,
            $"cvcompile-{cvCompileJobId:N}");
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new CvCompilationTaskPayload(cvCompileJobId),
            JsonSerializerOptions.Web);

        return new CreateTaskRequest
        {
            ParentAsQueueName = queueName,
            Task = new CloudTask
            {
                TaskName = taskName,
                HttpRequest = new HttpRequest
                {
                    HttpMethod = Google.Cloud.Tasks.V2.HttpMethod.Post,
                    Url = options.TargetUrl,
                    Body = ByteString.CopyFrom(payload),
                    OidcToken = new OidcToken
                    {
                        ServiceAccountEmail = options.OidcServiceAccountEmail,
                        Audience = options.OidcAudience
                    },
                    Headers = { ["Content-Type"] = "application/json" }
                }
            }
        };
    }

    private sealed record CvCompilationTaskPayload(Guid CvCompileJobId);
}
