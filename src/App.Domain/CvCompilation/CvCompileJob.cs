namespace App.Domain;

public sealed class CvCompileJob : LeasedJob
{
    public Guid GeneratedCvVersionId { get; set; }
    public Guid? PdfArtifactId { get; set; }
}

public sealed class PdfArtifact : WorkspaceOwnedEntity
{
    public Guid GeneratedCvVersionId { get; set; }
    public required string ObjectKey { get; set; }
    public required string Sha256 { get; set; }
    public long Size { get; set; }
}
