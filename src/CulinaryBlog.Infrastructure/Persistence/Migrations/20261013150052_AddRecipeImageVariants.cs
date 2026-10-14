using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecipeImageVariants : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Url",
            table: "RecipeImages",
            newName: "OriginalUrl");

        migrationBuilder.AddColumn<string>(
            name: "MediumUrl",
            table: "RecipeImages",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "OrderIndex",
            table: "RecipeImages",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "ThumbnailUrl",
            table: "RecipeImages",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.Sql(
            """
                WITH ranked_images AS (
                    SELECT "Id",
                           CAST(ROW_NUMBER() OVER (
                               PARTITION BY "RecipeId"
                               ORDER BY CASE WHEN "IsPrimary" THEN 0 ELSE 1 END, "Id") - 1 AS integer) AS "OrderIndex"
                    FROM "RecipeImages"
                )
                UPDATE "RecipeImages" AS image
                SET "OrderIndex" = ranked_images."OrderIndex"
                FROM ranked_images
                WHERE image."Id" = ranked_images."Id";
                """);

        migrationBuilder.CreateIndex(
            name: "IX_RecipeImages_RecipeId_OrderIndex",
            table: "RecipeImages",
            columns: ["RecipeId", "OrderIndex"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RecipeImages_RecipeId_OrderIndex",
            table: "RecipeImages");

        migrationBuilder.DropColumn(
            name: "MediumUrl",
            table: "RecipeImages");

        migrationBuilder.DropColumn(
            name: "OrderIndex",
            table: "RecipeImages");

        migrationBuilder.DropColumn(
            name: "ThumbnailUrl",
            table: "RecipeImages");

        migrationBuilder.RenameColumn(
            name: "OriginalUrl",
            table: "RecipeImages",
            newName: "Url");
    }
}
