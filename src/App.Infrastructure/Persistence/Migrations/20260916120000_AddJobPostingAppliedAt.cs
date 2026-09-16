using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260916120000_AddJobPostingAppliedAt")]
public partial class AddJobPostingAppliedAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "AppliedAt",
            table: "SourceItems",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "SourceItems" AS posting
            SET "AppliedAt" = COALESCE(
                (
                    SELECT MIN(history."CreatedAt")
                    FROM "StatusHistory" AS history
                    WHERE history."WorkspaceKey" = posting."WorkspaceKey"
                      AND history."SourceItemId" = posting."Id"
                      AND history."NewStatus" = 'APPLIED'
                ),
                posting."UpdatedAt"
            )
            WHERE posting."WorkflowStatus" IN ('APPLIED', 'INTERVIEWING', 'REJECTED')
              AND posting."AppliedAt" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AppliedAt", table: "SourceItems");
    }
}
