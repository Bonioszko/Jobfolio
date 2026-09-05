using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations;

/// <summary>
/// Transitional baseline for databases created before this repository adopted EF migrations.
/// The SQL is idempotent so it can be applied to both an existing EnsureCreated database and
/// a fresh database whose complete schema was just created by the initializer.
/// </summary>
public partial class BaselineGmailSync : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "EmailMessages" (
                "Id" uuid NOT NULL,
                "GmailMessageId" text NOT NULL,
                "ProcessingStatus" text NOT NULL,
                "ParserKey" text NULL,
                "ParserVersion" integer NULL,
                "SourceReceivedAt" timestamp with time zone NOT NULL,
                "WorkspaceKey" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_EmailMessages" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_EmailMessages_WorkspaceKey"
                ON "EmailMessages" ("WorkspaceKey");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_EmailMessages_WorkspaceKey_GmailMessageId"
                ON "EmailMessages" ("WorkspaceKey", "GmailMessageId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmailMessages");
    }
}
