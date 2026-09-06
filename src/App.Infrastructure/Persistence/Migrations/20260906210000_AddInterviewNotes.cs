using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260906210000_AddInterviewNotes")]
public partial class AddInterviewNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "InterviewNotes" (
                "Id" uuid NOT NULL,
                "JobPostingId" uuid NOT NULL,
                "Stage" character varying(100) NOT NULL,
                "InterviewDate" date NOT NULL,
                "Notes" character varying(10000) NOT NULL,
                "WorkspaceKey" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_InterviewNotes" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_InterviewNotes_WorkspaceKey"
                ON "InterviewNotes" ("WorkspaceKey");

            CREATE INDEX IF NOT EXISTS "IX_InterviewNotes_WorkspaceKey_JobPostingId_InterviewDate"
                ON "InterviewNotes" ("WorkspaceKey", "JobPostingId", "InterviewDate");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InterviewNotes");
    }
}
