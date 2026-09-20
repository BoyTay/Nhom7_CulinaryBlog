using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialArchitecture : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Extensions may be shared by other schemas, so rollback intentionally keeps them installed.
    }
}
