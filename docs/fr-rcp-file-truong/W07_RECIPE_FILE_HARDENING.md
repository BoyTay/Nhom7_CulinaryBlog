# Week 07 Recipe/File hardening (#36)

Owner: Trường

Branch: `feature/fr-rcp-file-truong`

Milestone: Week 07

## Completed scope

- Recipe lifecycle unit and endpoint coverage includes Draft, Published and
  Archived behavior, publish prerequisites, idempotent transitions and soft
  deletion with nested rows preserved.
- Nested Step and Ingredient CRUD verifies ownership, ETag enforcement,
  missing/foreign child handling and continuous ordering.
- File lifecycle coverage verifies validation, primary-image invariants,
  variant cleanup, MinIO failure, stale ETags and upload compensation after a
  database concurrency failure.
- Real concurrent HTTP requests now update the same Recipe with one ETag. One
  writer succeeds and every stale writer receives HTTP 422 with
  `RECIPE_CONCURRENCY_CONFLICT`. Concurrent image uploads exercise the same
  rule and delete every losing upload.
- Recipe list reads project directly to `RecipeListItemDto` with
  `AsNoTracking`. Recipe detail reads project directly to `RecipeDetailDto`
  with `AsNoTracking` and `AsSplitQuery` for Steps, Ingredients and Images.
  Command paths retain tracked aggregates for `xmin` concurrency checks.

No endpoint, shared Problem Details code, policy, migration or
`ApplicationDbContextModelSnapshot` was changed. Existing Recipe/File endpoints
retain their Scalar/OpenAPI summaries, response declarations and authorization.

## Failure-boundary evidence

| Failure | Expected and tested result |
| --- | --- |
| MinIO upload fails before DB write | HTTP 503; no RecipeImage row is created. |
| DB concurrency check fails after upload | HTTP 422; the uploaded original is deleted with a cancellation-independent compensation token. |
| Cleanup job enqueue fails after image deletion | Direct deletion is attempted for original, medium and thumbnail objects. |
| Direct cleanup also fails | All variants are attempted; DB deletion stays committed and the API returns HTTP 503 for reconciliation. |
| Thumbnail enqueue fails after upload commit | The original and RecipeImage remain usable; the request fails and reconciliation is required. |

## Remaining risks and handoff

- DB commit and thumbnail enqueue are still not atomic. A durable outbox or
  reconciliation worker crosses the Recipe/File producer and Jobs ownership
  boundary and must be agreed with Kiên/Hiếu before implementation.
- A process crash after an image deletion commit but before cleanup scheduling
  can still leave orphan objects. The same reconciliation mechanism should
  cover this gap.
- This issue introduces no migration, so clean/upgrade database migration
  checks are not applicable. Week 08 integration must continue to regenerate
  any combined snapshot only on `integration/week-08`.

## Local verification

Run from the repository root:

```powershell
dotnet build CulinaryBlog.sln --configuration Release --no-restore --warnaserror
dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Recipe"
dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~RecipeEndpointTests|FullyQualifiedName~RecipeLifecycleEndpointTests|FullyQualifiedName~RecipeNestedResourcesEndpointTests|FullyQualifiedName~RecipeImageEndpointTests|FullyQualifiedName~RecipeReadRepositoryTests"
```

Results on 2026-10-21:

- Release build: 0 warnings, 0 errors.
- Focused Recipe application tests: 70 passed, 0 failed, 0 skipped.
- Focused Recipe integration tests: 37 passed, 0 failed, 0 skipped.
- Full application tests: 152 passed, 0 failed, 0 skipped.
- Architecture tests: 3 passed, 0 failed, 0 skipped.
- Full integration tests without opt-in services: 179 passed, 0 failed,
  2 skipped.
- Real MinIO adapter tests with `MINIO_INTEGRATION_TESTS=true` and the Compose
  MinIO container healthy: 2 passed, 0 failed, 0 skipped.

Completion commit, pull request and successful remote CI link must be added
before the issue is closed. Integration is a `--no-ff` merge through
`integration/week-08`, never a direct merge to `main`.
