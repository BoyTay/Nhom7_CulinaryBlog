# Kế hoạch sửa code theo CR-002

Ngày lập: 23/10/2026. Người lập: Hiếu. Căn cứ: `docs/SRS_CONTRADICTIONS_2026-10-23.md` (mã C-01..C-33), `docs/integration/W08_INTEGRATION_RECORD.md` (lỗi theo người phụ trách) và `docs/tree.md` (quyền sở hữu).

Phạm vi: **chỉ phần code** của từng thành viên. Việc sửa SRS lên 1.0.2 do Hiếu tự làm, nằm ngoài kế hoạch này. Những mâu thuẫn mà code đã đúng một cách đọc thì không có việc code.

## 1. Nguyên tắc

- Mỗi người chỉ sửa module của mình theo `docs/tree.md`. File dùng chung (`GlobalExceptionHandler`, `Program.cs`, `compose.yaml`) và snapshot đi qua Hiếu.
- Mỗi việc là một PR nhỏ vào `main`: test đỏ trước rồi xanh, build `--warnaserror` sạch, CI xanh mới merge.
- Không đưa mật khẩu hay token vào code hoặc test (lấy từ biến môi trường hoặc sinh ngẫu nhiên; GitGuardian đã báo lỗi này một lần).
- Thời lượng đề xuất: **5 ngày làm việc** (ngày 0 đến ngày 4), rồi vào tuần 9.

## 2. Quyết định cần chốt trước khi viết code

Chỉ liệt kê những quyết định làm thay đổi code. Mỗi người cần biết cách đọc đã chốt trước khi làm. Cột "Mã" tham chiếu `SRS_CONTRADICTIONS`.

| # | Quyết định | Đề xuất | Mã | Ai sửa code |
| --- | --- | --- | --- | --- |
| D1 | Tài khoản bị khóa tạm hoặc vô hiệu | Lockout 423; `IsActive=false` → 403 `AUTH_ACCOUNT_DISABLED` (đăng nhập, refresh, Google) | C-03 | Bo |
| D2 | Rate limiting | ASP.NET Core theo IP thật; auth 10/phút, chung 100/phút, upload 5/phút; 429 có `type: RATE_LIMIT_EXCEEDED` | C-28, Phụ lục B | Bo (chính sách), Trường (gắn vào upload) |
| D3 | Recipe đọc công khai | Theo slug (`GET /recipes/{slug}`); ghi theo id; Draft và Archived của người khác trả 404 | C-13, C-16 | Trường, Kiên (sitemap) |
| D4 | Tạo recipe | Slug tự sinh từ title; title 5–200; category không tồn tại → 422; slug trùng → 409 `RECIPE_SLUG_EXISTS` | FR-RCP-003, C-13 | Trường |
| D5 | Danh sách recipe | `PagedResult`; `sort=-field`; mặc định 12, tối đa 50; sai tham số → 422; lọc `categoryId`, `difficulty`, `maxCookTime` | C-14, C-15 | Trường |
| D6 | Tìm kiếm | `q` rỗng hoặc dưới 2 ký tự → 422 `VALIDATION_ERROR` | FR-SRCH-001 | Kiên |
| D7 | Ảnh | Magic bytes đủ cho WebP và AVIF; giữ `PATCH .../primary` | C-22, C-25 | Trường |
| D8 | Readiness | `/health/ready` chỉ phụ thuộc database (và Hangfire khi bật); `/health` liệt kê tất cả | C-27 | Hiếu |
| D9 | Trang frontend | Thêm `/categories` và `/categories/[slug]`; trang chi tiết recipe chuyển sang `[slug]` | C-13, mục 5.1 SRS | Hiếu (hai trang danh mục), Trường (`/recipes/[slug]`) |
| D10 | Khóa ngoại Recipe | Bật `CategoryId → Categories` và `AuthorId → AspNetUsers` (`RESTRICT`) | Chương 7 | Trường và Kiên (fixture), Hiếu (migration) |

Các quyết định còn lại trong `SRS_CONTRADICTIONS` (400 hay 422, cookie hay body, `displayName`, độ khó, cache, v.v.) chỉ ảnh hưởng tài liệu nên không có việc code.

## 3. Việc từng người

Giờ chỉ để xếp thứ tự. **P0** = xong trong đợt này; **P1** = xếp vào tuần 9.

### 3.1 Hiếu (nền tảng, tích hợp)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Phụ thuộc |
| --- | --- | --- | --- | --- | --- |
| H-0 | Tách bộ sinh slug thành `Common` (`SlugGenerator`), `CategorySlug` gọi lại | P0 | 1 | 13 test `CategorySlugTests` vẫn đạt; Trường dùng được `SlugGenerator` | Ngày 0 |
| H-1 | Thêm ánh xạ lỗi dùng chung trong `GlobalExceptionHandler`: 423 (cho B-2) và 409 `RECIPE_SLUG_EXISTS` từ vi phạm unique (cho T-3) trong một PR riêng | P0 | 2 | Test đơn vị cho hai ánh xạ; Bo và Trường chỉ việc ném exception tương ứng | Ngày 1 |
| H-2 | `DatabaseHealthCheck` báo lỗi khi còn migration chưa áp dụng (INT-005); tách tag readiness theo D8 | P0 | 3 | Test: thiếu schema → `/health/ready` 503 `database` lỗi; Redis hoặc MinIO tắt → ready 200, `/health` 503 | D8 |
| H-3 | Trang `/categories` (ISR 3600) và `/categories/[slug]` (ISR 600, phân trang) dùng `PagedResult` | P0 | 6 | Có metadata, trạng thái loading, lỗi, rỗng; typecheck, lint, test sạch; axe không vi phạm | T-5 |
| H-4 | Sinh migration `AddRecipeForeignKeys` từ `docs/W08_wip_recipe_fk.patch`, chạy `check-migrations.ps1`, chạy toàn bộ test và smoke test | P0 | 4 | 5 kịch bản migration đạt; không còn test lỗi vì khóa ngoại; smoke 15/15 | T-7, K-4 |
| H-5 | Cập nhật `scripts/smoke-test.mjs` theo hợp đồng mới (slug, phân trang) | P0 | 2 | Smoke 15/15 trên Compose | T-4, T-5 |
| H-6 | Nginx HTTPS (chứng chỉ tự ký cho dev), HSTS, CSP (tính cả JSON-LD) | P1 | 5 | `curl -k https://localhost` trả 200 kèm `Strict-Transport-Security`; web không lỗi CSP | Không |
| H-7 | Sink log file xoay vòng theo ngày; `CHANGELOG.md` | P1 | 3 | File log `logs/culinaryblog-YYYYMMDD.log`; `CHANGELOG.md` theo Keep a Changelog | Không |

### 3.2 Bo (Identity, Auth, bảo mật)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Vị trí code |
| --- | --- | --- | --- | --- | --- |
| B-1 | Kiểm tra `IsActive`: tài khoản bị vô hiệu → 403 `AUTH_ACCOUNT_DISABLED` ở đăng nhập, refresh và Google | P0 | 3 | Test: đặt `IsActive=false` rồi login, refresh, Google đều 403; user hoạt động không đổi. Lưu ý: access token đã cấp còn hiệu lực đến khi hết hạn | `LoginCommandHandler`, `RefreshTokenCommandHandler`, `GoogleLoginCommandHandler` |
| B-2 | Lockout trả 423 (kèm thời gian còn lại, `Retry-After`) thay cho 401 `ACCOUNT_LOCKED` | P0 | 3 | Test: sai 5 lần rồi nhập đúng → 423; hết thời gian khóa → 200 | `IdentityService`, `LoginCommandHandler` (ném exception theo H-1) |
| B-3 | Body 429 có `type: RATE_LIMIT_EXCEEDED`; chính sách chung 100/phút/IP (loại `/health`) và `upload-rate-limit` 5/phút/IP | P0 | 4 | Test với `ForwardedHeaders:TrustAllProxies=true` (mẫu `ForwardedHeadersTests`): IP A bị 429, IP B không; có `Retry-After` | `Security/AuthRateLimit.cs`, `Program.cs` |
| B-4 | Frontend đăng nhập: thông báo cho 403 (tài khoản bị khóa) và 423 (tạm khóa kèm thời gian chờ) | P0 | 2 | Test `node --test` cho ánh xạ lỗi; kiểm tra thủ công hai thông báo | `app/(auth)/login`, `lib/auth/auth-client.ts` |

Tổng khoảng 12 giờ. Thứ tự: B-1 trước (lỗ hổng), rồi B-2, B-3, B-4.

### 3.3 Trường (Recipe, File)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Vị trí code |
| --- | --- | --- | --- | --- | --- |
| T-1 | Slug tự sinh từ title bằng `SlugGenerator` (hậu tố `-2`, `-3` khi trùng); bỏ `Slug` khỏi `CreateRecipeRequest`; editor frontend không gửi slug | P0 | 5 | Test: "Phở bò" → `pho-bo`, lần hai → `pho-bo-2`; body không có slug vẫn 201 | `CreateRecipeCommandHandler`, `CreateRecipeRequest`, `recipe-editor.tsx` |
| T-2 | Validator: title 5–200 ký tự; `categoryId` không tồn tại → 422 (`errors.categoryId`) | P0 | 2 | Test: title 3 ký tự → 422; category bịa → 422 | `CreateRecipeCommandValidator`, handler |
| T-3 | Slug trùng khi cập nhật (hoặc race khi tạo) → 409 `RECIPE_SLUG_EXISTS` thay vì 500 | P0 | 2 | Test: PUT đổi sang slug trùng → 409 | `UpdateRecipeCommandHandler` (dùng ánh xạ của H-1) |
| T-4 | `GET /recipes/{slug}` công khai (Draft và Archived chỉ chủ hoặc Admin, người khác 404); route web `/recipes/[slug]`; giữ `GET /recipes/{id:guid}` cho dashboard | P0 | 8 | Test: slug đúng 200, slug sai 404, Draft người khác 404; trang web mở được bằng slug | `EndpointRouteBuilderExtensions`, query mới `GetRecipeBySlugQuery` (cạnh `GetRecipeDetailQuery`), `app/recipes/[slug]`, `lib/recipes/client.ts` |
| T-5 | `GET /recipes` theo D5: `page`, `pageSize`, `sort`, `categoryId`, `difficulty`, `maxCookTime`, quyền xem Guest/Author/Admin, trả `PagedResult<RecipeSummaryDto>` | P0 | 8 | Test: `pageSize=51` và `page=0` → 422; sort tăng giảm; Guest chỉ thấy Published; Author thêm Draft của mình; Admin thấy tất cả; `/recipes` đọc `PagedResult` | `GetRecipeListQuery`, handler, `lib/recipes/list.ts`, `app/recipes/page.tsx` |
| T-6 | Gắn `upload-rate-limit` vào endpoint upload ảnh; magic bytes cho WebP (`RIFF....WEBP`) và AVIF (`ftyp`) | P1 | 3 | Test với tệp mẫu thật cho 4 định dạng; MIME sai → 400 `FILE_MIME_INVALID`; 6 upload/phút → 429 | `RecipeImageFileValidator` (mã `FILE_SIGNATURE_INVALID`), endpoint upload trong `EndpointRouteBuilderExtensions` |
| T-7 | **Fixture:** tạo helper test `RecipeTestData` (seed một danh mục và một người dùng thật) và dùng cho 33 test | P0 | 6 | Không còn test nào dùng `categoryId` hoặc `authorId` bịa; build và test xanh khi **chưa** bật khóa ngoại; gửi helper cho Kiên cuối ngày 1 | `RecipeImageEndpointTests` (20), `RecipeNestedResourcesEndpointTests` (5), `RecipeLifecycleEndpointTests` (4), `ApiSmokeTests` (2), `RecipeReadRepositoryTests` (1) |

Tổng khoảng 34 giờ, nặng nhất. Nếu không kịp, chuyển T-6 sang tuần 9 và nhờ Hiếu cùng làm T-7.

### 3.4 Kiên (Search, Jobs)

| ID | Việc | Ưu tiên | Giờ | Tiêu chí hoàn thành | Vị trí code |
| --- | --- | --- | --- | --- | --- |
| K-1 | `q` rỗng hoặc dưới 2 ký tự → 422 `VALIDATION_ERROR` cùng định dạng với phần còn lại (hiện trả ValidationProblem mặc định) | P0 | 2 | Test: `q=`, `q=a` → 422 `type: VALIDATION_ERROR` kèm `errors.q`; `q=ph` → 200 | `RecipeSearchEndpoints`, `RecipeSearchOptions` |
| K-2 | Sitemap dùng URL `/recipes/{slug}` và `/categories/{slug}` | P0 | 2 | `SitemapWriterTests` cập nhật; sitemap thật chứa slug | `SitemapWriter`, `RecipeRepository.ListPublishedAsync` |
| K-3 | Cơ chế khôi phục sự kiện bị bỏ lỡ (welcome email, thumbnail, sitemap): reconciliation job hoặc lệnh retry thủ công đã kiểm chứng | P0 | 4 | Nếu làm job thì có test idempotent; nếu làm thủ công thì có lệnh `POST /hangfire/recurring/trigger` và thao tác Retry kèm bằng chứng chạy thật | `BackgroundJobs/` |
| K-4 | Sửa fixture 5 test `RecipeSearchReaderTests` dùng helper của Trường | P0 | 2 | Test xanh với khóa ngoại bật trên database thử | `RecipeSearchReaderTests` |

Tổng khoảng 10 giờ. K-2 chờ T-4; K-4 chờ T-7.

## 4. Lịch theo ngày

| Ngày | Hiếu | Bo | Trường | Kiên |
| --- | --- | --- | --- | --- |
| 0 | H-0, chốt D1..D10 | Họp | Họp | Họp |
| 1 | H-1 | B-1, bắt đầu B-2 | T-1, T-2, bắt đầu T-7 (gửi helper cuối ngày) | K-1 |
| 2 | H-2, bắt đầu H-3 | B-2, B-3 | T-3, bắt đầu T-4, tiếp T-7 | K-3, K-4 khi có helper |
| 3 | H-3, H-5 | B-4 | T-4, T-5 | K-2 (sau T-4) |
| 4 | H-4: sinh migration khóa ngoại, chạy lại toàn bộ test và smoke | Duyệt PR | Hoàn tất T-5 và T-7 | Hoàn tất K-2 |

Phụ thuộc chính: T-7 trước H-4; T-4 trước K-2 và H-5; T-5 trước H-3 và H-5; B-3 trước T-6; H-1 trước B-2 và T-3.

## 5. Rủi ro

| Rủi ro | Hậu quả | Cách giảm |
| --- | --- | --- |
| T-4 và T-5 đổi hợp đồng nên frontend đổi theo | Chạm nhiều file, dễ xung đột với tuần 9 | PR nhỏ; cập nhật `contracts.ts` trong cùng PR; Hiếu duyệt |
| Bật khóa ngoại làm test lỗi | Chặn merge | Sửa fixture trước (T-7, K-4), bật khóa ngoại ở bước cuối (H-4) |
| Trường quá tải (34 giờ) | Trễ cả chuỗi | Chuyển T-6 sang tuần 9; Hiếu hỗ trợ T-7 |
| B-2 và T-3 cùng đụng `GlobalExceptionHandler` | Xung đột merge | H-1 thêm cả hai ánh xạ trong một PR riêng ngày 1 |
| Quyết định D1..D10 đổi sau họp | Làm lại | Ghi kết quả họp ngay; thay đổi sau đó cần Hiếu duyệt |

## 6. Tiêu chí kết thúc đợt

- Mọi việc P0 ở mục 3 đã merge vào `main` với CI xanh.
- Build Release 0 warning; toàn bộ test xanh khi khóa ngoại đã bật; smoke test 15/15.
- Migration chain khóa với `AddRecipeForeignKeys` là migration cuối.
