using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DemoSession> DemoSessions => Set<DemoSession>();
    public DbSet<SourceItem> SourceItems => Set<SourceItem>();
    public DbSet<StatusHistory> StatusHistory => Set<StatusHistory>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<DocumentTemplateVersion> DocumentTemplateVersions => Set<DocumentTemplateVersion>();
    public DbSet<UserRuleDocument> UserRuleDocuments => Set<UserRuleDocument>();
    public DbSet<UserRuleVersion> UserRuleVersions => Set<UserRuleVersion>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
    public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();
    public DbSet<GeneratedDocumentVersion> GeneratedDocumentVersions => Set<GeneratedDocumentVersion>();
    public DbSet<CompileJob> CompileJobs => Set<CompileJob>();
    public DbSet<PdfArtifact> PdfArtifacts => Set<PdfArtifact>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes().Where(t => typeof(WorkspaceEntity).IsAssignableFrom(t.ClrType)))
            entity.AddIndex(entity.FindProperty(nameof(WorkspaceEntity.WorkspaceKey))!);
        builder.Entity<SourceItem>().HasIndex(x => new { x.WorkspaceKey, x.SourceKey, x.SourceExternalId }).IsUnique();
        builder.Entity<DocumentTemplateVersion>().HasIndex(x => new { x.WorkspaceKey, x.TemplateId, x.Version }).IsUnique();
        builder.Entity<UserRuleVersion>().HasIndex(x => new { x.WorkspaceKey, x.RuleDocumentId, x.Version }).IsUnique();
        builder.Entity<GeneratedDocumentVersion>().HasIndex(x => new { x.WorkspaceKey, x.DocumentId, x.Version }).IsUnique();
        builder.Entity<GenerationJob>().Property(x => x.Status).HasConversion<string>();
        builder.Entity<CompileJob>().Property(x => x.Status).HasConversion<string>();
    }
}
