using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006144300_AddRecipeFullTextSearch")]
public sealed class AddRecipeFullTextSearch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE EXTENSION IF NOT EXISTS unaccent;
            CREATE EXTENSION IF NOT EXISTS pg_trgm;

            CREATE TEXT SEARCH CONFIGURATION culinary_vietnamese (COPY = simple);
            ALTER TEXT SEARCH CONFIGURATION culinary_vietnamese
                ALTER MAPPING FOR hword, hword_part, word
                WITH unaccent, simple;

            ALTER TABLE "Recipes"
                ADD COLUMN "SearchVector" tsvector NOT NULL DEFAULT ''::tsvector;

            CREATE FUNCTION update_recipe_search_vector()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                NEW."SearchVector" :=
                    setweight(
                        to_tsvector('culinary_vietnamese', coalesce(NEW."Title", '')),
                        'A')
                    ||
                    setweight(
                        to_tsvector('culinary_vietnamese', coalesce(NEW."Description", '')),
                        'B');
                RETURN NEW;
            END
            $function$;

            CREATE TRIGGER recipe_search_vector_update
                BEFORE INSERT OR UPDATE OF "Title", "Description"
                ON "Recipes"
                FOR EACH ROW
                EXECUTE FUNCTION update_recipe_search_vector();

            UPDATE "Recipes"
            SET "SearchVector" =
                setweight(
                    to_tsvector('culinary_vietnamese', coalesce("Title", '')),
                    'A')
                ||
                setweight(
                    to_tsvector('culinary_vietnamese', coalesce("Description", '')),
                    'B');

            CREATE INDEX "IX_Recipes_SearchVector"
                ON "Recipes" USING GIN ("SearchVector")
                WHERE "Status" = 'Published' AND "IsDeleted" = FALSE;

            CREATE INDEX "IX_Recipes_Title_Trgm"
                ON "Recipes" USING GIN ("Title" gin_trgm_ops)
                WHERE "Status" = 'Published' AND "IsDeleted" = FALSE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX "IX_Recipes_Title_Trgm";
            DROP INDEX "IX_Recipes_SearchVector";
            DROP TRIGGER recipe_search_vector_update ON "Recipes";
            DROP FUNCTION update_recipe_search_vector();
            ALTER TABLE "Recipes" DROP COLUMN "SearchVector";
            DROP TEXT SEARCH CONFIGURATION culinary_vietnamese;
            """);
    }
}
