using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace App.Tests;

public sealed class PersistenceMappingTests
{
    [Fact]
    public void Domain_renames_preserve_existing_database_names()
    {
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        AssertMapping<JobPosting>(db, "SourceItems", nameof(JobPosting.Title), "DisplayTitle");
        AssertMapping<GmailMessageReceipt>(db, "EmailMessages");
        AssertMapping<CvTemplate>(db, "DocumentTemplates");
        AssertMapping<CvTemplateVersion>(
            db,
            "DocumentTemplateVersions",
            nameof(CvTemplateVersion.CvTemplateId),
            "TemplateId");
        AssertMapping<CandidateRuleDocument>(db, "UserRuleDocuments");
        AssertMapping<CandidateRuleVersion>(
            db,
            "UserRuleVersions",
            nameof(CandidateRuleVersion.CandidateRuleDocumentId),
            "RuleDocumentId");
        AssertMapping<CvGenerationJob>(
            db,
            "GenerationJobs",
            nameof(CvGenerationJob.JobPostingId),
            "SourceItemId");
        AssertMapping<GeneratedCv>(db, "GeneratedDocuments");
        AssertMapping<GeneratedCvVersion>(
            db,
            "GeneratedDocumentVersions",
            nameof(GeneratedCvVersion.GeneratedCvId),
            "DocumentId");
        AssertMapping<CvCompileJob>(
            db,
            "CompileJobs",
            nameof(CvCompileJob.GeneratedCvVersionId),
            "DocumentVersionId");
    }

    private static void AssertMapping<TEntity>(
        AppDbContext db,
        string tableName,
        string? propertyName = null,
        string? columnName = null)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        Assert.Equal(tableName, entity.GetTableName());

        if (propertyName is null) return;

        var table = StoreObjectIdentifier.Table(tableName, entity.GetSchema());
        Assert.Equal(columnName, entity.FindProperty(propertyName)?.GetColumnName(table));
    }
}
