# ADR-0004: Search and Background Jobs Boundary

## Status

Accepted for the baseline integration.

## Decision

The search module exposes `RecipeSearchOptions`, `PagedResult<T>`, `RecipeSearchResult`, and `IRecipeSearchReader` from Application. Search options validate `page >= 1`, `1 <= pageSize <= 50`, supported sort fields, and non-negative filter values.

The PostgreSQL-specific full-text search implementation is added only after the Recipe schema is available. Its migration must own the `tsvector` column, trigger, and GIN index on `Recipes`; it must not create a parallel Recipe table or migration.

Background jobs are expressed through Application contracts. Infrastructure adapts those contracts to Hangfire and stores job state in PostgreSQL when `Hangfire:Enabled=true`. The feature is disabled by default so liveness and local smoke tests do not require PostgreSQL. Welcome email and thumbnail jobs are fire-and-forget jobs; sitemap generation uses a daily UTC recurring schedule at 02:00. Concrete job registration is enabled by the owning modules once their email, file, and recipe services are available.

Each job receives an idempotency key and claims it through `IJobExecutionStore` before performing side effects. A duplicate delivery returns without repeating the side effect. The key is derived from the source event and operation, for example `welcome-email:user:{userId}`, `thumbnail:recipe-image:{imageId}`, or `sitemap:{utcDate}`. The store must only mark a key completed after the side effect succeeds; a failed job remains retryable.

## Consequences

- Application does not reference Hangfire or PostgreSQL-specific APIs.
- Auth, Recipe, File, and Search modules can enqueue jobs through `IBackgroundJobScheduler`.
- Job service implementations remain integration points until their owning modules are merged.
- Hangfire retry is not treated as deduplication; idempotency is enforced by the job execution store.
- The search migration is intentionally deferred until Recipe's table and published-state contract are stable.
