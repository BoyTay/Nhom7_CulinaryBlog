# PostgreSQL recipe-search plan evidence

`RecipeSearchPostgresTests.SearchGinIndexesAreInstalledAndPostgresProducesIndexedExplainPlans`
compares the legacy title `ILIKE` scan with the current Vietnamese prefix-FTS
query on PostgreSQL 16. The test inserts 10,000 published recipes in a
transaction, with one row matching the selective search term, runs `ANALYZE`,
and rolls the fixture data back when the test completes.

The test records `EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, TIMING OFF)` output for:

| Query | Expected access path |
| --- | --- |
| Legacy `Title ILIKE '%needleham%'` | Sequential scan |
| Prefix `SearchVector @@ to_tsquery(...)` | `IX_Recipes_SearchVector` GIN index |
| Trigram `Title % 'needlehamx'` | `IX_Recipes_Title_Trgm` GIN index |

It also reads PostgreSQL's index catalog to verify each index uses GIN and the
expected indexed expression/operator class. The captured plans include actual
execution time and buffer usage; CI prints them in the focused
`Capture PostgreSQL FTS EXPLAIN ANALYZE evidence` step.

The CI migration step starts from an empty PostgreSQL service, first migrates
through `20260930124003_AddPublishedAtToRecipe`, then applies the Search
migration. This checks the clean migration chain and the upgrade from the
pre-FTS Recipe schema.

To capture the plans locally against a migrated PostgreSQL database:

```powershell
dotnet test .\tests\CulinaryBlog.Integration.Tests\CulinaryBlog.Integration.Tests.csproj `
  --configuration Release `
  --filter "FullyQualifiedName~SearchGinIndexesAreInstalledAndPostgresProducesIndexedExplainPlans" `
  --logger "console;verbosity=detailed"
```
