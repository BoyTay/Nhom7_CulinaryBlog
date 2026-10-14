# W05 / #29 — Hangfire infrastructure

Owner: Kiên. Branch: `feature/fr-srch-job-kien`.

## Scope

PostgreSQL-backed Hangfire worker, queues, graceful shutdown, Admin dashboard,
execution leases, retry, cooperative timeout, daily sitemap registration and cleanup.
W06/#33 supplies the real email sender, image processor and sitemap writer. W05
runtime tests inject those interfaces; production never registers fake side effects.

## Configuration

Apply EF migrations before enabling Hangfire. Both Hangfire storage and
`JobExecutions` use `ConnectionStrings:Postgres`.

| Key under `Hangfire` | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | Start worker, recurring registration and dashboard |
| `WorkerCount` | `2` | Worker threads per API instance |
| `Queues` | `critical`, `default`, `maintenance` | Required queues; additional lowercase queues allowed |
| `RetryDelaysSeconds` | `60,300,900,1800,3600` | Five retries; exhausted jobs remain Failed |
| `TimeoutSeconds` | `900` | Cooperative cancellation of the operation |
| `LeaseSeconds` | `1200` | Claim expiry for crash recovery |
| `RenewalSeconds` | `30` | Renewal interval; must be less than half the lease |
| `RetentionDays` | `90` | Execution deduplication retention |
| `ShutdownTimeoutSeconds` | `30` | Graceful Hangfire shutdown wait |

Environment keys use double underscores, e.g. `Hangfire__WorkerCount=2` and
`Hangfire__RetryDelaysSeconds__0=60`. Arrays replace code defaults. Configuration
is validated at startup, including when Hangfire is disabled.

Existing Compose wiring supports `HANGFIRE_ENABLED=true` and Nginx proxies
`/hangfire` to the API. Compose wiring and shared snapshot integration remain
Hiếu's ownership. Keep the API running for recurring schedules to fire.

When disabled, enqueue returns an empty string and recurring registration is
ignored. This is intentionally a no-op, not an outbox: producers must not assume
those events will be replayed when the flag is enabled later.

## Security and health

Dashboard: `GET /hangfire`, HTML, requires the existing `AdminPolicy` and an
authenticated user. Anonymous gets 401; authenticated non-Admin gets 403; Admin
gets 200. Dashboard POST operations have the same authorization and retain
Hangfire anti-forgery protection. This is Hangfire's HTML UI, not a new REST API
operation in Scalar. Existing health endpoints remain unchanged.

The API uses JWT bearer authentication. For browser access, use an approved
client/proxy that supplies `Authorization: Bearer ...` on dashboard and asset
requests. Do not put access tokens in URLs or bypass the Admin filter. A dedicated
browser cookie/SSO flow requires Bo's published authentication contract.

When enabled, readiness includes `hangfire`: PostgreSQL storage must be reachable
and at least one worker with a recent heartbeat must exist. Liveness remains
independent. Health responses do not expose storage exceptions or connection strings.

## Execution semantics

- `TryClaimAsync=false` means the key is already Completed. A busy key throws
  `JobLeaseUnavailableException`, allowing Hangfire to retry instead of losing work.
- Each scoped store keeps a claim token. Only its owner can complete or release
  the row. Completion and renewal also require an unexpired lease.
- A failure releases the lease immediately; a process crash recovers after expiry.
  Old owners cannot change a successor's execution state.
- Renewal uses separate PostgreSQL connections so the operation can use its EF
  context concurrently. Renewal continues until the operation actually returns,
  even if it ignores cancellation. Renewal failure cancels the operation.
- Timeout is cooperative. W06 services must observe cancellation and must use
  idempotent writes/provider keys. A crash after a side effect but before Completed
  can redeliver that effect; this infrastructure does not promise exactly-once email.
- Keys must be nonempty and at most 256 characters. Avoid storing secrets in keys.
- Cleanup at `0 3 * * *` UTC removes old completed/failed records and long-expired
  Running leases, but never live Running leases. After retention, old events can
  execute again; keep retention longer than the supported event replay window.

## Sitemap handoff to #33

Register `ISitemapWriter`, `IWelcomeEmailSender` and `IRecipeImageProcessor` with
their real implementations. Jobs are registered already, but resolving an unavailable
service fails visibly rather than silently pretending to complete an effect.

With a writer present, `sitemap-daily` runs at `0 2 * * *` UTC on `maintenance`.
The Infrastructure wrapper uses `sitemap:{jobCreatedDate}:{hangfireJobId}` so retries
retain the same key across UTC midnight. AddOrUpdate reuses the recurring ID.
Without a writer, startup removes stale sitemap registration and logs a warning.
The Application `ExecuteRecurringAsync` entrypoint remains available for callers
that intentionally deduplicate by the current UTC date.

## Recovery

1. Inspect the Failed job in the protected dashboard and fix the missing service
   or external dependency before retrying. Avoid copying payloads into public logs.
2. Use Hangfire's Retry action to preserve the original job arguments/key.
3. If the worker crashed, wait for lease expiry; never manually complete a claim
   whose side effect has not been verified.
4. If retry attempts are exhausted, the job remains Failed for manual investigation.
5. Verify storage connectivity and an active worker when readiness fails.

## Verification

Run with a disposable PostgreSQL database and `ConnectionStrings__Postgres` configured:

```powershell
dotnet build CulinaryBlog.sln -c Release --no-restore --warnaserror
dotnet ef database update --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.API --configuration Release --no-build
dotnet test CulinaryBlog.sln -c Release --no-build
dotnet format CulinaryBlog.sln --no-restore --verify-no-changes
```

Focused tests: `BackgroundJobTests`, `BackgroundJobInfrastructureTests`,
`JobExecutionStoreTests`, `HangfireRuntimeTests`. Runtime tests create/drop their
own randomly named database; the test PostgreSQL user needs CREATE DATABASE.
Store tests use isolated schemas. Migration tests cover clean install, upgrade
from W03/W04 baseline including `AddRecipeImageVariants`, and rollback/reapply
of the execution store while preserving preexisting data. EF tooling generated
`20261014143221_AddJobExecutionStore` (including claim tokens), its `.Designer.cs`
and `ApplicationDbContextModelSnapshot` from `JobExecutionConfiguration` after
merging main. The former handwritten migrations were removed; the snapshot was
not edited manually. `dotnet ef migrations has-pending-model-changes` must pass.

Databases used only to test the former unpublished handwritten migrations must
be recreated for this replacement history; do not apply it on top of those tables.
The supported upgrade baseline is main/week-before without JobExecutions.

Before closing #29, link the completion commit/PR and green CI. Integration is
through `integration/week-05` using `--no-ff`, never direct to main.

References: [Issue #29](https://github.com/BoyTay/Nhom7_CulinaryBlog/issues/29),
[Hangfire cancellation](https://docs.hangfire.io/en/latest/background-methods/using-cancellation-tokens.html),
[Hangfire recurring jobs](https://docs.hangfire.io/en/latest/background-methods/performing-recurrent-tasks.html).
