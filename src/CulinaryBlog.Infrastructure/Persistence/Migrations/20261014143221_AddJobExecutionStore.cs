using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddJobExecutionStore : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "JobExecutions",
            columns: table => new
            {
                IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                ClaimToken = table.Column<Guid>(type: "uuid", nullable: false),
                LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobExecutions", x => x.IdempotencyKey);
                table.CheckConstraint("CK_JobExecutions_AttemptCount", "\"AttemptCount\" > 0");
                table.CheckConstraint("CK_JobExecutions_Status", "\"Status\" IN ('Running', 'Failed', 'Completed')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_JobExecutions_CompletedAt",
            table: "JobExecutions",
            column: "CompletedAt",
            filter: "\"Status\" = 'Completed'");

        migrationBuilder.CreateIndex(
            name: "IX_JobExecutions_LastAttemptAt",
            table: "JobExecutions",
            column: "LastAttemptAt",
            filter: "\"Status\" <> 'Completed'");

        migrationBuilder.CreateIndex(
            name: "IX_JobExecutions_LockedUntil",
            table: "JobExecutions",
            column: "LockedUntil",
            filter: "\"Status\" = 'Running'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "JobExecutions");
    }
}
