# Báo cáo tích hợp `develop` và đối chiếu SRS

Ngày kiểm tra: 23/09/2026  
Nhánh đích: `develop`  
Baseline: `2a18afd`  
SRS đối chiếu: `docs/SRS_Culinary_Blog_v1.0.0.md` (nội dung đã có CR-001/SRS 1.0.1)

## 1. Phạm vi đã gộp

- Auth của Bo: merge commit `663c1e9`, giữ nguyên 5 commit tác giả.
- Recipe/File của Trường: merge commit `afeccd1`, giữ nguyên 20 commit tác giả.
- Search/Jobs của Kiên: merge commit `c5b5ccb`, giữ nguyên 3 commit tác giả.
- `ApplicationDbContext` đã hợp nhất Identity và Recipe.
- `ApplicationDbContextModelSnapshot` được EF Core tái tạo từ model cuối, không ghép thủ công hai snapshot.
- Không squash, không đổi lịch sử và không xóa nhánh thành viên.

## 2. Kết quả kiểm tra kỹ thuật

- Backend Release: đạt, 0 warning và 0 error.
- Backend tests: đạt 16/16 gồm 11 Application, 3 Architecture và 2 Integration tests.
- Frontend ESLint: đạt.
- Frontend production build: đạt; hiện chỉ sinh route `/` và `/_not-found`.
- `docker compose config`: hợp lệ.
- EF Core idempotent migration script: sinh thành công theo thứ tự Initial → Identity → Recipe.
- EF Core pending model changes: không có.
- NuGet vulnerable packages: không phát hiện.
- npm production vulnerabilities: không phát hiện.
- Chưa chạy được migration/Compose trên container thật vì Docker Desktop daemon không hoạt động tại thời điểm kiểm tra.

Kết luận kỹ thuật: mã đã gộp compile được và lịch sử Git hợp lệ, nhưng chưa đạt điều kiện feature-complete hoặc release candidate.

## 3. Lỗi Critical — phải sửa trước khi cho phép dùng Auth

### INT-001 — Google ID token không được xác minh mật mã

- SRS: FR-AUTH-003 bước 3 và ADR-0003 yêu cầu xác minh signature, issuer, audience và expiry.
- Hiện trạng: `IdentityService.FindOrCreateGoogleUserAsync` chỉ gọi `ReadJwtToken` rồi tin trực tiếp các claim email/name/picture.
- Tác động: người dùng có thể tự tạo JWT giả chứa email của nạn nhân và chiếm/liên kết tài khoản.
- Vị trí: `src/CulinaryBlog.Infrastructure/Identity/IdentityService.cs`, quanh dòng 126–145.
- Phụ trách: Bo.
- Cách sửa: dùng Google token validator/JWKS chính thức, kiểm tra issuer, audience, expiry, email verification và liên kết bằng Google `sub`; thêm test token giả, sai audience, sai issuer và hết hạn.

### INT-002 — Có JWT signing key và tài khoản Admin mặc định dùng được

- SRS: NFR-SEC-007 cấm commit secret; JWT phải từ secret/environment.
- Hiện trạng: `JwtService` fallback sang key cố định; `RoleSeeder` fallback sang `admin@culinaryblog.local` / `Admin@123456`.
- Tác động: có thể tự ký access token hợp lệ; nếu seeder được gọi, thông tin Admin mặc định có thể bị khai thác.
- Vị trí: `JwtService.cs` dòng 13 và 24–27; `RoleSeeder.cs` dòng 37–38.
- Phụ trách: Bo.
- Cách sửa: fail fast khi thiếu cấu hình; Admin chỉ được seed khi có email/password từ secret hợp lệ và cờ cấu hình rõ ràng; không lưu giá trị dùng được trong source.

## 4. Lỗi High — chặn luồng nghiệp vụ theo SRS

### INT-003 — Auth chưa được nối vào HTTP pipeline và không có endpoint

- SRS: FR-AUTH-001 đến FR-AUTH-007 và REST API `/api/v1/auth/*`.
- Hiện trạng: chưa có `AddAuthentication`, JWT bearer configuration, authorization policies, `UseAuthentication`, `UseAuthorization` hoặc Auth endpoints. API chỉ map `GET /api/v1/`.
- Tác động: toàn bộ Auth/Profile/RBAC không thể gọi qua HTTP; `ICurrentUser` không thể nhận authenticated principal.
- Vị trí: `src/CulinaryBlog.API/Program.cs`, `Endpoints/EndpointRouteBuilderExtensions.cs`.
- Phụ trách: Bo.

### INT-004 — Chưa có token lifecycle theo SRS

- SRS: FR-AUTH-001/002/004/005 và NFR-SEC-002.
- Hiện trạng: có entity/repository và hàm sinh token nhưng chưa có command/handler cho register, login, refresh rotation, reuse detection, logout hoặc revoke family. Raw refresh token chưa được đặt trong cookie `HttpOnly; Secure; SameSite=Strict`.
- Tác động: chưa có phiên đăng nhập hoàn chỉnh và chưa chống refresh-token reuse.
- Phụ trách: Bo.

### INT-005 — Exception HTTP mapping sai

- SRS: Conflict → 409; Unauthorized → 401; Not Found → 404; validation nghiệp vụ → 422.
- Hiện trạng: `GlobalExceptionHandler` chỉ nhận `ApplicationValidationException` và `DomainException`. Các `ConflictException`, `UnauthorizedException`, `NotFoundException` mới đều rơi vào 500; validation hiện trả 400 thay vì 422.
- Tác động: API trả sai status/error contract, làm frontend và integration test sai theo SRS.
- Vị trí: `src/CulinaryBlog.API/ErrorHandling/GlobalExceptionHandler.cs` dòng 29–46.
- Phụ trách: Hiếu và Bo.

### INT-006 — Module Category hoàn toàn chưa tồn tại

- SRS: FR-CAT-001 đến FR-CAT-005; Category là FK bắt buộc của Recipe.
- Hiện trạng: không có Category entity, configuration, repository, migration, CQRS, endpoint hoặc frontend Admin.
- Tác động: 0/5 FR-CAT; `Recipe.CategoryId` chỉ là GUID không có FK nên có thể lưu category không tồn tại.
- Phụ trách: Hiếu.

### INT-007 — Recipe mới có domain/persistence, chưa có application/API/authorization

- SRS: FR-RCP-001 đến FR-RCP-010.
- Hiện trạng: chưa có DTO, command/query, validator, handler hay endpoint. Không có owner/Admin authorization hoặc ETag/concurrency handling trả `422 RECIPE_CONCURRENCY_CONFLICT`.
- Tác động: 0/10 FR-RCP có thể sử dụng end-to-end dù aggregate và repository đã tồn tại.
- Phụ trách: Trường; phụ thuộc RBAC của Bo và Category của Hiếu.

### INT-008 — Recipe schema không khớp mô hình dữ liệu SRS

- Thiếu `PublishedAt`, `SearchVector` và legacy `Instructions` theo mục 7.2.
- `Publish()` không ghi thời điểm xuất bản; `Update()` cho đổi slug sau publish, trái NFR-SEO-004.
- `AuthorId` chưa có FK đến `AspNetUsers`; `CategoryId` chưa có FK đến `Categories`.
- Nutrition thiếu Fiber/Sodium và tên/precision cột khác đặc tả.
- RecipeStep thiếu `Title`.
- RecipeIngredient bắt buộc Quantity/Unit trong khi SRS cho phép nullable; dùng `SortOrder` thay cho `OrderIndex`.
- RecipeImage chỉ có `Url`; thiếu `OriginalUrl`, `MediumUrl`, `ThumbnailUrl`, `OrderIndex`.
- Phụ trách: Trường phối hợp Hiếu, Bo và Kiên để khóa schema trước migration FTS.

### INT-009 — Search mới chỉ có contract, chưa có chức năng

- SRS: FR-SRCH-001 đến FR-SRCH-004.
- Hiện trạng: chưa có `IRecipeSearchReader` implementation, query/handler/endpoint, `unaccent`, `pg_trgm`, `tsvector`, trigger hoặc GIN index. `RecipeSearchOptions.Validate()` chưa bắt buộc `q` tối thiểu 2 ký tự.
- Tác động: 0/4 FR-SRCH hoạt động.
- Phụ trách: Kiên; phải chờ schema Recipe ổn định.

### INT-010 — File/MinIO chưa được hiện thực

- SRS: FR-FILE-001/002 và FR-RCP-008.
- Hiện trạng: Compose có MinIO nhưng backend không có SDK, `IFileStorageService`, upload/delete, bucket bootstrap, size/MIME/magic-byte validation hoặc endpoint.
- Tác động: không upload/xóa ảnh; thumbnail job không có nguồn dữ liệu/dịch vụ thật.
- Phụ trách: Trường.

### INT-011 — Background Jobs mới là skeleton, chưa chạy end-to-end

- SRS: FR-JOB-001/002/003 và Dashboard `/hangfire` chỉ dành cho Admin.
- Hiện trạng: chưa có implementation cho `IJobExecutionStore`, `IWelcomeEmailSender`, `IRecipeImageProcessor`, `ISitemapWriter`; job chưa đăng ký DI/trigger/retry; sitemap chưa đăng ký cron; không có Dashboard/Admin filter.
- `Hangfire:Enabled` không có trong cấu hình nên mặc định false; khi false cũng không có `IBackgroundJobScheduler`, producer tương lai có thể lỗi resolve DI.
- `TryClaimAsync` chưa định nghĩa lease/release; implementation ngây thơ có thể khóa vĩnh viễn job thất bại.
- Phụ trách: Kiên, phụ thuộc event/contract của Bo và Trường.

### INT-012 — Thiếu rate limit, CORS, HTTPS/HSTS và security headers

- SRS: NFR-SEC-003/004/005.
- Hiện trạng: không có rate limiting middleware, CORS policy, HTTPS redirection/HSTS hoặc CSP. Nginx chỉ listen HTTP port 80.
- Tác động: chưa đạt baseline bảo mật API/upload/production.
- Phụ trách: Hiếu phối hợp Bo.

## 5. Lỗi Medium — chất lượng, vận hành và hiệu năng

### INT-013 — Health check chưa đủ dependency

- SRS: FR-OBS-001 yêu cầu `/health` kiểm tra DB, Redis, MinIO và `/health/ready` kiểm tra DB + Redis.
- Hiện trạng: chỉ có database check và self/liveness; không có Redis/MinIO checks.
- Phụ trách: Hiếu.

### INT-014 — Logging/tracing/metrics mới ở mức nền

- SRS: FR-OBS-002/003.
- Hiện trạng: chưa có UserId enrichment, MediatR logging/performance behavior, EF Core instrumentation, custom recipe/category metrics hoặc rolling file sink. Correlation/request logging, Seq và HTTP telemetry đã có.
- Phụ trách: Hiếu.

### INT-015 — Repository Recipe có nguy cơ Cartesian explosion và trả entity tracking

- SRS: NFR-PERF-004.
- Hiện trạng: `RecipeRepository.QueryWithDetails()` Include đồng thời Steps, Ingredients và Images nhưng không `AsSplitQuery`, projection hoặc `AsNoTracking` cho query đọc.
- Tác động: dữ liệu lớn có thể nhân bản số dòng và tăng memory/latency.
- Phụ trách: Trường.

### INT-016 — Frontend mới là landing page

- SRS: mục 5.1, các trang Auth, Category Admin, Recipe, Search và profile.
- Hiện trạng: production build chỉ có `/`; chưa có API client, providers, forms, server-state layer, loading/error/empty states hoặc trang nghiệp vụ.
- Phụ trách: cả nhóm theo module; Hiếu giữ shared frontend foundation.

### INT-017 — Test chưa đạt NFR-MAINT-002

- SRS yêu cầu ≥80% Application coverage, mỗi endpoint có happy/error integration test và 5 E2E flows.
- Hiện trạng: chỉ 16 tests; chưa có Auth tests nghiệp vụ, endpoint tests cho module, PostgreSQL/MinIO/Hangfire integration tests, frontend tests hoặc E2E.
- Phụ trách: mỗi thành viên chịu test module của mình; Hiếu giữ quality gate.

### INT-018 — Cấu hình chạy local trực tiếp không khớp Compose mặc định

- `appsettings.Development.json` dùng password PostgreSQL `culinary-local-only`.
- `.env.example` và Compose mặc định dùng `change-me-for-local-development`.
- Tác động: chạy `dotnet run` kết nối PostgreSQL Compose mặc định sẽ thất bại nếu người dùng không override một phía.
- Phụ trách: Hiếu.

### INT-019 — Chưa kiểm chứng container/runtime thật

- Compose config hợp lệ nhưng Docker Desktop daemon đang tắt nên chưa chạy được migration trên PostgreSQL thật, health dependency, Hangfire storage hoặc smoke test Nginx.
- Đây là giới hạn môi trường kiểm tra, chưa kết luận là lỗi mã nguồn.
- Phụ trách kiểm tra lại: Hiếu khi Docker Desktop hoạt động.

## 6. Mức đáp ứng chức năng hiện tại

- FR-AUTH: infrastructure/entity/service một phần; 0/7 FR hoàn chỉnh qua HTTP.
- FR-CAT: 0/5.
- FR-RCP: aggregate/persistence một phần; 0/10 FR hoàn chỉnh qua HTTP.
- FR-SRCH: contract/pagination foundation; 0/4.
- FR-FILE: 0/2.
- FR-JOB: contract/skeleton/Hangfire adapter; 0/3 chạy end-to-end.
- FR-OBS: có foundation cho cả ba trụ cột nhưng chưa FR nào đáp ứng đủ acceptance criteria.

## 7. Thứ tự sửa đề xuất trước lần merge kế tiếp

1. Bo sửa INT-001/002; Hiếu + Bo sửa INT-003/005/012.
2. Hiếu hoàn thiện Category schema tối thiểu; Bo khóa `ICurrentUser`/policies.
3. Trường và nhóm trưởng chốt lại Recipe schema (INT-008) rồi tạo migration tích hợp mới.
4. Trường làm CQRS/API Recipe; Kiên làm query filter/pagination trên schema đã khóa.
5. Kiên chỉ tạo FTS migration sau bước 3; Trường chỉ phát thumbnail event sau khi File contract ổn định.
6. Sau đó mới nối Hangfire và ba side effect thật.
7. Mỗi bước phải bổ sung integration test trước khi đưa tiếp vào `develop`.

## 8. Điều kiện cho phép merge `develop` vào `main`

- Không còn INT-001/002 và không có lỗi Critical/High mới.
- Auth pipeline hoạt động và endpoint tối thiểu có test.
- Migration chạy được trên PostgreSQL trắng và database từ baseline.
- Docker Compose full stack healthy; `/health`, `/health/live`, `/health/ready` đúng SRS.
- Các module chưa hoàn chỉnh không được expose như đã hoàn thành.
- Backend/frontend CI xanh và không có secret dùng được trong Git.

