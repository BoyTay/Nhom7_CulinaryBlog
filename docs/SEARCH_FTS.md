# Recipe full-text search

Recipe search stores a weighted PostgreSQL `tsvector` in `Recipes.SearchVector`.
The `simple` text-search configuration and `unaccent` normalize Vietnamese
terms; title terms receive weight A and description terms weight B. A trigger
keeps the vector current when either source column changes, and the migration
backfills existing recipes before creating the GIN index.

Queries treat each Unicode letter/number token as a prefix and require all
tokens to match. Search results remain Published-only and exclude soft-deleted
recipes. Filters, sort options, and offset pagination are applied in the
database; relevance sorting uses `ts_rank`.
An omitted `q` allows the Search page to browse published recipes, but an
explicitly supplied empty/whitespace query or a one-character term returns
422 `VALIDATION_ERROR` with an `errors.q` entry. Search result caching must
use the Redis distributed cache with the one-minute TTL required by
NFR-PERF-003. Cache keys include normalized search options and a generation
token; successful Recipe/Category writes rotate the token so old result keys
cannot be reused. Cache backend failures are logged and Search falls back to
PostgreSQL; any stale results after an invalidation failure expire within one
minute. Redis is configured through `ConnectionStrings:Redis`; local
configurations without it use process-local distributed memory cache and do
not satisfy the production distributed-cache requirement. The API emits
`culinaryblog.search.cache.requests` with a `result` tag (`hit`, `miss`, or
`bypass`) to measure cache effectiveness.

## Verification

Run
`dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --filter FullyQualifiedName~RecipeSearch`
against PostgreSQL. The fixture applies all EF migrations into a temporary
schema. Tests cover accented/unaccented Vietnamese prefixes, visibility,
title/description ranking, trigger updates, filters and pagination. The
GIN-plan test inserts 10,000 unrelated published recipes plus a selective
match, runs `ANALYZE`, executes `EXPLAIN (ANALYZE, BUFFERS)`, and asserts the
plan includes `IX_Recipes_SearchVector`.

The integration test writes the complete `EXPLAIN ANALYZE` plan to its test
output. That captured plan is the evidence to retain with the #21 issue/CI run;
do not disable sequential scans to force index selection.

### Local plan evidence

Captured from PostgreSQL 16.15 with 10,000 unrelated rows and one selective
match. The test drops/recreates only the temporary schema's GIN index to
compare plans without changing planner settings:

| State | Plan | Actual time | Shared buffers |
|---|---|---:|---:|
| Without GIN | Sequential scan | 12.219 ms | 176 hits |
| With GIN | Bitmap index scan + bitmap heap scan | 0.077 ms | 4 hits |

The indexed plan used `IX_Recipes_SearchVector` and returned the same single
row. Timings are a local test-container observation, not a production
performance guarantee.
