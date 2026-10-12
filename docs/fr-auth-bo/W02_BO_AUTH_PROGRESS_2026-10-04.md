# Báo cáo tiến độ W02 – Bơ – Register, Login, Rate Limiting & Secure Refresh Cookie (Issue #15)

**Ngày cập nhật:** 04/10/2026  
**Thành viên thực hiện:** Phùng Nguyễn Hoài Bơ (`HUBERTPHUNG` / Bo)  
**Mã số sinh viên:** 2312585  
**Nhóm:** 07 – Đồ án Culinary Blog  
**Nhánh làm việc (Branch):** [`feature/fr-auth-bo`](https://github.com/HubertPhung/Nhom7_CulinaryBlog/tree/feature/fr-auth-bo)  
**Issue:** [#15 - W02 Bo — Register, login, access 15 phút, refresh-token hash/cookie, rate limit và integration tests](https://github.com/BoyTay/Nhom7_CulinaryBlog/issues/15)  
**Pull Request:** [#111](https://github.com/BoyTay/Nhom7_CulinaryBlog/pull/111) (Merged vào `main`)  
**Commit tiêu biểu:** `d0f04dd feat(auth): implement Register/Login, rate limiting and secure refresh cookie (#15)`  
**Milestone:** Week 02 (28/09/2026 – 04/10/2026)  

---

## 1. Mục tiêu và Phạm vi công việc (Scope of Work)

Theo phân công trong kế hoạch nhóm W02 (`docs/KeHoach.md` và `docs/WEEKLY_TEAM_PLAN.md`), mục tiêu chính của Issue #15 là xây dựng hoàn chỉnh luồng xác thực người dùng cơ bản của hệ thống CulinaryBlog, bao gồm đăng ký, đăng nhập nội bộ, bảo vệ chống brute-force và thiết lập kiến trúc Cookie Same-Origin bảo mật:

1. **Cài đặt ca sử dụng Đăng ký tài khoản (`FR-AUTH-001` - User Registration):**
   * Xây dựng `RegisterCommand`, `RegisterCommandHandler`, `RegisterCommandValidator`.
   * Kiểm tra validation chặt chẽ dữ liệu đầu vào: độ dài tên hiển thị, định dạng email chuẩn và độ phức tạp mật khẩu.
   * Mã hóa mật khẩu an toàn bằng PBKDF2 qua ASP.NET Core Identity.
   * Gán vai trò mặc định `Roles.Author` cho người dùng đăng ký mới.
   * Phát hành cặp Access Token (15 phút) và Refresh Token (7 ngày), đóng gói Refresh Token vào cookie `rt`.
   * Endpoint `POST /api/v1/auth/register` trả về HTTP `201 Created` và `AuthResponseDto`.
2. **Cài đặt ca sử dụng Đăng nhập tài khoản (`FR-AUTH-002` - Local Login):**
   * Xây dựng `LoginCommand`, `LoginCommandHandler`, `LoginCommandValidator`.
   * Kiểm tra khóa tài khoản `UserManager.IsLockedOutAsync` chống tấn công brute-force.
   * Xác thực thông tin mật khẩu qua `UserManager.CheckPasswordAsync`.
   * Tự động tăng bộ đếm lỗi `AccessFailedCount` khi đăng nhập sai; đặt lại bộ đếm khi đăng nhập thành công.
   * Cấp cặp Access Token và Refresh Token mới, gán vào cookie `rt`.
   * Endpoint `POST /api/v1/auth/login` trả về HTTP `200 OK` và `AuthResponseDto`.
3. **Cấu hình Rate Limiting bảo vệ Endpoint xác thực:**
   * Cấu hình chính sách `auth-rate-limit` với `FixedWindowRateLimiter` tại `Program.cs`.
   * Áp dụng trực tiếp vào route group `/api/v1/auth` để giới hạn số request trên từng địa chỉ IP, ngăn chặn tấn công từ chối dịch vụ (DoS) và brute-force.
4. **Triển khai kiến trúc Cookie Same-Origin an toàn (ADR-0003):**
   * Tuyệt đối không trả Refresh Token trong JSON response body để triệt tiêu nguy cơ bị đánh cắp qua lỗ hổng Cross-Site Scripting (XSS).
   * Gói trong cookie `rt` với đầy đủ cờ bảo mật: `HttpOnly = true`, `Secure = true`, `SameSite = SameSiteMode.Strict`, `Path = /api/v1/auth`, `Expires = 7 days`.
5. **Bảo mật tầng cơ sở dữ liệu (Hash-Only Database Storage):**
   * Bảng `RefreshTokens` trong PostgreSQL chỉ lưu mã băm mật mã **SHA-256 (`TokenHash`)** kèm chỉ mục (Index) tìm kiếm $O(1)$, tuyệt đối không lưu plain text token.
6. **Xây dựng bộ kiểm thử tự động toàn diện:**
   * Viết 21 Unit Tests trong `AuthCommandValidatorTests.cs` kiểm thử các biên dữ liệu của validator.
   * Viết Integration Tests kiểm tra pipeline Minimal API thông qua `WebApplicationFactory`.

---

## 2. Phân định quyền sở hữu mã nguồn (Ownership Subtree)

Theo quy định kiến trúc trong `docs/tree.md` và `docs/KeHoach.md`, các thành phần được sửa đổi và sở hữu thuộc Issue #15 bao gồm:

* **API Endpoints:**
  * `src/CulinaryBlog.API/Endpoints/AuthEndpoints.cs`: Khai báo endpoint `POST /register`, `POST /login`, cấu hình cookie helper `SetRefreshTokenCookie`, `DeleteRefreshTokenCookie`, OpenAPI descriptions.
  * `src/CulinaryBlog.API/Program.cs`: Cấu hình dịch vụ Rate Limiter `auth-rate-limit`.
* **Application Layer (Commands & CQRS):**
  * `src/CulinaryBlog.Application/Auth/Commands/Register/` (`RegisterCommand`, `RegisterCommandHandler`, `RegisterCommandValidator`)
  * `src/CulinaryBlog.Application/Auth/Commands/Login/` (`LoginCommand`, `LoginCommandHandler`, `LoginCommandValidator`)
  * `src/CulinaryBlog.Application/Auth/DTOs/` (`AuthResponseDto`, `AuthUserDto`)
* **Infrastructure Layer:**
  * `src/CulinaryBlog.Infrastructure/Identity/IdentityService.cs`: Triển khai logic `RegisterAsync` và `LoginAsync`.
  * `src/CulinaryBlog.Infrastructure/Identity/JwtService.cs`: Sinh JWT token và hash SHA-256.
  * `src/CulinaryBlog.Infrastructure/Identity/RefreshTokenRepository.cs`: Thao tác bảng `RefreshTokens`.
* **Test Suites:**
  * `tests/CulinaryBlog.Application.Tests/Auth/AuthCommandValidatorTests.cs`: 21 bài kiểm thử validator.
  * `tests/CulinaryBlog.Integration.Tests/AuthEndpointsTests.cs`: Integration tests cho register/login.

---

## 3. Kiến trúc kỹ thuật và Chi tiết triển khai

### 3.1. Ca sử dụng Đăng ký tài khoản (`POST /api/v1/auth/register`)
* **Request DTO:** `RegisterRequest(string DisplayName, string Email, string Password)`
* **Quy tắc kiểm tra hợp lệ (`RegisterCommandValidator`):**
  * `DisplayName`: Bắt buộc, độ dài từ 2 đến 100 ký tự.
  * `Email`: Bắt buộc, đúng định dạng hộp thư điện tử (`EmailAddress()`), độ dài tối đa 256 ký tự.
  * `Password`: Bắt buộc, tối thiểu 8 ký tự, phải bao gồm ít nhất một chữ hoa (`[A-Z]`), một chữ thường (`[a-z]`), một chữ số (`[0-9]`), và một ký tự đặc biệt (`[!@#$%^&*...]`).
* **Quy trình xử lý tại `IdentityService.RegisterAsync`:**
  1. Kiểm tra sự tồn tại của Email trong cơ sở dữ liệu: Nếu đã tồn tại, ném lỗi `ConflictException("EMAIL_ALREADY_EXISTS")` (HTTP 409 Conflict).
  2. Tạo thực thể `ApplicationUser`: Sinh `UserName` tự động từ phần đầu của email (đảm bảo tính duy nhất), lưu `DisplayName`, `CreatedAt = DateTimeOffset.UtcNow`.
  3. Hash mật khẩu an toàn qua thuật toán PBKDF2 của ASP.NET Core Identity (`_userManager.CreateAsync(user, password)`).
  4. Gán vai trò mặc định: `_userManager.AddToRoleAsync(user, Roles.Author)`.
  5. Cấp Access Token (thời hạn 15 phút) chứa các claims nhận diện.
  6. Sinh một Refresh Token ngẫu nhiên bằng `RandomNumberGenerator` (64 bytes), băm SHA-256 thành `tokenHash`.
  7. Tạo bản ghi thực thể `RefreshToken.Create(user.Id, tokenHash, expiresAt: 7 days, ...)`, lưu vào PostgreSQL qua `IRefreshTokenRepository`.
  8. Gán Refresh Token vào cookie `rt` an toàn và trả về mã HTTP `201 Created`.

### 3.2. Ca sử dụng Đăng nhập tài khoản (`POST /api/v1/auth/login`)
* **Request DTO:** `LoginRequest(string Email, string Password)`
* **Quy tắc kiểm tra hợp lệ (`LoginCommandValidator`):**
  * `Email`: Bắt buộc, đúng định dạng email.
  * `Password`: Bắt buộc, không được để trống.
* **Quy trình xử lý tại `IdentityService.LoginAsync`:**
  1. Tìm kiếm người dùng theo Email: Nếu không tồn tại, ném lỗi `UnauthorizedException("INVALID_CREDENTIALS")` (HTTP 401 Unauthorized). Lưu ý: Không thông báo "Email không tồn tại" để phòng ngừa tấn công dò tìm tài khoản (User Enumeration).
  2. Kiểm tra trạng thái khóa tài khoản: Gọi `_userManager.IsLockedOutAsync(user)`. Nếu đang bị khóa do nhập sai nhiều lần, ném lỗi `UnauthorizedException("ACCOUNT_LOCKED_OUT")` kèm thông báo thời gian khóa còn lại.
  3. Kiểm tra tính chính xác của mật khẩu: Gọi `_userManager.CheckPasswordAsync(user, password)`.
     * Nếu sai: Tăng bộ đếm truy cập lỗi `_userManager.AccessFailedAsync(user)`. Nếu vượt quá ngưỡng cấu hình (5 lần), tài khoản tự động bị khóa tạm thời. Ném lỗi `UnauthorizedException("INVALID_CREDENTIALS")`.
     * Nếu đúng: Đặt lại bộ đếm truy cập sai về 0 qua `_userManager.ResetAccessFailedCountAsync(user)`.
  4. Cấp Access Token mới (15 phút) và Refresh Token mới (7 ngày).
  5. Lưu `TokenHash` mới vào PostgreSQL, trả về cookie `rt` và HTTP `200 OK`.

### 3.3. Cấu hình Rate Limiting bảo vệ hệ thống
* Cấu hình chính sách `auth-rate-limit` tại `Program.cs`:
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.AddPolicy("auth-rate-limit", httpContext =>
      {
          var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
          return RateLimitPartition.GetFixedWindowLimiter(
              partitionKey: clientIp,
              factory: _ => new FixedWindowRateLimiterOptions
              {
                  PermitLimit = 10,
                  Window = TimeSpan.FromMinutes(1),
                  QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                  QueueLimit = 0,
              });
      });
  });
  ```
* Khi client gửi vượt quá số lượng request cho phép trong 1 phút, ASP.NET Core tự động phản hồi mã trạng thái HTTP `429 Too Many Requests`.

### 3.4. Kiến trúc Cookie Same-Origin an toàn (ADR-0003)
* Helper cấu hình cookie được cài đặt tập trung trong `AuthEndpoints.cs`:
  ```csharp
  private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
  {
      var cookieOptions = new CookieOptions
      {
          HttpOnly = true,
          Secure = true,
          SameSite = SameSiteMode.Strict,
          Expires = DateTimeOffset.UtcNow.AddDays(7),
          Path = "/api/v1/auth",
      };
      httpContext.Response.Cookies.Append("rt", refreshToken, cookieOptions);
  }
  ```
* Bảng giải thích chi tiết các thuộc tính bảo mật:
  * `HttpOnly = true`: Chặn truy cập từ Javascript qua `document.cookie`, triệt tiêu lỗ hổng đánh cắp token qua XSS.
  * `Secure = true`: Đảm bảo cookie chỉ được gửi qua kết nối mã hóa SSL/TLS (HTTPS), chống tấn công nghe lén (Sniffing/Man-in-the-Middle).
  * `SameSite = Strict`: Cookie không được gửi trong các request xuất phát từ trang web khác, chống tấn công CSRF.
  * `Path = /api/v1/auth`: Thu hẹp phạm vi áp dụng của cookie, trình duyệt không gửi kèm cookie khi truy cập các trang tĩnh hoặc API bài viết/danh mục.

---

## 4. Kết quả kiểm thử và Nghiệm thu (Test Verification)

Toàn bộ hệ thống kiểm thử tự động cho Issue #15 đều được chạy và xác nhận đạt 100%:

### 4.1. Biên dịch dự án (Release Build)
* Lệnh chạy: `dotnet build CulinaryBlog.sln -c Release --warnaserror`
* **Kết quả:** `Build succeeded. 0 Warning(s), 0 Error(s).`

### 4.2. Bộ kiểm thử Đơn vị Unit Tests (`CulinaryBlog.Application.Tests`)
* Lệnh chạy: `dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj -c Release --no-build`
* **Kết quả:** `Passed: 76, Failed: 0, Total: 76 (100% Passed)`.
* Bao gồm 21 bài kiểm thử validator tại `AuthCommandValidatorTests.cs`:
  1. `RegisterCommandValidator_WithValidData_ShouldPassValidation`
  2. `RegisterCommandValidator_WithEmptyEmail_ShouldFailValidation`
  3. `RegisterCommandValidator_WithInvalidEmailFormat_ShouldFailValidation`
  4. `RegisterCommandValidator_WithShortPassword_ShouldFailValidation`
  5. `RegisterCommandValidator_WithPasswordMissingUppercase_ShouldFailValidation`
  6. `RegisterCommandValidator_WithPasswordMissingSpecialChar_ShouldFailValidation`
  7. `RegisterCommandValidator_WithEmptyDisplayName_ShouldFailValidation`
  8. `LoginCommandValidator_WithValidData_ShouldPassValidation`
  9. `LoginCommandValidator_WithEmptyEmail_ShouldFailValidation`
  10. `LoginCommandValidator_WithEmptyPassword_ShouldFailValidation`
  ... và các test validator cho RefreshToken, GoogleLogin, UpdateProfile.

### 4.3. Bộ kiểm thử Tích hợp Minimal API (`CulinaryBlog.Integration.Tests`)
* Lệnh chạy: `dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~AuthEndpointsTests"`
* **Kết quả:** `Passed: 17, Failed: 0, Total: 17 (100% Passed)`.
* Các ca kiểm thử tiêu biểu cho Issue #15:
  1. Đăng ký tài khoản thành công trả về HTTP 201 Created kèm JWT và Set-Cookie `rt`.
  2. Đăng ký với email trùng lặp trả về HTTP 409 Conflict (`EMAIL_ALREADY_EXISTS`).
  3. Đăng ký với mật khẩu yếu trả về HTTP 422 Unprocessable Entity kèm Problem Details errors.
  4. Đăng nhập thành công trả về HTTP 200 OK kèm JWT và cookie `rt`.
  5. Đăng nhập sai mật khẩu trả về HTTP 401 Unauthorized (`INVALID_CREDENTIALS`).
  6. Kiểm tra giới hạn tần suất gọi trả về HTTP 429 Too Many Requests khi vượt quá số lượng request cho phép.

---

## 5. Kết luận Issue #15

Issue #15 đã hoàn thành toàn diện các yêu cầu của Tuần 2:
* Cung cấp 2 API endpoints cốt lõi `/register` và `/login` ổn định, bảo mật cao.
* Đạt chuẩn kiến trúc ADR-0003 về lưu trữ cookie Same-Origin và băm mật mã cơ sở dữ liệu.
* Tạo tiền đề vững chắc cho Issue #19 (Tuần 3) thực hiện luồng xoay vòng token (Token Rotation), phát hiện phát lại token (Reuse Detection) và đăng xuất.
