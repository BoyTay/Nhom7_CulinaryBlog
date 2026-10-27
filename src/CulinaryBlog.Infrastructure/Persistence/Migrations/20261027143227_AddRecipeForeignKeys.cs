using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecipeForeignKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_Recipes_AspNetUsers_AuthorId",
            table: "Recipes",
            column: "AuthorId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Recipes_Categories_CategoryId",
            table: "Recipes",
            column: "CategoryId",
            principalTable: "Categories",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Recipes_AspNetUsers_AuthorId",
            table: "Recipes");

        migrationBuilder.DropForeignKey(
            name: "FK_Recipes_Categories_CategoryId",
            table: "Recipes");
    }
}
