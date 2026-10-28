# W08 Bo — Kiểm tra tích hợp hợp đồng Auth (#43)

- Issue: [#43](https://github.com/BoyTay/Nhom7_CulinaryBlog/issues/43)
- Ngày kiểm tra: 27–28/10/2026.
- Nhánh: `feature/fr-auth-bo-w08`, tạo từ `upstream/main` tại `ee8e63f` (đã có khóa ngoại `Recipes.AuthorId → AspNetUsers` của PR #153).
- Phạm vi: Auth/Identity/Security và frontend `(auth)`/session của Bo. Không sửa mã, test hay cấu hình của Recipe, Category, Search/Jobs hay Nginx.

## 1. Recipe dùng claim Author/Admin

Kiểm tra bằng `tests/CulinaryBlog.Integration.Tests/Auth/AuthRecipeClaimsTests.cs`. Khác các test Recipe dùng `TestAuthenticationHandler`, test này dùng access token thật do `/auth/register`, `/auth/login`, `/auth/refresh` cấp, Identity và PostgreSQL thật.

| Kiểm tra | Kết quả |
| --- | --- |
| Không có token gọi `POST /recipes` | 401 |
| Author tạo recipe | 201; `authorId` bằng claim `sub` của JWT và thỏa khóa ngoại sang `AspNetUsers` |
| Author khác xem bản nháp | 404 (không lộ bản nháp) |
| Author khác sửa | 403 `RECIPE_FORBIDDEN` |
| Admin seed sửa recipe của người khác | 200 |
| Chủ recipe sửa tiếp với ETag mới | 200 |
| Thêm role Admin trong database, dùng token cũ | 403: token là ảnh chụp role, tối đa 15 phút |
| Sau `/auth/refresh` | Token mới có Admin, sửa được (200); refresh đọc lại role từ database |
| Gỡ role Author rồi refresh | `POST /recipes` trả 403 |

Kết luận: không cần sửa phía Recipe. `ICurrentUser.UserId` lấy từ `sub` (map sang `NameIdentifier`), `IsInRole` dùng claim role chuẩn; policy `RequireAuthor` nhận Author hoặc Admin.

## 2. Welcome email và refresh cookie qua Nginx

`scripts/verify-auth-nginx.mjs` gọi thẳng cổng Nginx (mặc định `http://localhost`). Chạy ngày 27/10/2026 trên stack Compose đang chạy (API build từ `feature/fr-auth-bo`; phần cookie, refresh, Origin và sự kiện đăng ký giống `upstream/main`): **8/8 bước đạt**.

| Bước | Kết quả |
| --- | --- |
| Đăng ký qua Nginx | 201; cookie `rt` có `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`, không có `Domain` |
| `/auth/me` với access token | 200, role `Author` |
| Email chào mừng | Có trong MailHog (UserRegisteredEvent → job Hangfire) |
| Refresh bằng cookie | 200, cookie được xoay vòng |
| Refresh với `Origin` lạ | 403 |
| Logout | 204, cookie hết hạn |
| Refresh bằng cookie đã logout | 401 `REVOKED_REFRESH_TOKEN` |
| Dùng lại cookie đã xoay vòng | 401 `REVOKED_REFRESH_TOKEN` (phát hiện reuse) |

Ghi chú: cookie `Secure` chỉ được trình duyệt chấp nhận trên HTTP khi host là `localhost`; môi trường thật phải chạy HTTPS.

## 3. Lệch hợp đồng đã sửa (không đổi route, DTO hay mã trạng thái)

| Mã | Lệch | Sửa | Kiểm chứng |
| --- | --- | --- | --- |
| W08-A1 | Tài khoản tạo từ Google không phát `UserRegisteredEvent` nên không nhận email chào mừng như đăng ký bằng email | `IIdentityService.FindOrCreateGoogleUserAsync` trả `GoogleSignInResult(User, IsNewUser)`; `GoogleLoginCommandHandler` phát sự kiện đúng một lần khi tạo mới | `GoogleLoginCommandHandlerTests` (2 test) |
| W08-A2 | Khóa tài khoản trả `ACCOUNT_LOCKED` ở login/refresh nhưng `ACCOUNT_LOCKED_OUT` ở Google | Google dùng `ACCOUNT_LOCKED`; status 401 giữ nguyên | `AuthEndpointsTests.GoogleLoginWithLockedAccountReturnsUnauthorized` |
| W08-A3 | Frontend nhận diện khóa tài khoản bằng `problem.errorCode`/`detail`, API không gửi hai trường này; banner khóa không bao giờ hiện | `isAccountLocked` đọc `problem.type` (vẫn nhận mã cũ) cho form đăng nhập và nút Google | `tests/auth/session.test.ts` |
| W08-A4 | Frontend không làm mới access token 15 phút: sau 15 phút Profile và editor Recipe (đọc token từ `authStore`) bị 401; logout lúc đó cũng 401 nên cookie còn sống và lần tải trang sau tự đăng nhập lại | `AuthProvider` hẹn giờ refresh trước khi hết hạn 60 giây, refresh khi tab hiện lại; logout gặp 401 thì refresh một lần rồi gọi lại | `tests/auth/session.test.ts` |
| W08-A5 | Hai tab refresh cùng lúc gửi cùng một cookie, bị coi là reuse và mất mọi phiên | `refreshTokenApi` tuần tự hóa bằng Web Locks (`navigator.locks`) | Test "never sends two refresh requests at the same time" |
| W08-A6 | 403 của `AuthOriginFilter` có `type` là URL RFC, khác mọi lỗi khác dùng mã ứng dụng | `type = ORIGIN_NOT_ALLOWED`, status 403 giữ nguyên | `AuthHardeningTests.UntrustedOriginCannotRotateCookieOrWriteProfile` |

`/auth/logout` vẫn yêu cầu access token như hiện tại; việc cho phép logout chỉ với cookie (SRS FR-AUTH-005 A2, mục C-12 của `docs/SRS_CONTRADICTIONS_2026-10-23.md`) chờ CR-002.

## 4. Lỗi giao cho người phụ trách khác

| Mã | Phụ trách | Mô tả | Bằng chứng |
| --- | --- | --- | --- |
| W08-H1 | Hiếu | `deploy/nginx/nginx.conf` dùng `proxy_set_header Host $host`; `$host` bỏ số cổng. Nếu Nginx publish ở cổng khác 80, Origin `http://host:port` không khớp `Host` nên mọi POST tới Auth bị 403 | Gửi qua Nginx với `Host: localhost:8080`, `Origin: http://localhost:8080` → 403; cùng origin trên cổng 80 → không bị chặn. Đề xuất `$http_host` |
| W08-T1 | Trường | API Recipe tuần tự hóa enum dạng số (`status: 1`) trong khi frontend và contract dùng tên (`"Published"`), nên trang `/recipes/[id]` trả 404 cho mọi recipe | `GET /api/v1/recipes/{id}` trả `"status":1`; Search trả `"difficulty":"Medium"` |

## 5. Kiểm thử

| Kiểm tra | Kết quả |
| --- | --- |
| `dotnet build CulinaryBlog.sln -c Release -warnaserror` | 0 warning, 0 error |
| Architecture tests | 3/3 |
| Application tests | 167/167 (thêm 2) |
| Integration tests (PostgreSQL local) | 192 đạt, 2 bỏ qua (MinIO), 0 lỗi; thêm 2 test claim |
| Frontend `npm run typecheck`, `npm run lint` | sạch |
| Frontend `npm test` | 94/94 (thêm 6) |
| `node scripts/verify-auth-nginx.mjs` | 8/8 |

`RecipeImageEndpointTests.OnlyOwnerOrAdminCanUploadAndImageLifecyclePreservesSinglePrimary` (Trường) lỗi một lần khi chạy song song toàn bộ suite, đạt ở 3 lần chạy riêng và 2 lần chạy lại toàn bộ; ghi nhận là test chập chờn, không liên quan thay đổi Auth.

## 6. Cách chạy lại

```powershell
dotnet build CulinaryBlog.sln -c Release -warnaserror
dotnet test CulinaryBlog.sln
cd src/CulinaryBlog.Web; npm run typecheck; npm run lint; npm test; cd ../..
docker compose up -d --build
node scripts/verify-auth-nginx.mjs   # đợi 1 phút giữa hai lần chạy vì rate limit Auth
```
