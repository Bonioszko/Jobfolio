using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260906120000_UniqueCandidateRuleWorkspace")]
public partial class UniqueCandidateRuleWorkspace : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "IX_UserRuleDocuments_WorkspaceKey";
            CREATE UNIQUE INDEX "IX_UserRuleDocuments_WorkspaceKey"
                ON "UserRuleDocuments" ("WorkspaceKey");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_UserRuleDocuments_WorkspaceKey",
            table: "UserRuleDocuments");
        migrationBuilder.CreateIndex(
            name: "IX_UserRuleDocuments_WorkspaceKey",
            table: "UserRuleDocuments",
            column: "WorkspaceKey");
    }
}
