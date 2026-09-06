using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260906160000_UniqueCvTemplateName")]
public partial class UniqueCvTemplateName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentTemplates_WorkspaceKey_Name"
                ON "DocumentTemplates" ("WorkspaceKey", "Name");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DocumentTemplates_WorkspaceKey_Name",
            table: "DocumentTemplates");
    }
}
