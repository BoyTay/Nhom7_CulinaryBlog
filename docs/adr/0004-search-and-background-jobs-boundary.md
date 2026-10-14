# ADR-0004: Search and Background Jobs Boundary

## Status

Accepted for the baseline integration.

## Decision

The search module exposes `RecipeSearchOptions`, `PagedResult<T>`, `RecipeSearchResult`, and `IRecipeSearchReader` from Application. Search options validate `page >= 1`, `1 <= pageSize <= 50`, supported sort fields, and non-negative filter values.

The PostgreSQL-specific full-text search implementation is added only after the Recipe schema is available. Its migration owns the `tsvector` column, trigger, backfill, and GIN index on `Recipes`; it must not create a parallel Recipe table or update the shared model snapshot before the integration branch.

Recipe search uses PostgreSQL's `simple` text-search configuration with `unaccent` applied to both indexed text and query terms. Each query token is a prefix term and tokens are ANDed. The vector weights titles above descriptions; `ts_rank` supplies relevance ordering. Search continues to filter out soft-deleted recipes and only returns `Published` recipes. The `pg_trgm` extension is installed with the existing PostgreSQL search extensions for future similarity search; the FTS GIN index is over `tsvector`.

Background jobs are expressed through Application contracts. Infrastructure adapts those contracts to Hangfire and stores job state in PostgreSQL when `Hangfire:Enabled=true`. The feature is disabled by default so liveness and local smoke tests do not require PostgreSQL. Workers listen on the `critical`, `default`, and `maintenance` queues. Hangfire retries failed jobs up to five times with increasing delays; Application job execution cooperatively cancels after 15 minutes, while the PostgreSQL claim lease expires after 20 minutes and is renewed every 30 seconds. Worker count, queues, retry delays, timeout, lease, renewal, retention and graceful shutdown timeout are configurable through `Hangfire`. Invalid settings fail at startup. Completed, failed, and expired claims are pruned after 90 days. The Hangfire dashboard is available only when enabled and requires the Admin role.

Welcome email and thumbnail jobs are fire-and-forget jobs; sitemap generation uses the `0 2 * * *` daily UTC schedule and an execution key derived from the Hangfire job creation date and job ID. The key survives retries across UTC midnight. The Application date-based entrypoint remains available for explicitly daily deduplicated callers. Concrete side-effect implementations remain owned by the Auth, File, and Recipe modules. The recurring sitemap is registered only when an `ISitemapWriter` implementation is present; otherwise startup logs a warning and removes any stale sitemap schedule. No fake side effects are registered in production; W05 tests inject services and W06 supplies actual implementations.

When `Hangfire:Enabled=false`, Infrastructure registers a no-op `IBackgroundJobScheduler`. Producers can resolve and call the scheduler without conditional wiring; enqueue and recurring-registration calls are intentionally ignored until Hangfire is enabled.

Each job receives an idempotency key and claims it through `IJobExecutionStore` before performing side effects. A completed duplicate returns without repeating the side effect. A live competing claim throws `JobLeaseUnavailableException` so Hangfire retries it rather than acknowledging unperformed work. Each scoped store owns a fencing token; only that token can complete or release the claim, and completion/renewal require an unexpired lease. The optional `IJobLeaseStore` extension renews ownership while the operation is running, including when it ignores timeout cancellation. Renewal failure cancels the operation; completion is refused. Side-effect services must observe cancellation and implement their own idempotency: a crash between external success and recording completion can cause redelivery. The key is derived from the source event and operation, for example `welcome-email:user:{userId}`, `thumbnail:recipe-image:{imageId}`, or `sitemap:{utcDate}`. The store must only mark a key completed after the side effect succeeds; a failed job remains retryable.

## Consequences

- Application does not reference Hangfire or PostgreSQL-specific APIs.
- Auth, Recipe, File, and Search modules can enqueue jobs through `IBackgroundJobScheduler`.
- Job service implementations remain integration points until their owning modules are merged.
- Hangfire retry is not treated as deduplication; idempotency is enforced by the job execution store, which releases failed claims for retries and permits recovery after an expired lease.
- The search migration is intentionally deferred until Recipe's table and published-state contract are stable.
