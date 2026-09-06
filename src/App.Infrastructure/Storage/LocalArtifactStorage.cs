using System.Security.Cryptography;
using App.Application;

namespace App.Infrastructure;

public sealed class LocalArtifactStorage : IArtifactStorage
{
    private readonly string _root;

    public LocalArtifactStorage(string? configuredRoot)
    {
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? ".artifacts"
            : configuredRoot;
        _root = Path.IsPathRooted(root)
            ? Path.GetFullPath(root)
            : Path.GetFullPath(Path.Combine(RepositoryFileLocator.FindRepositoryRoot(), root));
    }

    public async Task<StoredArtifact> SaveAsync(
        string workspaceKey,
        Guid artifactId,
        byte[] data,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceKey);
        ArgumentNullException.ThrowIfNull(data);

        var workspaceDirectory = GetWorkspaceDirectoryName(workspaceKey);
        var key = Path.Combine(workspaceDirectory, $"{artifactId:N}.pdf");
        var fullPath = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (File.Exists(fullPath))
        {
            var existingData = await File.ReadAllBytesAsync(fullPath, cancellationToken);
            return CreateStoredArtifact(artifactId, key, existingData);
        }

        await using (var stream = new FileStream(
                         fullPath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 81_920,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await stream.WriteAsync(data, cancellationToken);
        }

        return CreateStoredArtifact(artifactId, key, data);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = ResolvePath(key);

        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81_920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteWorkspaceAsync(string workspaceKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceKey);
        cancellationToken.ThrowIfCancellationRequested();
        var directory = ResolvePath(GetWorkspaceDirectoryName(workspaceKey));
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        return Task.CompletedTask;
    }

    private string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Artifact paths must be relative.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var relativeToRoot = Path.GetRelativePath(_root, fullPath);

        if (relativeToRoot.Equals("..", StringComparison.Ordinal) ||
            relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativeToRoot))
        {
            throw new InvalidOperationException("Artifact path escapes the configured storage root.");
        }

        return fullPath;
    }

    private static StoredArtifact CreateStoredArtifact(
        Guid artifactId,
        string key,
        byte[] data) =>
        new(
            artifactId,
            key.Replace(Path.DirectorySeparatorChar, '/'),
            Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(),
            data.LongLength);

    private static string GetWorkspaceDirectoryName(string workspaceKey)
    {
        if (workspaceKey.StartsWith("demo:", StringComparison.Ordinal) &&
            Guid.TryParse(workspaceKey["demo:".Length..], out _))
        {
            return workspaceKey.Replace(':', '-');
        }

        if (workspaceKey.StartsWith("user:", StringComparison.Ordinal))
        {
            var userId = workspaceKey["user:".Length..];
            var isSafe = userId.Length is > 0 and <= 200 && userId.All(character =>
                char.IsLetterOrDigit(character) || character is '.' or '_' or '@' or '+' or '-');
            if (isSafe) return workspaceKey.Replace(':', '-');
        }

        throw new InvalidOperationException("The workspace key cannot be mapped to artifact storage.");
    }
}
