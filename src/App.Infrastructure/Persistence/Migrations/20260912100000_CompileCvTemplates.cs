using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260912100000_CompileCvTemplates")]
public partial class CompileCvTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "DocumentVersionId",
            table: "CompileJobs",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<Guid>(
            name: "TemplateVersionId",
            table: "CompileJobs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_CompileJobs_ExactlyOneSource",
            table: "CompileJobs",
            sql: "(\"DocumentVersionId\" IS NOT NULL) <> (\"TemplateVersionId\" IS NOT NULL)");

        migrationBuilder.AlterColumn<Guid>(
            name: "DocumentVersionId",
            table: "PdfArtifacts",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<Guid>(
            name: "TemplateVersionId",
            table: "PdfArtifacts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_PdfArtifacts_ExactlyOneSource",
            table: "PdfArtifacts",
            sql: "(\"DocumentVersionId\" IS NOT NULL) <> (\"TemplateVersionId\" IS NOT NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_CompileJobs_ExactlyOneSource",
            table: "CompileJobs");
        migrationBuilder.DropCheckConstraint(
            name: "CK_PdfArtifacts_ExactlyOneSource",
            table: "PdfArtifacts");

        migrationBuilder.Sql(
            "DELETE FROM \"CompileJobs\" WHERE \"DocumentVersionId\" IS NULL;");
        migrationBuilder.Sql(
            "DELETE FROM \"PdfArtifacts\" WHERE \"DocumentVersionId\" IS NULL;");

        migrationBuilder.DropColumn(name: "TemplateVersionId", table: "CompileJobs");
        migrationBuilder.DropColumn(name: "TemplateVersionId", table: "PdfArtifacts");

        migrationBuilder.AlterColumn<Guid>(
            name: "DocumentVersionId",
            table: "CompileJobs",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid?),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "DocumentVersionId",
            table: "PdfArtifacts",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid?),
            oldType: "uuid",
            oldNullable: true);
    }
}
