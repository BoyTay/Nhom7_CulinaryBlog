# Culinary Blog — Work Ownership Tree

> Updated from `WEEKLY_TEAM_PLAN.md` (21/09–29/11/2026).  One item has one
> owner.  Cross-module changes are made only through the named contract or an
> `integration/week-XX` branch; do not edit another owner's area directly.

```text
CulinaryBlog
├── Hieu — platform, Category, observability and shared frontend foundation
│   ├── docs/{SRS,ADR,API-contracts,integration-checklists,runbooks}
│   ├── backend/Category/{Domain,Application,Infrastructure,Api}
│   ├── backend/Observability/{HealthChecks,Telemetry,Metrics,Behaviors}
│   └── frontend/shared/{api,providers,query-keys,components}
├── Bo — Identity, authentication, authorization and profile
│   ├── backend/Auth/{Domain,Application,Infrastructure,Api}
│   ├── backend/Identity/{ApplicationUser,RefreshToken,RoleSeeder}
│   ├── backend/Security/{Jwt,GoogleValidation,RateLimits,Policies}
│   └── frontend/(auth)/{login,register,profile,session,protected-routes}
├── Truong — Recipe, nested recipe data and file storage
│   ├── backend/Recipe/{Domain,Application,Infrastructure,Api}
│   ├── backend/FileStorage/{Minio,UploadValidation,ImageLifecycle}
│   ├── migrations/Recipe-and-File (only while on feature branch)
│   └── frontend/recipes/{list,detail,editor,steps,ingredients,upload}
└── Kien — Search, FTS and background processing
    ├── backend/Search/{Application,Infrastructure,Api,Fts}
    ├── backend/Jobs/{Application,Infrastructure,Hangfire,ExecutionStore}
    ├── migrations/Search-FTS (only after Recipe schema is locked)
    └── frontend/search/{route,filters,results,pagination}
```

## Locked shared boundaries

- **Hiếu owns shared contracts:** `CategoryId`, `AuthorId`, roles/policies,
  `PagedResult<T>`, Problem Details codes, frontend API client/providers/query
  keys.  Any modification starts with an ADR/SRS/API-contract issue owned by
  Hiếu and must be merged before consumers change code.
- **Bo owns identity contracts:** claims, `ICurrentUser`, JWT/refresh cookie,
  Google-ID validation and authorization policy names.  Recipe uses only the
  published `ICurrentUser`/policy contract; it must not edit Auth/Identity.
- **Trường owns Recipe and File contracts:** Recipe lifecycle, `xmin`/ETag,
  nested Steps/Ingredients, RecipeImage and thumbnail event payload.  Kiên
  does not create a second Recipe model and starts FTS only after the Recipe
  schema lock milestone.
- **Kiên owns search and job contracts:** query normalization, FTS mapping,
  scheduler, idempotency/lease state and job retry semantics.  Event producers
  (Auth/File/Recipe) publish the agreed contract only; they do not implement
  jobs.

## Integration-only areas

- `ApplicationDbContextModelSnapshot` and the final combined EF migration are
  regenerated **only** on `integration/week-XX` by Hiếu.  Contributors never
  hand-merge the snapshot.
- Compose wiring, cross-module smoke tests and merge order are owned by Hiếu
  on `integration/week-XX`: `main → Auth → Recipe/File → Search/Jobs`.
- A consumer may use a producer's code only via a merge commit/cherry-pick that
  preserves authorship, or after the producer's published contract is merged.

## Delivery gates for every leaf issue

1. Keep changes within the owner subtree and a single reviewable intent.
2. Build Release without warnings; add unit tests for business/validation rules.
3. Add one endpoint integration happy path and one error path, including
   OpenAPI/Scalar summary, status codes and authorization.
4. For a migration: validate both clean database and previous-week database.
5. For frontend: supply loading, empty, validation and error states.
6. Link the completion commit/PR, CI run and any dependency issue before close.

## Dependency hand-offs

```text
Hiếu contracts ──► Bo / Trường / Kiên
Bo ICurrentUser + policies ──► Trường
Trường Recipe schema ──► Kiên FTS/search
Trường File thumbnail event ──► Kiên thumbnail job
Bo registration event ──► Kiên welcome-email job
Trường Published Recipe + Hiếu Category ──► Kiên sitemap job
All feature branches ──► Hiếu integration/week-XX ──► main
```
