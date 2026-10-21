# W07 Bo — Auth security and Identity migrations (#35)

- Issue: [#35](https://github.com/BoyTay/Nhom7_CulinaryBlog/issues/35)
- Ngày kiểm tra: 21/10/2026.
- Nhánh: `feature/fr-auth-bo`.
- Phạm vi: Auth/Identity/Security, các kiểm thử tương ứng và tài liệu bàn giao của Bo.

## Thay đổi bảo mật

| Hạng mục | Hành vi sau thay đổi |
| --- | --- |
| Refresh đồng thời | PostgreSQL transaction và advisory lock theo người dùng tuần tự hóa rotation/logout/reuse. Token được đọc lại sau khi giữ lock; hai request dùng cùng token chỉ tạo một successor. Request còn lại phát hiện reuse và thu hồi các refresh session của chủ tài khoản. |
| Lockout | Refresh kiểm tra trạng thái khóa bằng Identity, trả `401 ACCOUNT_LOCKED` và thu hồi các refresh session khi tài khoản đang khóa. |
| Origin | Auth kiểm tra Origin trước các thao tác ghi. Chấp nhận cùng origin hoặc origin trong CORS allow-list; từ chối origin lạ, `null`, origin có đường dẫn và nhiều giá trị. Request không có Origin vẫn hỗ trợ client ngoài trình duyệt. |
| Rate limit | Giữ giới hạn 10 request/phút theo địa chỉ kết nối; không lấy trực tiếp IP từ `X-Forwarded-For`. Response `429` có Problem Details và `Retry-After`. |
| Profile | Avatar chỉ nhận URL HTTP/HTTPS. Các trường hồ sơ vẫn được kiểm tra giới hạn độ dài; subject trong JWT quyết định người được cập nhật. |
| Logging | Seed admin không ghi email. Lỗi Identity chỉ đưa error code vào exception/log; không đưa mô tả có thể chứa username/email. Google validation không ghi exception có thể chứa token/claims, hoặc phản chiếu issuer không tin cậy vào exception. |
| OpenAPI | Auth group khai báo thêm response `403` và `429` cho các kiểm tra bảo mật. |

`Program.cs` chỉ thay wiring của Auth rate limiter bằng `AuthRateLimit.Configure`.
Không thay đổi DTO, interface dùng chung, tên role/policy hoặc migration/model snapshot.
Không có endpoint mới hoặc migration mới.

## Kiểm thử

Unit test Auth bao phủ rotation, reuse, thu hồi đúng chủ tài khoản, token không tồn tại/hết hạn,
user bị xóa, lockout, logout idempotent và validator profile. Integration test sử dụng
`UserManager`, `IdentityService`, JWT bearer, repository và PostgreSQL thật; các test
endpoint cũ dùng fake vẫn được giữ để kiểm tra mapping HTTP.

Các trường hợp integration được bổ sung:

- Khóa sau 5 lần sai mật khẩu, thời gian khóa 15 phút, reset failure count khi đăng nhập
  đúng, đăng nhập lại sau khi hết khóa và chặn refresh trong thời gian khóa.
- Rotation/reuse giữa nhiều phiên; không thu hồi phiên của người khác; hai refresh đồng
  thời chỉ có một request thành công. Refresh hết hạn không giữ database lock.
- Cookie phát hành/rotation/xóa giữ `HttpOnly`, `Secure`, `SameSite=Strict`,
  `Path=/api/v1/auth`, không có Domain; raw refresh token không nằm trong JSON response.
- Origin cùng nguồn/allow-list, Origin không hợp lệ, CORS preflight được phép/bị từ chối,
  chặn thay đổi dữ liệu khi Origin không được phép.
- `429` đúng định dạng, có thời gian thử lại, không vượt rate limit bằng cách đổi
  `X-Forwarded-For`; IP kết nối khác có quota riêng.
- Profile lưu xuống database, validation lỗi không ghi dữ liệu, request không xác thực
  bị từ chối, không sửa user khác/email/role qua các trường JSON bổ sung.
- JWT Author bị từ chối ở route Admin, JWT Admin được phép, token bị sửa bị từ chối.
- Log từ request Auth, seed admin thành công/thất bại và Google validation không chứa
  các credential/email/bio dùng làm dữ liệu kiểm thử.

## Kiểm tra migration

Mỗi fixture tạo schema `auth_security_<guid>` riêng trên PostgreSQL 16 và xóa schema đó
khi kết thúc. Extension dùng chung ở `public` được chuẩn bị trước khi áp dụng toàn bộ
migration. Không sử dụng `EnsureCreated` thay cho migration.

1. Database/schema sạch (`0`) → `AddIdentityAndRefreshTokens` → ghi/đọc Identity.
2. Baseline `20260920041340_InitialArchitecture` → Identity → rollback → áp dụng lại.
3. Identity gốc → migration mới nhất: giữ password hash, role, profile, failure count,
   refresh revocation và replacement hash.
4. Baseline tuần 6 `20261014143221_AddJobExecutionStore` → migration mới nhất:
   kiểm tra dữ liệu Identity tương tự và không còn pending migration/model changes.
5. Toàn bộ migration từ schema sạch: kiểm tra unique token hash và cascade delete
   user → refresh tokens/user roles.

Baseline tuần 6 được tái tạo bằng EF migration và dữ liệu Identity mẫu; đây không phải
bản backup database thực tế của tuần trước. Lệnh EF update trên database local sạch
cũng đã hoàn tất. Công cụ `dotnet-ef` hiện có thông báo phiên bản 10.0.4 cũ hơn runtime
EF 10.0.12; Release build vẫn không có warning/error. Manifest tool thuộc nền tảng dùng
chung nên không được sửa trong issue này.

## Lệnh xác minh

Cấu hình `ConnectionStrings__Postgres` trỏ tới database kiểm thử trước khi chạy.
Database user cần quyền tạo schema và extension; bộ test Hangfire hiện có còn cần quyền
tạo database. Chạy PostgreSQL local bằng:

```powershell
docker compose --env-file .env.example up -d postgres
dotnet restore CulinaryBlog.sln
dotnet build CulinaryBlog.sln -c Release --no-restore --warnaserror -m:1
dotnet test CulinaryBlog.sln -c Release --no-build --no-restore -m:1
dotnet list CulinaryBlog.sln package --vulnerable --include-transitive --no-restore
```

Audit frontend (thư mục `src/CulinaryBlog.Web`):

```powershell
npm audit --json
npm audit --omit=dev --json
```

| Kiểm tra | Kết quả |
| --- | --- |
| Release build với `--warnaserror` | PASS, 0 warning, 0 error |
| Application tests | PASS, 150/150; bao gồm 57 test Auth |
| Architecture tests | PASS, 3/3 |
| Integration tests | PASS, 176 passed; 2 test MinIO skip vì chưa bật `MINIO_INTEGRATION_TESTS` |
| Tổng backend | 329 passed, 0 failed, 2 skipped; 57 unit + 72 integration liên quan Auth |
| Formatting các file thay đổi | PASS, `dotnet format --no-restore --verify-no-changes --include ...` |
| NuGet audit, gồm transitive | Không có package bị báo lỗ hổng |
| npm audit, toàn bộ và production | 0 lỗ hổng |

TRX của lượt thành công cuối có prefix `TestResults/issue-35/verified-suite` (được gitignore);
không đưa credential hay test log
chứa dữ liệu thử nghiệm vào source control.

## Bàn giao tích hợp

- Khi chạy HTTPS qua reverse proxy, cấu hình `Cors:AllowedOrigins` chứa origin public
  của frontend. Địa chỉ kết nối phục vụ rate limit chỉ nên được cập nhật qua forwarded
  headers middleware với danh sách proxy tin cậy do owner nền tảng quản lý.
- Mã nguồn bàn giao trên nhánh `feature/fr-auth-bo`; commit có tiêu đề
  `fix(auth): harden security and verify Identity migrations (#35)`.
  Cần liên kết completion commit/PR và CI thành công trước khi đóng issue theo DoD;
  việc merge đi qua `integration/week-07` theo `docs/tree.md`.
