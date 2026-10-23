# Biên bản tích hợp tuần 8 (`integration/week-08`, issue #40)

> **Ghi chú cuối (23/10/2026):** nhánh `integration/week-08` và nhánh `wip/w08-recipe-fk` đã bị xóa. Toàn bộ nội dung tuần 8 nằm trên `feat/fr-cat-hieu` (cây thư mục giống hệt nhánh tích hợp, đã kiểm tra); các merge `-s ours` của nhánh Bo và Trường chỉ phục vụ nối lịch sử nên không mang sang. Migration khóa ngoại của Recipe chưa nằm trong repo: tạo lại bằng cách thêm `HasOne<Category>()` và `HasOne<ApplicationUser>()` với `OnDelete(Restrict)` vào `RecipeConfiguration`, rồi chạy `dotnet ef migrations add AddRecipeForeignKeys` (không sửa snapshot bằng tay).

Người thực hiện: Hiếu. Nhánh: `integration/week-08`, tạo từ `origin/main` tại `b05e746` (ngày 23/10/2026).
Quy ước: merge `--no-ff` theo thứ tự `main/Hiếu → Bo/Auth → Trường/Recipe-File → Kiên/Search-Job`; không hand-merge `ApplicationDbContextModelSnapshot`.

## 1. Kiểm kê nhánh thành viên

| Nhánh | Commit chưa có trong `main` | Kết quả merge | Ghi chú |
| --- | --- | --- | --- |
| `feat/fr-cat-hieu` (Hiếu) | 0 | Already up to date | Toàn bộ W03–W07 đã vào `main` qua PR #126, #134, #135, #141 |
| `feature/fr-auth-bo` (Bo) | 1 (merge PR #102) | Merge commit `03201bd`, cây không đổi so với `main` | Công việc thật của Bo vào `main` qua PR từ fork (#130, #136, #142) |
| `feature/fr-rcp-file-truong` (Trường) | 4 (docs và CI từ 23–30/09) | Merge commit `f62d0b6` bằng `-s ours` (chỉ nối lịch sử) | Xem 1.1 |
| `feature/fr-srch-job-kien` (Kiên) | 0 | Already up to date | Công việc vào `main` qua PR #127, #133, #140, #145 |

### 1.1 Vì sao nhánh của Trường dùng `-s ours`

Nhánh trên repo chính đã lệch khỏi `main` 165 commit. Merge thường xung đột ở `.github/workflows/ci.yml`, `docs/WEEKLY_TEAM_PLAN.md`, `ApplicationDbContext.cs` và `ApplicationDbContextModelSnapshot.cs`. Hai commit riêng của nhánh đã được kiểm tra là đã có trong `main` hoặc đã bị thay bằng bản mới hơn:

- `86ce83c` (service PostgreSQL trong CI): mọi dòng đều có trong `main`, trừ một dòng `POSTGRES_PASSWORD` cũ.
- `4fd82e9` (bản đầu của `WEEKLY_TEAM_PLAN.md`): `main` có bản mới hơn; chỉ khác 4 dòng quy tắc cũ đã được viết lại.

Merge `-s ours` giữ nguyên lịch sử tác giả mà không đưa nội dung cũ vào. Rủi ro nếu sai: một dòng cấu hình CI cũ không được mang sang.

## 2. Build và test sau hợp nhất

| Kiểm tra | Kết quả |
| --- | --- |
| `dotnet build CulinaryBlog.sln -c Release --warnaserror` | 0 warning, 0 error |
| Architecture tests | 3/3 |
| Application tests | 152/152 (165/165 sau mục 9) |
| Integration tests (PostgreSQL của Compose) | 182 đạt, 2 bỏ qua, 0 lỗi |
| Frontend `npm run typecheck`, `npm run lint` | sạch |
| Frontend `npm test` | 88/88 |
| `dotnet format --verify-no-changes` | Cục bộ trên Windows báo `ENDOFLINE` hàng loạt (CRLF trong working copy, kể cả file không sửa); chỉ có một cảnh báo `WHITESPACE` trên `ObservabilityExtensions.cs`, biến mất khi file ở LF. Chỉ CI trên Linux có giá trị quyết định |

## 3. Migration và snapshot

`dotnet ef migrations has-pending-model-changes`: *No changes have been made to the model since the last migration.* Vì vậy không có migration hợp nhất mới và snapshot không bị sửa. Chuỗi hiện có 8 migration, mới nhất là `20261014143221_AddJobExecutionStore`.

`scripts/check-migrations.ps1` chạy trên database dùng một lần trong Compose:

| Kịch bản | Kết quả |
| --- | --- |
| Model khớp snapshot | PASS |
| Database trắng: áp dụng cả 8 migration, không còn Pending | PASS |
| Trạng thái tuần trước (`AddRecipeImageVariants`) có dữ liệu → nâng lên mới nhất, dữ liệu còn nguyên | PASS |
| Schema Category cũ nhất (`AddCategoryFoundation`) có dữ liệu → nâng lên mới nhất | PASS |
| Migration mới nhất rollback rồi áp dụng lại | PASS |

## 4. Compose với schema tổng hợp và dữ liệu mới

Chạy project Compose riêng `culinary-w08` (volume và cổng riêng) để không xóa dữ liệu local, rồi `down -v` chỉ trên project này.

- Trước khi sửa: volume trắng khởi động xong, `/health/ready` trả 200 nhưng database không có bảng (xem INT-001).
- Sau khi sửa: 8 dòng `__EFMigrationsHistory`, 3 danh mục seed, đăng nhập Admin 200, `/health/ready` 200 với `database`, `redis`, `minio`, `hangfire` đều Healthy.

## 5. Smoke test liên module

`node scripts/smoke-test.mjs` (qua Nginx, API chạy Hangfire): **15/15 bước đạt**.

| Bước | Module |
| --- | --- |
| Readiness có database, redis, minio, hangfire | Hiếu/ops |
| Admin đăng nhập có role Admin | Bo |
| Author đăng ký, đăng nhập, `/auth/me` | Bo |
| Email chào mừng xuất hiện ở MailHog (sự kiện Auth → job) | Kiên/Bo |
| Admin tạo Category | Hiếu |
| Author tạo Recipe, thêm bước và nguyên liệu, publish | Trường |
| Upload ảnh và job thumbnail điền `thumbnailUrl` | Trường/Kiên |
| Search tìm thấy có dấu và không dấu | Kiên |
| Category detail liệt kê recipe đã publish | Hiếu/Trường |
| `sitemap.xml` có recipe đã publish | Kiên |
| Author khác sửa recipe → 403 | Bo/Trường |
| Chủ recipe soft delete → 204 | Trường |
| Recipe đã xóa không còn trong search, Category detail; GET trực tiếp 404 | Kiên/Trường |
| `sitemap.xml` không còn recipe đã xóa | Kiên |
| Dọn Category của smoke test | Hiếu |

Đối chứng: tắt Hangfire (mặc định cũ của Compose) thì smoke test còn 10/15; năm bước liên quan job báo FAIL đúng như mong đợi. Smoke test dùng được để bắt lỗi.

## 6. Lỗi và việc cần theo dõi, theo người phụ trách

| Mã | Phụ trách | Mô tả | Trạng thái |
| --- | --- | --- | --- |
| INT-001 | Hiếu | Compose trên volume trắng không áp dụng migration. API chạy với `/health/ready` 200 nhưng login và categories lỗi (`42P01`) | Đã sửa (`e19738c`): cờ `Database:ApplyMigrationsOnStartup`, Compose bật; mặc định tắt. Có `StartupMigrationTests` (3 test, đỏ trước, xanh sau) |
| INT-002 | Hiếu, Kiên | Compose và `.env.example` mặc định `HANGFIRE_ENABLED=false`, nên email chào mừng, thumbnail, sitemap không bao giờ chạy trong stack; sự kiện bị bỏ, không phát lại được | Đã đổi mặc định sang `true` (`dd4033c`, `0f7f9f3`). Kiên xác nhận lại hướng này |
| INT-003 | Kiên | Sitemap chỉ chạy lúc 02:00 UTC hoặc qua Dashboard Hangfire; chưa có reconciliation job | Cách chạy tay đã kiểm chứng: `POST /hangfire/recurring/trigger` với `jobs[]=sitemap-daily` và Bearer Admin trả 204, sitemap cập nhật trong vài giây. Reconciliation vẫn thuộc việc tuần 8 của Kiên |
| INT-004 | Trường, Bo, Kiên | Nhánh trên repo chính cách `main` 151–180 commit; công việc thật đi qua PR từ fork. Merge nhánh cũ gây xung đột | Đề nghị merge `origin/main` về nhánh cá nhân đầu tuần (đúng quy tắc thứ Hai) |
| INT-005 | Hiếu | `/health/ready` chỉ kiểm tra kết nối nên vẫn 200 khi chưa có schema hoặc còn migration chưa áp dụng | Chưa làm, đề xuất mở issue: `DatabaseHealthCheck` kiểm tra `GetPendingMigrationsAsync` |
| INT-006 | Hiếu | `Minio__PublicEndpoint` trong Compose mặc định `http://localhost:9000`; nếu đổi cổng MinIO phải đặt `MINIO_PUBLIC_ENDPOINT` khớp, nếu không URL ảnh sai cổng | Cấu hình, đã ghi nhận; không đổi mã |

## 7. Bàn giao

- Hoàn thành: nhánh `integration/week-08` (cục bộ; 7 commit của tuần 8: 2 merge commit và 5 commit sửa/kiểm thử/tài liệu, cộng các commit lịch sử của nhánh Bo và Trường được nối vào), script kiểm tra migration, smoke test, sửa INT-001 và INT-002, biên bản này.
- Chưa làm: push `integration/week-08`, mở PR/merge vào `main`, link CI xanh (cần push trước); INT-005; reconciliation job (Kiên).
- Blocker: không có. Việc push và merge vào `main` chờ xác nhận của nhóm trưởng theo quy tắc "chỉ merge khi mọi cổng chất lượng đạt".

## 8. Cách chạy lại

```powershell
dotnet build CulinaryBlog.sln -c Release --warnaserror
powershell -NoProfile -File scripts/check-migrations.ps1
docker compose up -d --build
$env:ADMIN_PASSWORD = "<Admin:Password trong src/CulinaryBlog.API/appsettings.Development.json>"
node scripts/smoke-test.mjs
```

`BASE_URL`, `MAILHOG_URL`, `MINIO_CONTAINER` đổi được bằng biến môi trường khi dùng cổng khác (xem đầu `scripts/smoke-test.mjs`).

## 9. Đối chiếu SRS và các sửa lỗi sau đối chiếu (23/10/2026)

Đối chiếu code đang chạy với `SRS_Culinary_Blog_v1.0.0.md` phát hiện các lệch sau thuộc phần của Hiếu; đã sửa theo TDD (test đỏ trước), mỗi mục một commit:

| Lệch | Sửa | Kiểm chứng |
| --- | --- | --- |
| Slug tiếng Việt hỏng trong Docker: image Alpine chạy invariant globalization nên `Normalize` không bỏ dấu ("Bánh tráng" → `b-nh-tr-ng`) | `CategorySlug` ánh xạ tường minh các chữ cái tiếng Việt; project Application.Tests chạy ở chế độ invariant như container | 13 test mới (12 đỏ trước); chạy trong container: `banh-trang-dac-biet` |
| JSON sai cú pháp, UTF-8 lỗi, sai kiểu giá trị trả 500 (CR-001, Phụ lục A yêu cầu 400) | `GlobalExceptionHandler` map `BadHttpRequestException` → 400 `MALFORMED_REQUEST` (413 `PAYLOAD_TOO_LARGE` giữ nguyên mã) | 6 test mới, 5 đỏ trước; không lộ stack trace |
| Sau Nginx, rate limit theo IP thành một bucket dùng chung và `CreatedByIp` của refresh token là IP của Nginx | Cờ `ForwardedHeaders:TrustAllProxies` (mặc định tắt, Compose bật vì cổng API không publish) kèm `UseForwardedHeaders` | 2 test mới (1 đỏ trước); trong Compose `CreatedByIp` = IP client, giả mạo `X-Forwarded-For` bị bỏ qua |
| Body `/health*` dùng `checks[]`, SRS 8.7 yêu cầu `entries{}` | `HealthResponseWriter` đổi sang `entries`; cập nhật smoke test và runbook | `DegradedHealthTests` đổi sang đọc `entries` (đỏ trước) |

Chưa sửa vì ảnh hưởng nhiều module: khóa ngoại `Recipes.CategoryId → Categories` và `Recipes.AuthorId → AspNetUsers` (SRS chương 7). Đã tạo migration trên nhánh `wip/w08-recipe-fk` (commit `2b997b0`, không merge). Thêm khóa ngoại làm 41 integration test lỗi vì fixture của Trường (RecipeImageEndpoint 20, RecipeNestedResources 5, RecipeLifecycle 4, RecipeReadRepository 1, ApiSmoke 2), Kiên (RecipeSearchReader 5) và Hiếu (CategoryRepository 4) tạo recipe với `categoryId` và `authorId` bịa. Cần chủ các test cập nhật fixture rồi mới merge và phải xong trước khi khóa migration chain.

Kết quả sau các sửa: build 0 warning; Architecture 3/3, Application 165/165, Integration 190 đạt (2 bỏ qua); smoke test 15/15.
