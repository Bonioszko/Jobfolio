using System.Net;
using System.Security.Cryptography;
using App.Application;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure;

public sealed record GoogleCloudArtifactStorageOptions(string Bucket)
{
    internal static GoogleCloudArtifactStorageOptions FromConfiguration(
        IConfiguration configuration)
    {
        var bucket = configuration["Artifacts:Bucket"];
        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new InvalidOperationException(
                "Configuration value 'Artifacts:Bucket' is required.");
        }

        return new GoogleCloudArtifactStorageOptions(bucket);
    }
}

public sealed class GoogleCloudArtifactStorage(
    StorageClient client,
    GoogleCloudArtifactStorageOptions options) : IArtifactStorage
{
    public async Task<StoredArtifact> SaveAsync(
        string workspaceKey,
        Guid artifactId,
        byte[] data,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        var key = ArtifactObjectKey.Create(workspaceKey, artifactId);
        var hash = Hash(data);

        try
        {
            await using var source = new MemoryStream(data, writable: false);
            var uploaded = await client.UploadObjectAsync(
                options.Bucket,
                key,
                "application/pdf",
                source,
                new UploadObjectOptions { IfGenerationMatch = 0 },
                cancellationToken);
            return new StoredArtifact(
                artifactId,
                key,
                hash,
                checked((long)(uploaded.Size ?? (ulong)data.LongLength)));
        }
        catch (GoogleApiException exception)
            when (exception.HttpStatusCode == HttpStatusCode.PreconditionFailed)
        {
            var existing = await DownloadAsync(key, cancellationToken)
                ?? throw new InvalidOperationException(
                    "The existing artifact disappeared while resolving an idempotent upload.");
            return new StoredArtifact(
                artifactId,
                key,
                Hash(existing),
                existing.LongLength);
        }
    }

    public async Task<Stream?> OpenReadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        ArtifactObjectKey.Validate(key);
        var data = await DownloadAsync(key, cancellationToken);
        return data is null ? null : new MemoryStream(data, writable: false);
    }

    public async Task DeleteWorkspaceAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var prefix = $"{ArtifactObjectKey.GetWorkspacePrefix(workspaceKey)}/";
        await foreach (var item in client
                           .ListObjectsAsync(options.Bucket, prefix)
                           .WithCancellation(cancellationToken))
        {
            await client.DeleteObjectAsync(
                options.Bucket,
                item.Name,
                cancellationToken: cancellationToken);
        }
    }

    private async Task<byte[]?> DownloadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var destination = new MemoryStream();
            await client.DownloadObjectAsync(
                options.Bucket,
                key,
                destination,
                cancellationToken: cancellationToken);
            return destination.ToArray();
        }
        catch (GoogleApiException exception)
            when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static string Hash(byte[] data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
}
