using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261007190000_AddRecipeFullTextSearch")]
public sealed class AddRecipeFullTextSearch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
            CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;

            ALTER TABLE "Recipes"
                ADD COLUMN "SearchVector" tsvector;

            CREATE FUNCTION update_recipe_search_vector()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                NEW."SearchVector" :=
                    setweight(
                        to_tsvector(
                            'simple',
                            public.unaccent('public.unaccent', coalesce(NEW."Title", ''))),
                        'A')
                    ||
                    setweight(
                        to_tsvector(
                            'simple',
                            public.unaccent('public.unaccent', coalesce(NEW."Description", ''))),
                        'B');
                RETURN NEW;
            END
            $function$;

            UPDATE "Recipes"
            SET "SearchVector" =
                setweight(
                    to_tsvector(
                        'simple',
                        public.unaccent('public.unaccent', coalesce("Title", ''))),
                    'A')
                ||
                setweight(
                    to_tsvector(
                        'simple',
                        public.unaccent('public.unaccent', coalesce("Description", ''))),
                    'B');

            CREATE TRIGGER "TR_Recipes_UpdateSearchVector"
                BEFORE INSERT OR UPDATE OF "Title", "Description"
                ON "Recipes"
                FOR EACH ROW
                EXECUTE FUNCTION update_recipe_search_vector();

            CREATE INDEX "IX_Recipes_SearchVector"
                ON "Recipes"
                USING GIN ("SearchVector");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS "TR_Recipes_UpdateSearchVector" ON "Recipes";
            DROP FUNCTION IF EXISTS update_recipe_search_vector();
            DROP INDEX IF EXISTS "IX_Recipes_SearchVector";
            ALTER TABLE "Recipes" DROP COLUMN IF EXISTS "SearchVector";
            """);
    }
}
