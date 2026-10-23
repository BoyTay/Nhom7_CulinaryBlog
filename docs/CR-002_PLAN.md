# Kế hoạch CR-002: xử lý mâu thuẫn trong SRS và lỗi lệch SRS

Ngày lập: 23/10/2026. Người lập: Hiếu. Căn cứ: `docs/SRS_CONTRADICTIONS_2026-10-23.md` (33 mâu thuẫn, mã C-01..C-33), biên bản `docs/integration/W08_INTEGRATION_RECORD.md` (lỗi theo người phụ trách) và `docs/tree.md` (quyền sở hữu).

## 1. Mục tiêu và nguyên tắc

- Chốt **một** cách đọc cho mỗi mâu thuẫn rồi sửa SRS lên 1.0.2 (CR-002). Ưu tiên cách đọc mà code đang theo, vì sửa chữ rẻ hơn sửa code.
- Chỉ sửa code ở những chỗ code lệch cách đọc đã chốt, và những chỗ ảnh hưởng hợp đồng API hoặc schema phải xong **trước khi sang tuần 9** (E2E, bảo mật).
- Mỗi người chỉ sửa module của mình theo `docs/tree.md`. Việc đụng file dùng chung (`GlobalExceptionHandler`, `Program.cs`, `compose.yaml`, snapshot) đi qua Hiếu.
- Mỗi việc là một PR nhỏ vào `main`, test đỏ trước rồi xanh, CI xanh mới merge.

Thời lượng đề xuất: **5 ngày làm việc** (ngày 0 đến ngày 4), sau đó vào tuần 9.

## 2. Quyết định cần chốt ở buổi họp ngày 0

Cả nhóm trả lời "đồng ý" hoặc đổi; Hiếu ghi vào CR-002. Cột "Ảnh hưởng code" cho biết ai phải sửa nếu đồng ý với đề xuất.

| # | Quyết định | Đề xuất | Mã | Ảnh hưởng code |
| --- | --- | --- | --- | --- |
| D1 | Lỗi validation và nghiệp vụ | 422 `VALIDATION_ERROR`; JSON sai cú pháp 400 `MALFORMED_REQUEST` | C-01, C-02 | Không (đã đúng); sửa SRS |
| D2 | Refresh token | Cookie `HttpOnly`, không có trong body | C-05 | Không; sửa SRS |
| D3 | Phản hồi đăng ký | 201 kèm access token và user (giống đăng nhập) | C-06 | Không; sửa SRS |
| D4 | Hồ sơ người dùng | `id, email, displayName, avatarUrl, bio, roles` | C-07 | Không; sửa SRS |
| D5 | Google | Chỉ nhận ID token; 401 khi token sai; thêm 502 vào Phụ lục A | C-04, C-08 | Không; sửa SRS |
| D6 | Tài khoản bị khóa tạm, vô hiệu | Lockout 423; `IsActive=false` 403 `AUTH_ACCOUNT_DISABLED` (đăng nhập, refresh, Google) | C-03 | **Bo** |
| D7 | Recipe | Đọc công khai theo slug; ghi theo id; chi tiết cho người không có quyền xem Draft là 404 | C-13, C-16 | **Trường**, Kiên |
| D8 | Phân trang | `PagedResult` (`items`, `totalCount`, `page`, `pageSize`, `totalPages`, `hasNextPage`, `hasPreviousPage`), `sort=-field`, mặc định 12, tối đa 50, sai tham số 422 | C-14, C-15 | **Trường**, Kiên |
| D9 | Độ khó | Giữ 3 mức Easy, Medium, Hard, lưu chuỗi | C-17 | Không; sửa SRS |
| D10 | `Instructions`, `Step.Title`, `stepNumber` từ client | Bỏ `Instructions` và `Title`; `stepNumber` luôn do server sinh | C-18, C-19 | Không; sửa SRS |
| D11 | Nguyên liệu | `Quantity` và `Unit` bắt buộc; cột `SortOrder` | C-20 | Không; sửa SRS |
| D12 | Dinh dưỡng | 4 trường: `Calories`, `ProteinGrams`, `CarbohydratesGrams`, `FatGrams` | C-21 | Không; sửa SRS |
| D13 | API ảnh | Giữ `PATCH .../primary`; phản hồi `RecipeImageDto`; kiểm tra magic bytes đủ cho WebP, AVIF | C-22, C-25 | **Trường** |
| D14 | Danh mục | Soft delete; có `imageUrl`, `orderIndex`; danh sách sắp theo tên | C-23, C-24 | Không; sửa SRS |
| D15 | Cache | Chỉ `IMemoryCache` cho danh mục (60 phút); recipe và tìm kiếm không cache; bỏ Redis, Output Cache, `CachingBehavior` khỏi thiết kế | C-26 | Không; sửa SRS |
| D16 | Readiness | `/health/ready` chỉ phụ thuộc database (và Hangfire khi bật); `/health` liệt kê tất cả | C-27 | **Hiếu** |
| D17 | Rate limiting | ASP.NET Core theo IP thật; auth 10/phút, chung 100/phút, upload 5/phút | C-28 | **Bo** (chính sách), Trường (gắn vào upload) |
| D18 | Kiến trúc | `ApplicationUser` thuộc Infrastructure; `LoggingBehavior` không ghi tham số | C-29, C-30 | Không; sửa SRS |
| D19 | Frontend | Giữ route `/login`, `/register`, `/admin/categories`; thêm `/categories` và `/categories/[slug]`; Recipe Draft dùng render động, Published dùng ISR; Node 22; bỏ Tailwind khỏi SRS | C-31..C-33 | **Hiếu** (hai trang mới) |
| D20 | Khóa ngoại Recipe | **Bật** `CategoryId → Categories` và `AuthorId → AspNetUsers` (`RESTRICT`) | (chương 7) | **Trường** và **Kiên** sửa fixture; **Hiếu** sinh migration |

## 3. Kế hoạch từng người

Ước lượng tính bằng giờ làm việc, chỉ để xếp thứ tự; "P0" là phải xong trong đợt này, "P1" xếp vào tuần 9.

### 3.1 Hiếu (nhóm trưởng: tài liệu, nền tảng, tích hợp)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Phụ thuộc |
| --- | --- | --- | --- | --- | --- |
| H-0 | Tách bộ sinh slug thành `Common` (`SlugGenerator`), `CategorySlug` gọi lại; giữ nguyên test slug hiện có | P0 | 1 | 13 test `CategorySlugTests` vẫn đạt; Trường dùng được `SlugGenerator` | Ngày 0 |
| H-1 | Họp chốt D1..D20 và ghi kết quả | P0 | 1 | Bảng quyết định có chữ "chốt" kèm người xác nhận | Không |
| H-2 | Viết CR-002 và sửa SRS lên 1.0.2 cho các mục chỉ cần sửa chữ: C-01, 02, 04..12, 14..16 (phần chữ), 17..24, 26..33 | P0 | 6 | Bảng lịch sử thay đổi có dòng 1.0.2; `SRS_CONTRADICTIONS` cập nhật cột "Đã xử lý"; không còn hai đoạn nói ngược nhau | H-1 |
| H-3 | Trang `/categories` (ISR 3600) và `/categories/[slug]` (ISR 600, phân trang) dùng `PagedResult` | P0 | 6 | Hai trang có metadata, trạng thái loading, lỗi, rỗng; typecheck, lint, test sạch; kiểm tra axe không vi phạm | T-5 (phân trang) |
| H-4 | `DatabaseHealthCheck` báo lỗi khi còn migration chưa áp dụng (INT-005); tách tag readiness theo D16 | P0 | 3 | Test: schema thiếu → `/health/ready` 503 `database` lỗi; Redis hoặc MinIO tắt → ready vẫn 200, `/health` 503 | D16 |
| H-5 | Sinh migration `AddRecipeForeignKeys` từ patch `docs/W08_wip_recipe_fk.patch`, chạy `check-migrations.ps1`, chạy lại toàn bộ test và smoke test | P0 | 4 | 5 kịch bản migration đạt; Integration không còn test lỗi vì FK; smoke 15/15 | T-7, K-4 |
| H-6 | Cập nhật `scripts/smoke-test.mjs` theo hợp đồng mới (slug, phân trang, `entries`) | P0 | 2 | Smoke 15/15 trên Compose | T-4, T-5 |
| H-7 | Nginx HTTPS (chứng chỉ tự ký cho dev), HSTS, CSP (tính cả JSON-LD) | P1 | 5 | `curl -k https://localhost` trả 200 với `Strict-Transport-Security`; web không lỗi CSP | Không |
| H-8 | Sink log file xoay vòng theo ngày; `CHANGELOG.md` | P1 | 3 | File log `logs/culinaryblog-YYYYMMDD.log`; `CHANGELOG.md` theo Keep a Changelog | Không |

Việc thông báo: gửi bảng D1..D20 trước buổi họp; sau họp gửi danh sách việc từng người (mục 3.2 đến 3.4).

### 3.2 Bo (Identity, Auth, bảo mật)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Vị trí code |
| --- | --- | --- | --- | --- | --- |
| B-1 | Kiểm tra `IsActive`: tài khoản bị vô hiệu → 403 `AUTH_ACCOUNT_DISABLED` ở đăng nhập, refresh và Google | P0 | 3 | Test: đặt `IsActive=false` rồi login, refresh, Google đều 403; user hoạt động không đổi; ghi chú: access token đã cấp còn hiệu lực đến khi hết hạn | `LoginCommandHandler`, `RefreshTokenCommandHandler`, `GoogleLoginCommandHandler` |
| B-2 | Lockout trả 423 (kèm thời gian còn lại, `Retry-After`) thay cho 401 `ACCOUNT_LOCKED` | P0 | 3 | Test: sai 5 lần rồi đăng nhập đúng → 423; sau thời gian khóa → 200; Hiếu duyệt thay đổi ở `GlobalExceptionHandler` (thêm ánh xạ 423) | `IdentityService`, `LoginCommandHandler`, `GlobalExceptionHandler` |
| B-3 | Body 429 có `type: RATE_LIMIT_EXCEEDED`; thêm chính sách chung 100/phút/IP (loại trừ `/health`) và chính sách `upload-rate-limit` 5/phút/IP | P0 | 4 | Test với `ForwardedHeaders:TrustAllProxies=true` (mẫu trong `ForwardedHeadersTests`): IP A bị 429, IP B không; `Retry-After` có mặt | `Security/AuthRateLimit.cs`, `Program.cs` |
| B-4 | Frontend đăng nhập: thông báo cho 403 (tài khoản bị khóa) và 423 (tạm khóa kèm thời gian chờ) | P0 | 2 | Test `node --test` cho ánh xạ lỗi; kiểm tra thủ công hai thông báo | `app/(auth)/login`, `lib/auth/auth-client.ts` |
| B-5 | Cập nhật tài liệu Auth (`docs/fr-auth-bo`) theo D2..D6 và soát lại các mục Auth trong SRS 1.0.2 do Hiếu sửa | P0 | 2 | Tài liệu không còn nhắc `fullName`, refresh trong body; Bo xác nhận trên PR của Hiếu | `docs/fr-auth-bo` |

Tổng khoảng 14 giờ. Thứ tự: B-1 trước (lỗ hổng), rồi B-2, B-3, B-4, B-5.

### 3.3 Trường (Recipe, File)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Vị trí code |
| --- | --- | --- | --- | --- | --- |
| T-1 | Slug recipe tự sinh từ title (dùng `SlugGenerator`, thêm hậu tố `-2`, `-3` khi trùng); bỏ `Slug` khỏi `CreateRecipeRequest`; frontend editor không gửi slug nữa | P0 | 5 | Test: "Phở bò" → `pho-bo`; tạo lần hai → `pho-bo-2`; body không có slug vẫn 201 | `CreateRecipeCommandHandler`, `CreateRecipeRequest`, `recipe-editor.tsx` |
| T-2 | Validator: title 5–200 ký tự; `categoryId` không tồn tại → 422 (`errors.categoryId`) | P0 | 2 | Test: title 3 ký tự → 422; category bịa → 422 | `CreateRecipeCommandValidator`, handler |
| T-3 | Slug trùng khi tạo (race) và khi cập nhật → 409 `RECIPE_SLUG_EXISTS` thay vì 500 | P0 | 2 | Test: PUT đổi slug trùng → 409; vi phạm unique `IX_Recipes_Slug` được ánh xạ | `UpdateRecipeCommandHandler`, `GlobalExceptionHandler` (Hiếu duyệt) |
| T-4 | `GET /recipes/{slug}` công khai (Draft và Archived chỉ chủ hoặc Admin, người khác 404); route web `/recipes/[slug]`; sitemap dùng slug | P0 | 8 | Test endpoint: slug đúng → 200, slug sai → 404, Draft người khác → 404; trang web mở được bằng slug; `GET /recipes/{id:guid}` giữ lại cho dashboard | `EndpointRouteBuilderExtensions`, query mới `GetRecipeBySlugQuery` (cạnh `GetRecipeDetailQuery`), `app/recipes/[slug]`, `lib/recipes/client.ts` |
| T-5 | `GET /recipes` theo D8: `page`, `pageSize`, `sort`, `categoryId`, `difficulty`, `maxCookTime`, quyền xem Guest/Author/Admin, trả `PagedResult<RecipeSummaryDto>` | P0 | 8 | Test: `pageSize=51` và `page=0` → 422; sort tăng giảm; Guest chỉ thấy Published; Author thêm Draft của mình; Admin thấy tất cả; trang `/recipes` đọc `PagedResult` | `GetRecipeListQuery`, handler, `lib/recipes/list.ts`, `app/recipes/page.tsx` |
| T-6 | Gắn `upload-rate-limit` vào endpoint upload ảnh; kiểm tra magic bytes cho WebP (`RIFF....WEBP`) và AVIF (`ftyp`) | P1 | 3 | Test với file mẫu thật cho 4 định dạng; MIME sai → 400 `FILE_MIME_INVALID`; gửi 6 upload/phút → 429 | endpoint upload ảnh trong `EndpointRouteBuilderExtensions`, nơi kiểm tra chữ ký tệp (`FILE_SIGNATURE_INVALID`) |
| T-7 | **Fixture:** tạo helper test `RecipeTestData` (seed một danh mục và một người dùng thật) và dùng cho 33 test | P0 | 6 | Không còn test nào dùng `categoryId` hoặc `authorId` bịa; build và test xanh khi **chưa** bật FK; gửi helper cho Kiên cuối ngày 1 | `RecipeImageEndpointTests` (20), `RecipeNestedResourcesEndpointTests` (5), `RecipeLifecycleEndpointTests` (4), `ApiSmokeTests` (2), `RecipeReadRepositoryTests` (1) và các test dùng `Recipe.Create` |

Tổng khoảng 34 giờ, nặng nhất. Nếu không kịp, chuyển T-6 sang tuần 9 và nhờ Hiếu cùng làm T-7.

### 3.4 Kiên (Search, Jobs)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Vị trí code |
| --- | --- | --- | --- | --- | --- |
| K-1 | `q` rỗng hoặc dưới 2 ký tự → 422 `VALIDATION_ERROR` cùng định dạng với phần còn lại (hiện trả ValidationProblem mặc định) | P0 | 2 | Test: `q=`, `q=a` → 422 `type: VALIDATION_ERROR` kèm `errors.q`; `q=ph` → 200 | `RecipeSearchEndpoints`, `RecipeSearchOptions` |
| K-2 | Sitemap dùng URL `/recipes/{slug}` và `/categories/{slug}` | P0 | 2 | `SitemapWriterTests` cập nhật; sitemap thật chứa slug | `SitemapWriter`, `RecipeRepository.ListPublishedAsync` |
| K-3 | Reconciliation hoặc hướng dẫn retry thủ công cho welcome email, thumbnail, sitemap bị bỏ lỡ | P0 | 4 | Tài liệu runbook từng bước (kèm lệnh `POST /hangfire/recurring/trigger` đã kiểm chứng); nếu làm job thì có test idempotent | `docs/fr-srch-job-kien`, `BackgroundJobs/` |
| K-4 | Sửa fixture 5 test `RecipeSearchReaderTests` dùng helper của Trường | P0 | 2 | Test xanh với FK bật trên database thử | `RecipeSearchReaderTests` |
| K-5 | Xác nhận mặc định Hangfire bật trong Compose (INT-002) và cập nhật `W05_HANGFIRE_RUNBOOK.md` | P0 | 1 | Runbook ghi `HANGFIRE_ENABLED=true` là mặc định | `docs/fr-srch-job-kien` |

Tổng khoảng 11 giờ. K-2 chờ T-4; K-4 chờ T-7.

## 4. Lịch theo ngày

| Ngày | Hiếu | Bo | Trường | Kiên |
| --- | --- | --- | --- | --- |
| 0 | H-0, họp H-1, bắt đầu H-2 | Họp | Họp | Họp |
| 1 | H-2 | B-1, bắt đầu B-2 | T-1, T-2, bắt đầu T-7 (gửi helper cuối ngày) | K-1, K-5 |
| 2 | H-4, bắt đầu H-3 | B-2, B-3 | T-3, bắt đầu T-4, tiếp T-7 | K-3, K-4 khi có helper |
| 3 | H-3, H-6 | B-4, B-5 | T-4, T-5 | K-2 (sau T-4) |
| 4 | H-5 (migration FK), chạy lại toàn bộ test và smoke, tổng kết | Duyệt PR | Hoàn tất T-7 và T-5 | Hoàn tất K-2 |

Phụ thuộc chính: T-7 phải xong trước H-5; T-4 xong trước K-2 và H-6; T-5 xong trước H-3 và H-6; B-3 xong trước T-6.

## 5. Quy trình phối hợp

1. Mỗi việc một issue (nhãn `cr-002` và `member:...`) và một PR vào `main`; PR nêu rõ mã việc và mã mâu thuẫn (C-xx).
2. Mỗi PR: test đỏ trước rồi xanh, `dotnet build -c Release --warnaserror`, `dotnet format --verify-no-changes` (trên Linux CI), `npm run typecheck`, `lint`, `test`, cập nhật OpenAPI summary khi đổi endpoint.
3. Hiếu duyệt PR đụng file dùng chung (`GlobalExceptionHandler`, `Program.cs`, `compose.yaml`) và chuẩn bị snapshot; không ai sửa snapshot bằng tay.
4. Không tự đưa mật khẩu hay token vào code hoặc test: lấy từ biến môi trường hoặc sinh ngẫu nhiên (GitGuardian đã báo lỗi này một lần).
5. Cuối ngày 4: Hiếu chạy lại toàn bộ test, smoke test và `check-migrations.ps1`; chỉ khi xanh mới khóa migration chain và sang tuần 9.

## 6. Rủi ro

| Rủi ro | Hậu quả | Cách giảm |
| --- | --- | --- |
| T-4 và T-5 đổi hợp đồng nên frontend của Trường và Kiên đổi theo | Chạm nhiều file frontend, dễ xung đột với tuần 9 | Làm theo từng PR nhỏ; cập nhật `contracts.ts` trong cùng PR; Hiếu duyệt |
| Bật khóa ngoại làm test lỗi | Chặn merge | Sửa fixture trước (T-7, K-4), bật FK ở bước cuối (H-5) |
| Trường quá tải (34 giờ) | Trễ cả chuỗi | Chuyển T-6 sang tuần 9; Hiếu hỗ trợ T-7 |
| Hai người đổi cùng `GlobalExceptionHandler` (B-2, T-3) | Xung đột merge | Hiếu thêm ánh xạ 423 và 409 slug trong một PR riêng ngày 1 |
| Quyết định D1..D20 bị đổi sau họp | Làm lại | Ghi quyết định vào CR-002 ngay trong cuộc họp; thay đổi sau đó cần Hiếu duyệt |

## 7. Tiêu chí kết thúc đợt

- SRS 1.0.2 không còn mâu thuẫn trong danh sách C-01..C-33 (mỗi mục được đánh dấu "đã sửa SRS" hoặc "đã sửa code").
- Không còn lỗi lệch SRS thuộc nhóm P0 của bốn người (B-1..B-5, T-1..T-5, T-7, K-1..K-5, H-0..H-6).
- Build Release 0 warning; toàn bộ test xanh khi khóa ngoại đã bật; smoke test 15/15; CI xanh trên mọi PR.
- Migration chain khóa với `AddRecipeForeignKeys` là migration cuối.
