using System.Text;
using App.Infrastructure;

namespace App.Tests;

public sealed class LocalArtifactStorageTests
{
    [Fact]
    public async Task Saves_and_reads_artifact_beneath_workspace_directory()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var storage = new LocalArtifactStorage(root);
            var data = Encoding.UTF8.GetBytes("private pdf");
            var workspaceKey = $"demo:{Guid.NewGuid()}";

            var stored = await storage.SaveAsync(
                workspaceKey,
                Guid.NewGuid(),
                data,
                CancellationToken.None);
            await using var content = await storage.OpenReadAsync(
                stored.Key,
                CancellationToken.None);

            Assert.NotNull(content);
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer);
            Assert.Equal(data, buffer.ToArray());
            Assert.NotEqual(Guid.Empty, stored.Id);
            Assert.Equal(64, stored.Sha256.Length);
            Assert.Equal(data.Length, stored.Size);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Rejects_artifact_path_outside_storage_root()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var storage = new LocalArtifactStorage(root);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.OpenReadAsync("../outside.pdf", CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Repeated_save_for_same_job_is_idempotent()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var storage = new LocalArtifactStorage(root);
            var workspaceKey = $"demo:{Guid.NewGuid()}";
            var artifactId = Guid.NewGuid();
            var original = Encoding.UTF8.GetBytes("first completed compile");

            var first = await storage.SaveAsync(
                workspaceKey,
                artifactId,
                original,
                CancellationToken.None);
            var repeated = await storage.SaveAsync(
                workspaceKey,
                artifactId,
                Encoding.UTF8.GetBytes("retry output"),
                CancellationToken.None);

            Assert.Equal(first, repeated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"job-parser-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
