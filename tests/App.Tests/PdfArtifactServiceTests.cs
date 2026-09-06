using System.Text;
using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class PdfArtifactServiceTests
{
    [Fact]
    public async Task Opens_artifact_only_for_its_owning_workspace()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var artifactId = Guid.NewGuid();
        db.PdfArtifacts.Add(new PdfArtifact
        {
            Id = artifactId,
            WorkspaceKey = "demo:owner",
            GeneratedCvVersionId = Guid.NewGuid(),
            ObjectKey = "owner/cv.pdf",
            Sha256 = "hash",
            Size = 3
        });
        await db.SaveChangesAsync();

        var storage = new TestArtifactStorage();
        var service = new PdfArtifactService(db, storage);

        var otherWorkspaceResult = await service.OpenReadAsync(
            "demo:other",
            artifactId,
            CancellationToken.None);
        var ownerResult = await service.OpenReadAsync(
            "demo:owner",
            artifactId,
            CancellationToken.None);

        Assert.Null(otherWorkspaceResult);
        Assert.NotNull(ownerResult);
        Assert.Equal("owner/cv.pdf", Assert.Single(storage.OpenedKeys));
        await ownerResult.DisposeAsync();
    }

    private sealed class TestArtifactStorage : IArtifactStorage
    {
        public List<string> OpenedKeys { get; } = [];

        public Task<StoredArtifact> SaveAsync(
            string workspaceKey,
            Guid artifactId,
            byte[] data,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(
            string key,
            CancellationToken cancellationToken)
        {
            OpenedKeys.Add(key);
            return Task.FromResult<Stream?>(new MemoryStream(Encoding.UTF8.GetBytes("pdf")));
        }

        public Task DeleteWorkspaceAsync(
            string workspaceKey,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
