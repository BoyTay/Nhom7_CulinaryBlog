# Kế hoạch triển khai theo tuần — Culinary Blog

> Nguồn: `WEEKLY_TEAM_PLAN.md`, cây issue GitHub `#5` và `tree.md`.
> Quy tắc đọc bảng: **Trước** = phải hoàn thành hoặc công bố contract/SHA trước
> khi bắt đầu; **Song song** = được triển khai độc lập; **Sau** = chỉ bắt đầu
> sau khi dependency đã được merge/cherry-pick giữ nguyên tác giả.

## Thứ tự ưu tiên xuyên suốt

```text
Hiếu: contract + nền tảng
   ├──► Bo: Identity/Auth/policy
   ├──► Trường: CategoryId/AuthorId + Recipe
   └──► Kiên: PagedResult + Search/Job contract

Bo: ICurrentUser / RBAC ──► Trường: Recipe owner authorization
Trường: Recipe schema lock ──► Kiên: FTS/search migration
Trường: File thumbnail event ──► Kiên: Thumbnail job
Bo: Registration event ──► Kiên: Welcome-email job
Trường Published Recipe + Hiếu Category ──► Kiên: Sitemap job
Tất cả ──► Hiếu: integration/week-XX ──► main
```

## Tuần 1 — Khóa contract và sửa nền nhánh (21/09–27/09)

| Thứ tự | Người / GitHub issue | Làm gì | Trước / sau / song song | Điểm có thể đụng nhau |
|---|---|---|---|---|
| 1 | Hiếu — #10 | Chốt `CategoryId`, `AuthorId`, role/policy, `PagedResult`, Problem Details; tạo shared frontend foundation và Category domain/EF baseline. | **Làm đầu tiên**. Công bố SHA baseline trước khi các nhánh consumer rebase. | Owner duy nhất của shared contract và frontend shared; không ai tự sửa các contract này. |
| 2 | Bo — #11 | Bỏ default JWT/Admin secret; verify Google ID token; cấu hình JWT/authz/middleware/role seeder; test security failure. | Có thể khởi động song song, nhưng tên role/policy phải chờ #10 chốt. | Không sửa Recipe policy consumer; chỉ publish contract để Trường dùng từ tuần 3–4. |
| 2 | Trường — #12 | So khớp Recipe với SRS, tối ưu read query, kiểm tra migration và concurrency invariant. | Chờ **SHA #10** để rebase một lần; sau đó làm độc lập. | Khóa Recipe schema ở cuối tuần; không tạo FTS/migration Search. |
| 2 | Kiên — #13 | Chốt validation Search và semantics retry/idempotency/no-op Hangfire; viết contract tests. | Làm song song; **không** tạo FTS migration trước khi #12 khóa schema. | Không chỉnh Recipe model hoặc snapshot. |

**Cổng cuối tuần:** #10 công bố contract/baseline; #12 công bố Recipe-schema lock; bốn nhánh CI xanh.

## Tuần 2 — CRUD lõi và read/write đầu tiên (28/09–04/10)

| Thứ tự | Người / GitHub issue | Làm gì | Trước / sau / song song | Điểm có thể đụng nhau |
|---|---|---|---|---|
| 1 | Hiếu — #14 | Category Create/Update/Delete, queries, migration, seed và unit tests. | Độc lập sau baseline W01. | Chỉ Hiếu tạo Category migration. |
| 1 | Bo — #15 | Register/Login, access 15 phút, refresh-token hash/cookie, rate limit và integration tests. | Độc lập với #14/#16; publish auth endpoint/event contract khi xong. | Chỉ Bo sửa Identity/Auth migration và refresh cookie. |
| 1 | Trường — #16 | Recipe create/list/detail, filter cơ bản và visibility. | Độc lập sau schema lock #12; dùng `AuthorId` contract #10. | Chỉ Trường sửa Recipe API/migration. |
| 2 | Kiên — #17 | EF Search reader, Published-only filter/sort/pagination và endpoint non-FTS. | **Sau #12/#16 schema/read contract**; có thể code adapter sau schema lock, integration test sau Recipe API. | Không tạo FTS extension/index ở tuần này. |

**Có thể song song:** #14, #15, #16. **Phải làm sau:** #17 phụ thuộc Recipe schema/API.

## Tuần 3 — Lifecycle nghiệp vụ và Full-Text Search (05/10–11/10)

| Thứ tự | Người / GitHub issue | Làm gì | Trước / sau / song song | Điểm có thể đụng nhau |
|---|---|---|---|---|
| 1 | Hiếu — #18 | Hoàn tất Category endpoints, cache/invalidation, FR-CAT tests và khởi đầu Admin UI. | Độc lập. | Cache/category UI thuộc Hiếu. |
| 1 | Bo — #19 | Refresh rotation, reuse detection, logout/revoke-all, `/refresh`, `/logout`, `/me`. | Độc lập; cần thông báo cookie/session behavior cho frontend W06. | Chỉ Bo sửa refresh-token family. |
| 1 | Trường — #20 | Recipe update/publish/archive/soft delete, ETag/xmin, owner/Admin authz. | Policy/current-user contract cần từ #11; nếu chưa merge, dùng contract đã công bố, không copy code. | Recipe lifecycle chỉ Trường sửa. |
| 2 | Kiên — #21 | `unaccent`, `pg_trgm`, SearchVector/trigger/GIN migration; FTS endpoint, real PostgreSQL test và EXPLAIN. | **Sau #12 schema lock**; nên nhận #20 nếu FTS cần status/soft-delete cuối cùng. | Đây là migration duy nhất của Search; không tự sửa Recipe snapshot. |

**Điểm chạm lớn:** #21 phải được review sau Recipe schema/lifecycle; snapshot tích hợp chỉ regenerate trên `integration/week-03`.

## Tuần 4 — Nested resources, RBAC và UI vòng 1 (12/10–18/10)

| Thứ tự | Người / GitHub issue | Làm gì | Trước / sau / song song | Điểm có thể đụng nhau |
|---|---|---|---|---|
| 1 | Bo — #23 | Hoàn tất RBAC, `ICurrentUser`, Recipe owner/Admin resource authorization và Profile. | **Làm trước hoặc công bố contract trước** #24. | Chỉ Bo sửa policy/claims/current-user. |
| 2 | Trường — #24 | Steps/Ingredients CRUD, renumber/sort-order, nested authorization và tests. | **Sau #23 contract**; domain work có thể chuẩn bị song song. | Không chỉnh Auth policy implementation. |
| 1 | Hiếu — #22 | Hoàn tất Admin Category UI; Redis/MinIO health; MediatR logging/performance behavior. | Độc lập. | Health/observability là vùng Hiếu. |
| 1 | Kiên — #25 | Search UI URL state, TanStack Query, debounce/loading/empty/error và đo tốc độ. | Độc lập sau #17/#21 API; dùng shared query key #10. | Không sửa shared provider/API client trực tiếp. |

## Tuần 5 — File, Google Login, Hangfire và tracing (19/10–25/10)

| Thứ tự | Người / GitHub issue | Làm gì | Trước / sau / song song | Điểm có thể đụng nhau |
|---|---|---|---|---|
| 1 | Trường — #28 | `IFileStorageService`, MinIO upload validation, primary image/delete, publish thumbnail event. | **Phải công bố event payload trước** #33. | File/MinIO implementation chỉ Trường sở hữu. |
| 1 | Bo — #27 | Google Login, account-linking an toàn, rate limit và security tests. | **Phải công bố registration event trước** #33. | Auth event producer chỉ Bo sở hữu. |
| 1 | Hiếu — #26 | EF tracing, correlation, Category metrics, OTLP docs và kiểm soát sensitive logging. | Song song. | Observability pipeline chỉ Hiếu sửa. |
| 2 | Kiên — #29 | Hangfire worker/queue/dashboard, `IJobExecutionStore`, retry/timeout/recurring sitemap và tests. | Hạ tầng làm song song; dashboard dùng Admin policy #23. | Không implement side effect của File/Auth trong issue này. |

## Tuần 6 — Frontend nghiệp vụ và ba job thật (26/10–01/11)

| Thứ tự | Người / GitHub issue | Làm gì | Trước / sau / song song | Điểm có thể đụng nhau |
|---|---|---|---|---|
| 1 | Hiếu — #30 | Observability runbook, Category cache verification và shared form/pagination/alert/skeleton. | Làm đầu tuần để frontend khác tái sử dụng component. | Shared components chỉ Hiếu sửa. |
| 2 | Bo — #31 | Login/Register/Profile UI, Google button, session/protected routes. | Sau endpoint contract #15/#19/#27; dùng shared UI #30. | Không sửa provider/query foundation của Hiếu. |
| 2 | Trường — #32 | Recipe list/detail/editor, dynamic Steps/Ingredients, upload progress, Next Image config. | Sau #16/#20/#24/#28; dùng shared UI #30. | Recipe/File frontend chỉ Trường sở hữu. |
| 3 | Kiên — #33 | Welcome email từ Auth event, Thumbnail từ File event, Sitemap từ Published Recipe/Category, retry/error handling. | **Sau #27 event, #28 event, #29 infrastructure**; Sitemap cần Recipe/Category published contract. | Kiên chỉ consume event, không sửa producer module. |

## Tuần 7 — Hardening theo module (02/11–08/11)

| Thứ tự | Hiếu #34 | Bo #35 | Trường #36 | Kiên #37 |
|---|---|---|---|---|
| Làm song song | FR-CAT/OBS tests, accessibility, degraded health, runbook. | Refresh/revoke/lockout/RBAC tests, cookie/CORS/audit, Identity migration. | Lifecycle/file/concurrency/rollback tests, query tuning. | Vietnamese FTS, retry/idempotency/failure logging, GIN evidence, sitemap canonical. |
| Chỉ phối hợp khi | Kiểm tra health Redis/MinIO với #36/#37. | Test policy failure mà #36 sử dụng. | Thử lỗi MinIO/job với #37. | Test event retries với #27/#28 producer. |

## Tuần 8 — Integration có kiểm soát (09/11–15/11)

| Thứ tự | Người / GitHub issue | Làm gì | Dependency / phối hợp |
|---|---|---|---|
| 1 | Hiếu — #40 | Tạo `integration/week-08`, merge theo thứ tự, regenerate combined migration/snapshot, Compose + smoke test. | Tất cả branch phải CI xanh trước khi giao cho Hiếu. |
| 2 | Bo — #43 | Xác minh claims Recipe, welcome-email event và refresh cookie qua Nginx; sửa contract mismatch không phá API. | Phối hợp #45/#46. |
| 2 | Trường — #45 | Xác minh CategoryId/AuthorId/owner auth, upload → thumbnail → RecipeImage, soft delete không xuất hiện Search/Sitemap. | Phối hợp #43/#46. |
| 2 | Kiên — #46 | Xác minh Search Recipe thật, ba job bằng event thật, reconciliation/manual retry. | Phối hợp #43/#45. |

**Không làm song song trên cùng snapshot:** chỉ #40 regenerate migration/snapshot sau khi lần lượt merge branch.

## Tuần 9 — E2E, performance, security và tài liệu (16/11–22/11)

| Người / issue | Việc chính | Cần phối hợp |
|---|---|---|
| Hiếu — #47 | Category/health E2E, README/architecture/migration/demo, tổng hợp evidence. | Nhận CI/coverage/link từ #48–#50. |
| Bo — #48 | Auth + Google E2E, tài liệu secret/Google/API/security. | Không dùng credential thật trong tài liệu. |
| Trường — #49 | Recipe/File E2E, SEO metadata/structured data, MinIO/concurrency docs. | Cần thumbnail job #33 chạy trên môi trường tích hợp. |
| Kiên — #50 | Search/jobs E2E, large-seed performance, FTS/Hangfire failure docs. | Cần Published Recipe/Category và job event thật. |

## Tuần 10 — Buffer, release và rehearsal (23/11–29/11)

| Thứ tự | Người / GitHub issue | Làm gì | Quy tắc |
|---|---|---|---|
| 1 | Hiếu — #51 | Scope freeze, audit branch/CI/ownership, architecture diagram/demo/recovery plan. | Chỉ nhận bug fix đã review. |
| 2 | Bo — #52 | Sửa Auth/Profile bug, rehearsal rotation/RBAC/lockout/Google, release tag. | Không mở feature mới. |
| 2 | Trường — #53 | Sửa Recipe/File bug, rehearsal concurrency/MinIO/lifecycle, release tag. | Không mở feature mới. |
| 2 | Kiên — #54 | Sửa Search/Job bug, rehearsal FTS/GIN/retry, release tag. | Không mở feature mới. |

## Quy trình phối hợp mỗi tuần

1. **Thứ Hai:** Hiếu xác nhận contract/dependency đã sẵn sàng; mỗi người chỉ chọn tối đa ba mục tiêu chính.
2. **Thứ Năm 18:00:** feature freeze; mọi người push commit chạy được, cập nhật issue với SHA, CI, completed/not completed/blocker.
3. **Thứ Sáu:** Hiếu review contract, migration, security và test gate; phần không đạt giữ trên branch cá nhân.
4. **Thứ Bảy:** tạo `integration/week-XX` từ `main`; merge `main → Auth → Recipe/File → Search/Jobs` bằng `--no-ff`; sau nhánh cuối mới regenerate migration/snapshot.
5. **Chủ Nhật:** chỉ merge integration vào `main` khi build/test/Compose/smoke test xanh; Thứ Hai sau mỗi người merge `origin/main` về branch cá nhân, không rebase/force-push branch đã công bố.

## Quy tắc quyết định khi có blocker

- Contract chưa chốt: gắn `blocked`, link issue producer và chuyển sang phần không phụ thuộc.
- Hai issue cùng cần sửa shared contract/migration/snapshot: **không code đồng thời**; Hiếu mở/chốt contract trước, integration branch xử lý snapshot sau cùng.
- Producer trễ: consumer viết interface/test giả theo contract đã duyệt, không copy model hoặc sửa module của producer.
- CI đỏ: không chuyển sang integration; issue chỉ được close khi đã có commit/PR và CI xanh.
