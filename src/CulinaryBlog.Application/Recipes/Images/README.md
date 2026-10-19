# Recipe image and file hand-off (Week 05)

Owner: Truong (Recipe/File). Background processing and retry policy: Kien.

## HTTP contract

- `POST /api/v1/recipes/{recipeId}/images`: multipart `file`, optional `altText`
  and `isPrimary`; returns 201 and a new recipe ETag.
- `PATCH /api/v1/recipes/{recipeId}/images/{imageId}/primary`: returns 200 and
  a new ETag.
- `DELETE /api/v1/recipes/{recipeId}/images/{imageId}`: returns 204; fetch the
  recipe again before another mutation.
- All mutations require Bearer authentication, Author/Admin, owner/Admin
  resource authorization and a quoted numeric `If-Match` recipe ETag.
- Missing ETag: 428; malformed ETag: 400; stale ETag: 422
  `RECIPE_CONCURRENCY_CONFLICT`; missing recipe/image: 404; forbidden: 403.
- File limit: 5 * 1024 * 1024 bytes. JPEG, PNG, WebP and AVIF MIME types must
  match their signatures. File validation returns 400; MIME rejection uses the
  SRS code `FILE_MIME_INVALID`. Oversized HTTP bodies can be rejected with 413
  before application validation. Storage unavailability returns 503.

## Storage contract

`IFileStorageService` uses streams, not HTTP types. Upload returns a public URL;
deletion is idempotent. `GetInfoAsync(publicUrl, ct)` returns `StoredFileInfo`
(object name, MIME type, byte length, ETag), or null when the object is absent.
`OpenReadAsync(publicUrl, ct)` returns a readable stream, or null when the object
is absent; disposing the stream also releases the storage response. Deletion,
metadata reads and content reads reject URLs outside the configured
endpoint/bucket. Credentials, endpoints and bucket come from configuration.
Storage ETags are opaque object metadata and must not be used as recipe
concurrency tokens.

## Thumbnail producer contract

The existing `RecipeImageThumbnailRequested` payload contains:

| Field | Meaning |
| --- | --- |
| RecipeId | Recipe aggregate ID |
| RecipeImageId | Persisted image ID |
| OriginalUrl | Public URL of the original image |
| IdempotencyKey | `thumbnail:recipe-image:{RecipeImageId}` using normal GUID formatting |

The producer publishes only after the image and aggregate commit. The adapter
calls `ThumbnailJob.ExecuteAsync(idempotencyKey, recipeId, recipeImageId,
originalUrl, ct)` through `IBackgroundJobScheduler`; it does not introduce a
second job/event API. `MediumUrl` and `ThumbnailUrl` remain null until processing.
The existing `Url` response field remains an alias of `OriginalUrl`.

The consumer loads the Recipe aggregate by the event's RecipeId, checks that the
image still exists and still references the event's OriginalUrl, reads the
original through `OpenReadAsync`, and creates WebP variants within the 800x600
and 300x300 bounds. Image persistence uses `UpdateImageVariants` and
`IRecipeRepository.SaveImageChangesAsync`, which locks/checks the recipe's
original version before saving children in one transaction and advances
aggregate xmin. On deletion or replacement it discards generated variants;
failed database writes trigger bounded cleanup before Hangfire retries. The
processor rejects source files over 5 MiB and decoded images over 40 million
pixels. Do not update variants directly without participating in aggregate
concurrency.

## Week 06 job consumers

- `UserRegisteredEvent` is consumed by a MediatR handler that enqueues
  `WelcomeEmailJob` using `welcome-email:user:{UserId}` as its idempotency key.
  SMTP defaults to required STARTTLS; the local Compose stack explicitly uses
  MailHog without TLS. Configure `Email__SmtpHost`, `Email__SmtpPort`,
  `Email__SmtpSecurity`, and `PublicSite__BaseUrl`; SMTP credentials are
  optional, but if a username is provided a secure connection and password are
  required.
- `SitemapWriter` creates sitemap-standard XML for the public root, recipes,
  search, published recipe details, and category-filtered recipe listings, then
  uploads `sitemap.xml` to the configured MinIO bucket. Recipe detail URLs use
  the existing `/recipes/{id}` frontend route; category entries use
  `/recipes?categoryId={id}`. Set `PublicSite__BaseUrl` to the externally
  reachable canonical origin (Compose defaults to `http://localhost`).
- Welcome-email and thumbnail jobs retry three times at 1, 5, and 30 minutes;
  sitemap retries twice at 1 and 5 minutes. Other Hangfire jobs keep the
  configured general retry delays.

## Failure boundaries requiring integration work

- DB commit and job enqueue are separate. A process crash/enqueue failure can
  leave an original without a thumbnail job. A durable outbox/reconciliation
  design needs agreement with Kien/Hieu; it is not implemented here.
- When Hangfire is disabled, the existing no-op scheduler intentionally drops
  thumbnail work. Original images remain available.
- Delete schedules each distinct original/medium/thumbnail URL after commit.
  If scheduling fails or is disabled, direct cleanup is attempted independently
  of request cancellation. If direct cleanup fails, the API returns 503 after
  the DB deletion; all variant deletions are attempted, and orphan reconciliation
  remains necessary. A process crash between commit and cleanup has the same gap.
- Failed upload persistence triggers bounded compensation even if the request
  was cancelled. Failed compensation is logged for reconciliation.
- Idempotency/lease implementation belongs to Kien's job infrastructure. No
  producer-side event behavior or Recipe model changes are part of this work.
- The pre-existing migration/snapshot changes require Hieu's integration review;
  contributors must not hand-merge or regenerate the shared snapshot.

## Local verification (2026-10-14)

- Release solution build with `--no-restore --warnaserror`: 0 warnings/errors.
- Recipe-related Application tests: 68 passed, 0 skipped.
- Recipe-related Integration tests plus MinIO adapter tests: 52 passed, 0 skipped
  (`MINIO_INTEGRATION_TESTS=true`, real local PostgreSQL/MinIO).
- Architecture tests: 3 passed.
- The existing migration chain applied to the isolated database
  `culinary_w05_review_clean`.
- Upgrade verification used `culinary_w05_review_upgrade`, migrated first to
  `20261007190000_AddRecipeFullTextSearch`, seeded with a recipe and three legacy
  image rows, then upgraded to `20261013150052_AddRecipeImageVariants`.
  Original URLs/primary were preserved; order became 0, 1, 2; variant URLs stayed
  null. This reconstructs the previous schema; it is not a production DB backup.
- The shared snapshot SHA256 stayed
  `BB8641A8AE6008E12657627E1CC9F002759DD0B825551212DB8B07BD9CC39E4A`.
- EF CLI reports its existing version 10.0.4 is older than runtime 10.0.12;
  migrations succeeded. Tool-version alignment belongs to the platform owner.
- The initial feature delivery excluded the shared snapshot, which caused CI's
  `PendingModelChangesWarning`. The follow-up CI fix includes the existing
  snapshot after verifying its model matches the migration designer exactly;
  no manual snapshot edits or warning suppression were used. The user authorized
  this feature-branch fix after reporting the CI failure. Compose wiring and
  development settings remain excluded. Local verification is not a remote CI
  certification.
- Final integration remains Hieu's `--no-ff` workflow through
  `integration/week-05`. Review the shared/configuration changes separately and
  exclude development settings/secrets. PR and remote CI review remain pending.
