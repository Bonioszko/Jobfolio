using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DemoSession> DemoSessions => Set<DemoSession>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistory => Set<ApplicationStatusHistory>();
    public DbSet<CvTemplate> CvTemplates => Set<CvTemplate>();
    public DbSet<CvTemplateVersion> CvTemplateVersions => Set<CvTemplateVersion>();
    public DbSet<CandidateRuleDocument> CandidateRuleDocuments => Set<CandidateRuleDocument>();
    public DbSet<CandidateRuleVersion> CandidateRuleVersions => Set<CandidateRuleVersion>();
    public DbSet<CvGenerationJob> CvGenerationJobs => Set<CvGenerationJob>();
    public DbSet<GeneratedCv> GeneratedCvs => Set<GeneratedCv>();
    public DbSet<GeneratedCvVersion> GeneratedCvVersions => Set<GeneratedCvVersion>();
    public DbSet<CvCompileJob> CvCompileJobs => Set<CvCompileJob>();
    public DbSet<PdfArtifact> PdfArtifacts => Set<PdfArtifact>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ConfigureLegacySchemaNames(builder);

        foreach (var entity in builder.Model.GetEntityTypes()
                     .Where(type => typeof(WorkspaceOwnedEntity).IsAssignableFrom(type.ClrType)))
        {
            entity.AddIndex(entity.FindProperty(nameof(WorkspaceOwnedEntity.WorkspaceKey))!);
        }

        builder.Entity<JobPosting>()
            .HasIndex(posting => new
            {
                posting.WorkspaceKey,
                posting.ProviderKey,
                posting.ProviderExternalId
            })
            .IsUnique();
        builder.Entity<CvTemplateVersion>()
            .HasIndex(version => new
            {
                version.WorkspaceKey,
                version.CvTemplateId,
                version.Version
            })
            .IsUnique();
        builder.Entity<CandidateRuleVersion>()
            .HasIndex(version => new
            {
                version.WorkspaceKey,
                version.CandidateRuleDocumentId,
                version.Version
            })
            .IsUnique();
        builder.Entity<GeneratedCvVersion>()
            .HasIndex(version => new
            {
                version.WorkspaceKey,
                version.GeneratedCvId,
                version.Version
            })
            .IsUnique();
        builder.Entity<CvGenerationJob>().Property(job => job.Status).HasConversion<string>();
        builder.Entity<CvCompileJob>().Property(job => job.Status).HasConversion<string>();
    }

    private static void ConfigureLegacySchemaNames(ModelBuilder builder)
    {
        builder.Entity<DemoSession>().ToTable("DemoSessions");

        builder.Entity<JobPosting>(entity =>
        {
            entity.ToTable("SourceItems");
            entity.Property(posting => posting.ProviderKey).HasColumnName("SourceKey");
            entity.Property(posting => posting.ProviderExternalId).HasColumnName("SourceExternalId");
            entity.Property(posting => posting.Title).HasColumnName("DisplayTitle");
            entity.Property(posting => posting.NormalizedDataJson).HasColumnName("ParsedDataJson");
            entity.Property(posting => posting.ApplicationStatus).HasColumnName("WorkflowStatus");
        });
        builder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.ToTable("StatusHistory");
            entity.Property(history => history.JobPostingId).HasColumnName("SourceItemId");
        });
        builder.Entity<CvTemplate>().ToTable("DocumentTemplates");
        builder.Entity<CvTemplateVersion>(entity =>
        {
            entity.ToTable("DocumentTemplateVersions");
            entity.Property(version => version.CvTemplateId).HasColumnName("TemplateId");
        });
        builder.Entity<CandidateRuleDocument>().ToTable("UserRuleDocuments");
        builder.Entity<CandidateRuleVersion>(entity =>
        {
            entity.ToTable("UserRuleVersions");
            entity.Property(version => version.CandidateRuleDocumentId).HasColumnName("RuleDocumentId");
        });
        builder.Entity<CvGenerationJob>(entity =>
        {
            entity.ToTable("GenerationJobs");
            entity.Property(job => job.JobPostingId).HasColumnName("SourceItemId");
            entity.Property(job => job.CvTemplateVersionId).HasColumnName("TemplateVersionId");
            entity.Property(job => job.CandidateRuleVersionId).HasColumnName("RuleVersionId");
            entity.Property(job => job.JobPostingSnapshotJson).HasColumnName("SourceSnapshotJson");
            entity.Property(job => job.GeneratedCvId).HasColumnName("GeneratedDocumentId");
        });
        builder.Entity<GeneratedCv>(entity =>
        {
            entity.ToTable("GeneratedDocuments");
            entity.Property(cv => cv.JobPostingId).HasColumnName("SourceItemId");
        });
        builder.Entity<GeneratedCvVersion>(entity =>
        {
            entity.ToTable("GeneratedDocumentVersions");
            entity.Property(version => version.GeneratedCvId).HasColumnName("DocumentId");
        });
        builder.Entity<CvCompileJob>(entity =>
        {
            entity.ToTable("CompileJobs");
            entity.Property(job => job.GeneratedCvVersionId).HasColumnName("DocumentVersionId");
        });
        builder.Entity<PdfArtifact>(entity =>
        {
            entity.ToTable("PdfArtifacts");
            entity.Property(artifact => artifact.GeneratedCvVersionId).HasColumnName("DocumentVersionId");
        });
    }
}
