using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261013220000_AddJobExecutionStore")]
public sealed class AddJobExecutionStore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "JobExecutions",
            columns: table => new
            {
                IdempotencyKey = table.Column<string>(
                    type: "character varying(256)",
                    maxLength: 256,
                    nullable: false),
                Status = table.Column<string>(
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                LockedUntil = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                LastAttemptAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobExecutions", row => row.IdempotencyKey);
                table.CheckConstraint(
                    "CK_JobExecutions_Status",
                    "\"Status\" IN ('Running', 'Failed', 'Completed')");
                table.CheckConstraint(
                    "CK_JobExecutions_AttemptCount",
                    "\"AttemptCount\" > 0");
            });

        migrationBuilder.Sql(
            """
            CREATE INDEX "IX_JobExecutions_CompletedAt"
                ON "JobExecutions" ("CompletedAt")
                WHERE "Status" = 'Completed';

            CREATE INDEX "IX_JobExecutions_LastAttemptAt"
                ON "JobExecutions" ("LastAttemptAt")
                WHERE "Status" <> 'Completed';

            CREATE INDEX "IX_JobExecutions_LockedUntil"
                ON "JobExecutions" ("LockedUntil")
                WHERE "Status" = 'Running';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "JobExecutions");
}
