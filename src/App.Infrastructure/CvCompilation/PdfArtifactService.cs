using App.Application;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class PdfArtifactService(
    AppDbContext db,
    IArtifactStorage storage) : IPdfArtifactService
{
    public async Task<Stream?> OpenReadAsync(
        string workspaceKey,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        var objectKey = await db.PdfArtifacts
            .AsNoTracking()
            .Where(artifact =>
                artifact.WorkspaceKey == workspaceKey &&
                artifact.Id == artifactId)
            .Select(artifact => artifact.ObjectKey)
            .SingleOrDefaultAsync(cancellationToken);

        return objectKey is null
            ? null
            : await storage.OpenReadAsync(objectKey, cancellationToken);
    }
}
