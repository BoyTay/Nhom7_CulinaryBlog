# Kế hoạch công việc theo tuần - Nhóm 7 Culinary Blog

## 1. Mục đích và phạm vi

Kế hoạch này chia nhỏ phần việc của bốn thành viên từ ngày 21/09/2026 đến ngày 29/11/2026. Mỗi tuần phải tạo ra một phần mềm chạy được, có kiểm thử và có bằng chứng trên GitHub; không dùng phần trăm hoàn thành chỉ dựa trên số file hoặc số commit.

Nguồn dùng để lập kế hoạch:

- Mã nguồn và lịch sử commit của `main` tại `2a18afd`.
- Nhánh `feature/fr-auth-bo` tại `2cef96f`.
- Nhánh `feature/fr-rcp-file-truong` tại `74c9e17`.
- Nhánh `feature/fr-srch-job-kien` tại `cb5574b`.
- SRS Culinary Blog 1.0.1, các ADR và kế hoạch triển khai trong repository.
- Chương 1: Kiến trúc Web và RESTful API.
- Chương 2: Xác thực, Phân quyền và Bảo mật API.
- Chương 3: Thiết kế Dữ liệu và Tối ưu Truy vấn.
- Chương 4: Kiến trúc Frontend với Next.js.

## 2. Thứ tự ưu tiên khi tài liệu khác nhau

Khi tài liệu môn học và dự án có nội dung khác nhau, nhóm áp dụng thứ tự sau:

1. SRS phiên bản 1.0.1 đã phê duyệt.
2. ADR trong `docs/adr`.
3. Hợp đồng API đã được nhóm trưởng chốt.
4. Tài liệu chương 1-4 chỉ dùng làm hướng dẫn kỹ thuật và ví dụ tham khảo.

Các quyết định bắt buộc của dự án:

- Dùng PostgreSQL system column `xmin` ánh xạ thành `uint Version`; không dùng `byte[] RowVersion` theo ví dụ SQL Server trong Chương 3.
- Dùng `IDataSession` để commit và repository/query contract theo module; không tạo một `IUnitOfWork` khổng lồ chứa repository của mọi module.
- Refresh token thô được gửi bằng cookie `HttpOnly`, `Secure`, `SameSite=Strict`; database chỉ lưu token hash.
- Google Identity Services chạy ở frontend; backend phải xác minh chữ ký, issuer, audience và thời hạn của ID token trước khi tạo/liên kết tài khoản.
- Recipe và Category dùng soft delete. Dữ liệu con chỉ bị xóa vật lý bởi tác vụ bảo trì sau thời hạn lưu giữ.

## 3. Trạng thái tại thời điểm lập kế hoạch - 22/09/2026

### Hiếu - `main`

Đã có:

- Clean Architecture, CQRS contracts, validation, middleware và RFC 7807.
- PostgreSQL/EF Core baseline, migration nền và Docker Compose.
- Next.js shell và design system nền.
- Health endpoints, PostgreSQL readiness, Serilog, Seq và OpenTelemetry foundation.
- CI backend/frontend/Compose và tài liệu bàn giao.

Còn thiếu:

- Toàn bộ FR-CAT backend và frontend Admin.
- Redis/MinIO health checks, MediatR logging/performance behavior, EF Core tracing và business metrics.
- Shared frontend API client, providers và form/query foundation.

### Bo - `feature/fr-auth-bo`

Đã có:

- Identity/JWT/Google dependency.
- `ApplicationUser`, `RefreshToken`, identity contracts và repository.
- Identity configuration, migration và role seeder.
- Nhánh build sạch, test hiện có pass và CI xanh.

Còn thiếu hoặc phải sửa trước:

- Chưa có command/query/validator/handler và Auth endpoints.
- Chưa cấu hình authentication/authorization middleware trong API.
- Chưa gọi `RoleSeeder` khi khởi động.
- Google ID token đang chỉ được đọc, chưa được xác minh mật mã.
- JWT key và tài khoản Admin không được có giá trị mặc định dùng được trong source code.
- Chưa có refresh rotation flow, cookie, logout, RBAC/resource authorization, profile UI và test nghiệp vụ Auth.

### Trường - `feature/fr-rcp-file-truong`

Đã có:

- Recipe aggregate, Step, Ingredient, Image, Nutrition và enum.
- EF Core configuration, migration và repository.
- Mapping `Version` sang PostgreSQL `xmin`.
- Unit tests cho một số invariant của aggregate.
- Nhánh build sạch, 11 test hiện có pass và CI xanh.

Còn thiếu:

- Chưa có CQRS use case, validator, DTO và API endpoints.
- Chưa có update cho Step/Ingredient và đầy đủ vòng đời Recipe theo SRS.
- Repository đọc nhiều collection chưa dùng `AsSplitQuery`/projection phù hợp.
- Chưa có MinIO service, upload validation, thumbnail contract và integration tests.
- Chưa có frontend Recipe/File.

### Kiên - `feature/fr-srch-job-kien`

Đã có:

- Contract `RecipeSearchOptions`, `RecipeSearchResult`, `IRecipeSearchReader` và `PagedResult<T>`.
- Contract lập lịch job, dịch vụ tích hợp và idempotency store ở Application layer.
- Skeleton cho welcome email, thumbnail và sitemap job; các job bỏ qua idempotency key đã được claim.
- Hangfire adapter và cấu hình PostgreSQL storage có feature flag `Hangfire:Enabled`.
- ADR-0004 mô tả ranh giới Search/Background Jobs, thứ tự migration và nguyên tắc idempotency.
- Nhánh tại `cb5574b` build Release sạch; 11/11 test hiện có pass.

Còn thiếu hoặc phải hoàn thiện:

- Validation chưa bắt buộc từ khóa FTS tối thiểu 2 ký tự theo SRS và chưa có FluentValidation/query handler.
- Chưa có implementation `IRecipeSearchReader`, filter/sort/pagination trên dữ liệu Recipe thật hoặc endpoint Search.
- Chưa có PostgreSQL `unaccent`, `pg_trgm`, SearchVector, trigger, GIN index và migration FTS.
- `IJobExecutionStore` chưa có persistence/lease để job lỗi vẫn retry được; mới chỉ có contract.
- Khi `Hangfire:Enabled=false`, `IBackgroundJobScheduler` không được đăng ký; module gọi scheduler có thể lỗi DI nếu chưa có chiến lược no-op/optional rõ ràng.
- Chưa đăng ký ba job và dịch vụ thật trong DI, chưa có retry policy, queue, timeout hoặc dashboard bảo vệ bằng Admin policy.
- Test job mới chỉ kiểm tra duplicate welcome email; chưa kiểm tra success/failure/retry và hai job còn lại.
- Chưa có email sender, image processor, sitemap writer hoặc tích hợp event từ Auth/File/Recipe.
- Chưa có frontend Search.

## 4. Quy tắc làm việc chung

- Hiếu quản lý `main`; Bo, Trường và Kiên làm trên nhánh cá nhân đã có.
- Không copy-paste code giữa nhánh. Nếu một nhánh cần nền của nhánh khác, dùng merge commit hoặc cherry-pick có giữ nguyên tác giả.
- Mọi thay đổi hợp đồng dùng chung phải được ghi vào SRS/ADR trước khi code phụ thuộc vào hợp đồng đó.
- Không merge nhánh thành viên vào `main` trong giai đoạn giáo viên chấm riêng, trừ khi nhóm trưởng xác nhận bằng văn bản.
- Không tự ghép các file `ApplicationDbContextModelSnapshot`. Khi tích hợp nhiều module, regenerate migration hợp nhất trên nhánh tích hợp.
- Mỗi commit chỉ chứa một ý nghĩa review được. Không dùng commit kiểu `update`, `fix all` hoặc gom backend, test và frontend không liên quan vào cùng một commit.
- Mỗi tuần kết thúc với CI xanh, danh sách endpoint đã thử trên Scalar và báo cáo ngắn về phần hoàn thành/chưa hoàn thành.

## 5. Definition of Done cho một công việc

Một công việc chỉ được xem là hoàn thành khi đáp ứng đủ:

- Code build không warning với cấu hình Release.
- Unit test cho business rules và validator quan trọng.
- Integration test tối thiểu một happy path và một error path cho endpoint mới.
- OpenAPI/Scalar có summary, response codes và authorization requirement đúng.
- Không hardcode secret, password, token hoặc URL production.
- Migration chạy được trên database mới và database đã có migration baseline.
- Frontend có loading, empty, validation và error state.
- CI của nhánh xanh và commit đã được push.

## 6. Lịch thực hiện 10 tuần

### Tuần 1 - 21/09 đến 27/09: Khóa hợp đồng và sửa nền nhánh

#### Hiếu

- Chốt contract `CategoryId`, `AuthorId`, role/policy, `PagedResult<T>` và Problem Details error codes.
- Bổ sung shared frontend foundation: API types, server/client fetch helper, provider boundary và quy ước query keys.
- Tạo Category aggregate, invariant, repository/query contracts và EF configuration.
- Công bố mốc baseline dùng chung để thành viên rebase một lần trước Tuần 2.

Đầu ra: tài liệu contract cập nhật, Category domain/persistence compile được và baseline dùng chung ổn định.

#### Bo

- Bỏ JWT secret mặc định và Admin password mặc định; bắt buộc đọc từ secret/environment.
- Thay việc `ReadJwtToken` bằng xác minh Google ID token đúng chữ ký, issuer, audience và expiry.
- Cấu hình `AddAuthentication`, `AddJwtBearer`, authorization policies và đúng thứ tự middleware.
- Gọi role seeder có kiểm soát trong Development; production chỉ seed khi có cấu hình hợp lệ.
- Viết test cho `RefreshToken`, JWT claims và Google token validation failure.

Đầu ra: không còn lỗ hổng nền Auth, CI xanh và không có secret dùng được trong Git.

#### Trường

- Rebase từ baseline mới của Hiếu sau khi contract được chốt.
- Đối chiếu Recipe aggregate với SRS: field bắt buộc, status transition, soft delete, `PublishedAt`, version và error codes.
- Sửa query repository dùng `AsNoTracking`, projection hoặc `AsSplitQuery` theo từng use case.
- Kiểm tra migration từ database trắng và bổ sung test cho update/archive/delete/concurrency invariant.

Đầu ra: commit/tag schema Recipe ổn định để Kiên sử dụng từ Tuần 2.

#### Kiên

- Giữ nhánh tại mốc `cb5574b`; chỉ rebase khi Hiếu công bố baseline mới.
- Hoàn thiện validation contract: `q` tối thiểu 2 ký tự khi tìm kiếm, chuẩn hóa sort và quy tắc chỉ dùng `relevance` khi có `q`.
- Làm rõ `IJobExecutionStore`: claim phải có trạng thái/lease; job lỗi không được bị khóa vĩnh viễn và chỉ đánh dấu completed sau side effect thành công.
- Chốt hành vi khi Hangfire tắt: dùng no-op/outbox phù hợp hoặc không kích hoạt producer; không để API lỗi resolve DI.
- Bổ sung test success, duplicate và side-effect failure cho cả welcome email, thumbnail và sitemap.
- Chưa tạo migration FTS trước khi Trường khóa schema Recipe.

Đầu ra: contract/ADR đã review, semantics retry/idempotency rõ ràng, test contract đầy đủ và chưa phát sinh migration xung đột.

Mốc kiểm tra tuần: bốn nhánh CI xanh; Auth không có default secret; Recipe schema được đánh dấu ổn định.

### Tuần 2 - 28/09 đến 04/10: CRUD lõi và luồng đọc/ghi đầu tiên

#### Hiếu

- Hoàn thiện Create/Update/Delete Category command, validator và handler.
- Hoàn thiện GetCategories và GetCategoryBySlug query.
- Tạo migration Category và dữ liệu seed tối thiểu phục vụ development.
- Viết unit tests cho slug, unique name, soft delete và không xóa Category đang có Recipe.

Đầu ra: FR-CAT backend use cases chạy qua test, chưa cần giao diện hoàn chỉnh.

#### Bo

- Cài đặt Register và Login command/validator/handler.
- Phát access token 15 phút; tạo refresh token, lưu hash và gửi raw token bằng cookie.
- Cài đặt `/api/v1/auth/register` và `/api/v1/auth/login` với rate limit.
- Viết integration tests cho duplicate email, weak password, invalid credentials và lockout.

Đầu ra: đăng ký/đăng nhập chạy qua Scalar và cookie đúng thuộc tính bảo mật.

#### Trường

- Tạo DTO, validator và handlers cho Create Recipe, Get list và Get detail.
- Cài đặt endpoint công khai cho danh sách/chi tiết và endpoint Author/Admin để tạo Recipe.
- Hỗ trợ filter cơ bản Category/Difficulty và authorization visibility Draft/Published/Archived.
- Viết integration tests cho create/list/detail.

Đầu ra: Recipe có thể tạo và đọc qua API.

#### Kiên

- Nhận schema Recipe ổn định bằng merge/cherry-pick giữ nguyên tác giả của Trường.
- Cài đặt `IRecipeSearchReader` bằng EF Core cho filter Category/Difficulty/MaxCookTime/MinServings, sắp xếp và offset pagination.
- Chỉ đọc Published Recipe; dùng `AsNoTracking`, projection DTO và query đếm tổng riêng.
- Tạo `SearchRecipesQuery`, validator, handler và endpoint; chưa bật FTS trong tuần này.
- Viết unit/integration test cho tham số biên, sort tăng/giảm, visibility và empty result.

Đầu ra: FR-SRCH-002/003/004 chạy được chưa cần FTS.

Mốc kiểm tra tuần: Category/Auth/Recipe đều có ít nhất một endpoint chạy; Kiên có pagination contract thống nhất.

### Tuần 3 - 05/10 đến 11/10: Hoàn thiện vòng đời nghiệp vụ và Full-Text Search

#### Hiếu

- Map toàn bộ Category endpoints, OpenAPI metadata và error codes.
- Thêm cache danh mục TTL 60 phút và cache invalidation khi mutation.
- Thêm integration tests cho 5 yêu cầu FR-CAT.
- Bắt đầu giao diện danh sách và form Admin Category.

Đầu ra: FR-CAT backend hoàn chỉnh.

#### Bo

- Cài đặt refresh token rotation, phát hiện reuse và thu hồi token family phù hợp phạm vi MVP.
- Cài đặt logout và revoke all sessions.
- Thêm `/api/v1/auth/refresh`, `/logout` và `/me`.
- Kiểm tra cookie/origin/CORS trên cùng origin qua Nginx.

Đầu ra: vòng đời Register → Login → Refresh → Logout chạy end-to-end.

#### Trường

- Cài đặt Update, Publish/Unpublish, Archive và Soft Delete Recipe.
- Áp dụng owner/Admin authorization ở resource level.
- Xử lý `xmin`/ETag và trả `422 RECIPE_CONCURRENCY_CONFLICT`.
- Viết test cho publish thiếu Step/Ingredient và concurrent update.

Đầu ra: vòng đời Recipe theo SRS hoàn chỉnh.

#### Kiên

- Tạo PostgreSQL `unaccent`, `pg_trgm`, SearchVector, trigger và GIN index bằng migration riêng trên schema Recipe đã khóa.
- Cài đặt FTS prefix query không dấu/có dấu, chỉ tìm Published Recipe và xếp hạng bằng `ts_rank`.
- Map `/api/v1/recipes/search` với pagination/filter/sort kết hợp.
- Thêm integration test chạy PostgreSQL thật; ghi lại `EXPLAIN ANALYZE` trước/sau index trong tài liệu kỹ thuật.

Đầu ra: FR-SRCH-001 hoàn chỉnh và có bằng chứng dùng index.

Mốc kiểm tra tuần: backend chính của bốn thành viên có thể demo độc lập.

### Tuần 4 - 12/10 đến 18/10: Module con, phân quyền và giao diện nền

#### Hiếu

- Hoàn thiện giao diện Admin Category: list, create, edit, delete confirmation.
- Thêm loading/error/empty state và validation theo API Problem Details.
- Thêm Redis và MinIO readiness checks.
- Cài đặt MediatR logging/performance behavior và cảnh báo request chậm hơn 500 ms.

Đầu ra: FR-CAT backend + frontend hoàn chỉnh; FR-OBS bổ sung health/logging.

#### Bo

- Hoàn thiện RBAC Admin/Author và `ICurrentUser`.
- Cài đặt resource authorization contract cho Recipe owner/Admin.
- Cài đặt xem/cập nhật profile.
- Viết test authorization cho anonymous, owner, non-owner và Admin.

Đầu ra: Auth/Profile/RBAC sẵn sàng cho Recipe sử dụng.

#### Trường

- Cài đặt CRUD Recipe Steps và đảm bảo renumber nhất quán.
- Cài đặt CRUD Recipe Ingredients và đảm bảo sort order nhất quán.
- Bổ sung endpoint, validator và authorization cho nested resources.
- Viết integration tests cho add/update/delete Step và Ingredient.

Đầu ra: FR-RCP-009/010 hoàn chỉnh.

#### Kiên

- Hoàn thiện UI Search với URL state cho query/filter/sort/page.
- Dùng TanStack Query cho server state và giữ query keys ổn định.
- Thêm debounce, loading skeleton, empty state và error state.
- Kiểm tra response time với dữ liệu seed đủ lớn.

Đầu ra: Search backend + frontend có thể demo.

Mốc kiểm tra tuần: hoàn thành demo vòng 1 cho Category, Auth, Recipe core và Search.

### Tuần 5 - 19/10 đến 25/10: File storage, Google Login, Hangfire và tracing

#### Hiếu

- Thêm EF Core instrumentation, trace correlation và metrics request/error/duration.
- Thêm custom metrics Category created/updated/deleted.
- Cấu hình OTLP development và tài liệu xem trace/log trên Seq hoặc công cụ phù hợp.
- Kiểm tra không log token, cookie, password và dữ liệu nhạy cảm.

Đầu ra: FR-OBS tracing/metrics foundation hoàn chỉnh.

#### Bo

- Hoàn thiện Google Login theo ADR-0003.
- Xử lý liên kết email đã tồn tại một cách an toàn, không chiếm đoạt tài khoản.
- Áp dụng rate limiting cho login/register/refresh.
- Viết integration tests cho Google token sai audience/issuer/expired và account linking.

Đầu ra: Email login và Google login đều hoạt động an toàn.

#### Trường

- Tạo `IFileStorageService` và MinIO implementation.
- Cài đặt upload với giới hạn kích thước, MIME allow-list và magic-byte validation.
- Cài đặt set primary/delete image; đảm bảo một ảnh chính trên mỗi Recipe.
- Phát event/contract ổn định cho Kiên tạo thumbnail.

Đầu ra: FR-FILE backend hoàn chỉnh và thumbnail contract được khóa.

#### Kiên

- Hoàn thiện cấu hình Hangfire PostgreSQL đã có: worker/queue, server lifecycle và health check.
- Thêm Hangfire Dashboard và bắt buộc xác thực bằng Admin policy; không để dashboard public.
- Cài đặt persistence cho `IJobExecutionStore` với unique idempotency key, trạng thái/lease và cleanup policy.
- Đăng ký ba job trong DI, cấu hình retry/timeout và recurring sitemap `0 2 * * *` UTC.
- Viết integration test cho DI, PostgreSQL storage, recurring registration và idempotency cạnh tranh.

Đầu ra: FR-JOB infrastructure chạy được end-to-end; skeleton hiện có được nối vào Hangfire nhưng chưa cần hoàn thiện side effect của ba job.

Mốc kiểm tra tuần: MinIO upload và Hangfire server cùng chạy trong Compose.

### Tuần 6 - 26/10 đến 01/11: Frontend nghiệp vụ và ba Background Jobs

#### Hiếu

- Hoàn thiện dashboard quan sát tối thiểu hoặc tài liệu truy vấn log/trace/metrics.
- Kiểm tra Category cache hit/miss và invalidation bằng integration test.
- Chuẩn hóa shared frontend components: form field, pagination, alert và skeleton.
- Review contract giữa bốn module, không đưa code thành viên vào `main` khi chưa được phép.

Đầu ra: FR-OBS có hướng dẫn vận hành và shared UI ổn định.

#### Bo

- Xây dựng trang Login, Register và Profile.
- Tích hợp Google button, session state và protected routes.
- Xử lý access token in-memory và refresh cookie theo cùng origin.
- Thêm loading, validation, lockout và unauthorized UX.

Đầu ra: Auth/Profile frontend hoàn chỉnh.

#### Trường

- Xây dựng Recipe list/detail với chiến lược render phù hợp.
- Xây dựng Create/Edit Recipe form bằng React Hook Form + Zod.
- Hỗ trợ dynamic Steps/Ingredients và upload ảnh có progress.
- Cấu hình Next Image cho MinIO/CDN và ảnh placeholder.

Đầu ra: Recipe/File frontend có thể demo end-to-end.

#### Kiên

- Hoàn thiện welcome email job nhận event từ Auth.
- Hoàn thiện thumbnail job nhận event từ File và cập nhật URL an toàn/idempotent.
- Hoàn thiện sitemap job cho Published Recipe và Category.
- Thêm retry policy và xử lý lỗi từng job.

Đầu ra: FR-JOB-001/002/003 chạy được bằng trigger thủ công.

Mốc kiểm tra tuần: tất cả module có cả backend và frontend hoặc giao diện vận hành phù hợp.

### Tuần 7 - 02/11 đến 08/11: Kiểm thử sâu và hardening theo module

#### Hiếu

- Đạt test đầy đủ cho FR-CAT và FR-OBS.
- Kiểm tra accessibility bàn phím, focus, contrast và responsive Admin UI.
- Kiểm tra health degradation khi tắt PostgreSQL/Redis/MinIO.
- Cập nhật runbook xử lý lỗi môi trường local.

#### Bo

- Bổ sung unit/integration tests cho refresh reuse, revoke, lockout, RBAC và profile.
- Kiểm tra cookie flags, CORS, Origin và rate-limit responses.
- Chạy dependency audit và rà soát không lộ secret/PII trong log.
- Kiểm tra migration Identity trên database mới.

#### Trường

- Bổ sung tests cho toàn bộ Recipe status transition, nested entities và file lifecycle.
- Kiểm tra concurrent update thực tế với hai request.
- Kiểm tra rollback khi DB save thành công nhưng MinIO/job thất bại và ngược lại.
- Tối ưu query detail/list bằng projection, `AsNoTracking` và `AsSplitQuery` đúng chỗ.

#### Kiên

- Test FTS tiếng Việt có dấu/không dấu, filter/sort/page kết hợp.
- Test job retry, idempotency và failure logging.
- Đo FTS bằng `EXPLAIN ANALYZE`; xác nhận GIN index được dùng.
- Kiểm tra sitemap chỉ chứa Published Recipe và URL canonical.

Đầu ra chung: mỗi nhánh có test report, danh sách rủi ro còn lại và CI xanh.

### Tuần 8 - 09/11 đến 15/11: Kiểm thử liên-module có kiểm soát

#### Hiếu

- Chuẩn bị checklist tích hợp và một nhánh tích hợp tạm thời nếu giáo viên cho phép.
- Regenerate migration tích hợp; không ghép thủ công ModelSnapshot.
- Kiểm tra Compose với schema tổng hợp và dữ liệu mới.
- Điều phối smoke test toàn hệ thống.

#### Bo

- Kiểm tra Author/Admin claims được Recipe sử dụng đúng.
- Kiểm tra welcome email event và refresh cookie qua Nginx.
- Sửa contract mismatch mà không phá API đã công bố.

#### Trường

- Kiểm tra CategoryId, AuthorId và owner authorization với module thật.
- Kiểm tra upload → enqueue thumbnail → cập nhật RecipeImage.
- Kiểm tra soft delete Recipe không xuất hiện trong Search/Sitemap.

#### Kiên

- Kiểm tra Search trên dữ liệu Recipe thật.
- Kiểm tra welcome email, thumbnail và sitemap với event thật.
- Bổ sung reconciliation job hoặc hướng dẫn retry thủ công nếu event bị bỏ lỡ.

Đầu ra chung: biên bản tích hợp, danh sách lỗi theo người phụ trách và không làm mất lịch sử tác giả.

### Tuần 9 - 16/11 đến 22/11: E2E, hiệu năng, bảo mật và tài liệu

#### Hiếu

- Chạy E2E cho Category Admin và health/observability.
- Cập nhật README, kiến trúc, migration guide và demo script.
- Tổng hợp coverage, CI links và bằng chứng đóng góp từng thành viên.

#### Bo

- E2E Register → Login → Profile → Refresh → Logout và Google Login.
- Viết hướng dẫn cấu hình Google credentials và JWT secrets không chứa giá trị thật.
- Hoàn thiện tài liệu API Auth và security decisions.

#### Trường

- E2E Author tạo Recipe → thêm Step/Ingredient → upload ảnh → publish → edit.
- Kiểm tra SEO metadata/structured data cho Recipe detail.
- Hoàn thiện tài liệu MinIO, concurrency và file lifecycle.

#### Kiên

- E2E Search/filter/sort/page và ba jobs.
- Chạy performance test với dữ liệu seed đủ lớn.
- Hoàn thiện tài liệu FTS index, Hangfire queues và xử lý job thất bại.

Đầu ra chung: release candidate trên từng nhánh, không còn lỗi Critical/High.

### Tuần 10 - 23/11 đến 29/11: Buffer, đóng gói và diễn tập bảo vệ

#### Hiếu

- Đóng băng scope, chỉ nhận bug fix.
- Kiểm tra toàn bộ branch links, commit ownership, CI và báo cáo tiến độ.
- Chuẩn bị kiến trúc diagram, demo order và phương án khôi phục khi dịch vụ lỗi.

#### Bo

- Sửa bug Auth/Profile còn lại.
- Chuẩn bị demo security: token rotation, RBAC, lockout và Google validation.
- Chốt release tag của nhánh Auth.

#### Trường

- Sửa bug Recipe/File còn lại.
- Chuẩn bị demo concurrency conflict, MinIO upload và Recipe lifecycle.
- Chốt release tag của nhánh Recipe/File.

#### Kiên

- Sửa bug Search/Job còn lại.
- Chuẩn bị demo FTS tiếng Việt, GIN index và retry/idempotency của job.
- Chốt release tag của nhánh Search/Job.

Đầu ra chung: bốn nhánh có release tag, CI xanh, demo rehearsal hoàn tất và tài liệu nộp sẵn sàng.

## 7. Nhịp quản lý mỗi tuần

### Đầu tuần

- Mỗi người chọn tối đa 3 mục tiêu chính.
- Ghi rõ endpoint/file/test dự kiến và dependency cần từ người khác.
- Không bắt đầu công việc phụ thuộc khi contract chưa được chốt.

### Giữa tuần

- Push ít nhất một commit chạy được.
- Nhóm trưởng review contract, migration và rủi ro bảo mật.
- Nếu trễ, cắt scope phụ trước; không bỏ test của business rule chính.

### Cuối tuần

- CI phải xanh.
- Demo phần hoàn thành bằng Scalar hoặc giao diện.
- Cập nhật ba mục: đã hoàn thành, chưa hoàn thành, blocker.
- Gắn tag hoặc ghi commit SHA làm mốc tuần để giáo viên có thể đối chiếu.

## 8. Đề xuất quản lý trên GitHub

- Tạo Milestone `Week 01` đến `Week 10`.
- Mỗi đầu việc có một GitHub Issue, gán đúng thành viên, FR code và tuần.
- Dùng nhãn: `member:hieu`, `member:bo`, `member:truong`, `member:kien`, `backend`, `frontend`, `database`, `security`, `test`, `blocked`.
- Mỗi issue phải có acceptance criteria và liên kết commit hoàn thành.
- Dùng Draft Pull Request để review nhánh thành viên nhưng không merge vào `main` trong giai đoạn chấm riêng.
- Tạo một GitHub Project board với các cột `Backlog`, `This Week`, `In Progress`, `Review`, `Done`, `Blocked`.

## 9. Rủi ro cần theo dõi

- Auth: xác minh Google token sai hoặc dùng secret mặc định là rủi ro Critical; phải xử lý trong Tuần 1.
- Migration: Identity, Category, Recipe và FTS cùng sửa `ApplicationDbContextModelSnapshot`; không được merge snapshot thủ công.
- Kiên phụ thuộc schema Recipe; phải nhận mốc schema ổn định thay vì tự tạo Recipe model khác.
- Trường phụ thuộc role/current-user contract của Bo nhưng có thể tiếp tục bằng scalar `AuthorId` cho đến khi contract ổn định.
- Frontend package/config dễ xung đột giữa bốn nhánh; shared provider/API client nên được Hiếu chốt sớm.
- Các PDF dùng một số ví dụ khác quyết định dự án; không sao chép nguyên mẫu `RowVersion`, refresh token JSON body hoặc Google OAuth flow nếu trái SRS/ADR.
- Không chạy theo số lượng commit. Mỗi tuần ưu tiên một luồng chạy end-to-end và có test.

## 10. Tiêu chí kết thúc đồ án

- Tất cả FR được gắn với endpoint/UI/test và người chịu trách nhiệm.
- Không còn lỗi Critical/High trong security review và dependency audit.
- Backend, frontend, Compose và migration chạy từ máy sạch theo README.
- Năm luồng E2E quan trọng pass: register/login, Category Admin, create/publish Recipe, upload/thumbnail, search.
- Health, logging và tracing đủ để chẩn đoán lỗi demo.
- Mỗi thành viên có lịch sử commit rõ ràng trên nhánh cá nhân và có thể giải thích thiết kế của phần mình.
