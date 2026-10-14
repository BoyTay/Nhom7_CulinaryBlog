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
Deletion and metadata reject URLs outside the configured endpoint/bucket.
Credentials, endpoints and bucket come from configuration. Storage ETags are
opaque object metadata and must not be used as recipe concurrency tokens.

## Thumbnail producer contract

The existing `RecipeImageThumbnailRequested` payload contains:

| Field | Meaning |
| --- | --- |
| RecipeId | Recipe aggregate ID |
| RecipeImageId | Persisted image ID |
| OriginalUrl | Public URL of the original image |
| IdempotencyKey | `thumbnail:recipe-image:{RecipeImageId}` using normal GUID formatting |

The producer publishes only after the image and aggregate commit. The adapter
calls the existing `ThumbnailJob.ExecuteAsync(idempotencyKey, recipeImageId,
originalUrl, ct)` through `IBackgroundJobScheduler`; it does not introduce a
second job/event API. `MediumUrl` and `ThumbnailUrl` remain null until processing.
The existing `Url` response field remains an alias of `OriginalUrl`.

Kiên's consumer should load the existing Recipe aggregate, check that the image
still exists, and use `UpdateImageVariants`. Image persistence through
`IRecipeRepository.SaveImageChangesAsync` locks/checks the recipe's original
version before saving children in one transaction, and advances aggregate xmin.
Set the original version through `SetOriginalVersion` when a caller provides it.
Do not update variants directly without participating in aggregate concurrency.
If deletion wins, discard/clean any generated variants rather than resurrecting
the image. A conflicting version requires reloading before retry.

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
- Retry count, idempotency/lease implementation and thumbnail processing belong
  to Kien. No changes were made to those shared contracts.
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
