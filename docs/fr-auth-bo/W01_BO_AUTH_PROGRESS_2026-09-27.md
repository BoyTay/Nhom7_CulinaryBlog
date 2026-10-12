# Báo cáo tiến độ W01 – Bơ – Baseline Security, Google Token Validation & Auth Pipeline (Issue #11)

**Ngày cập nhật:** 27/09/2026  
**Thành viên thực hiện:** Phùng Nguyễn Hoài Bơ (`HUBERTPHUNG` / Bo)  
**Mã số sinh viên:** 2312585  
**Nhóm:** 07 – Đồ án Culinary Blog  
**Nhánh làm việc (Branch):** [`feature/fr-auth-bo`](https://github.com/HubertPhung/Nhom7_CulinaryBlog/tree/feature/fr-auth-bo)  
**Issue:** [#11 - W01 Bo — Hardening security nền tảng, loại bỏ JWT/Admin secret mặc định, xác thực Google ID token & pipeline auth](https://github.com/BoyTay/Nhom7_CulinaryBlog/issues/11)  
**Pull Request:** [#103](https://github.com/BoyTay/Nhom7_CulinaryBlog/pull/103) (Merged vào `main`)  
**Commit tiêu biểu:** `cf9bfe5 fix(auth): resolve baseline security gaps, verify google tokens, and configure auth pipeline (#11)`  
**Milestone:** Week 01 (21/09/2026 – 27/09/2026)  

---

## 1. Mục tiêu và Phạm vi công việc (Scope of Work)

Theo phân công trong kế hoạch nhóm W01 (`docs/KeHoach.md` và `docs/WEEKLY_TEAM_PLAN.md`), mục tiêu chính của Issue #11 là giải quyết dứt điểm các lỗ hổng bảo mật nền tảng nghiêm trọng (Critical Security Gaps) từ bản scaffold ban đầu, khóa contract bảo mật và thiết lập pipeline xác thực chuẩn mực cho toàn bộ hệ thống CulinaryBlog:

1. **Loại bỏ Secret mặc định (Eliminate Hardcoded Secrets):**
   * Xóa bỏ hoàn toàn JWT secret key mặc định và Admin password mặc định hardcoded trong `appsettings.json`.
   * Bắt buộc cấu hình đọc từ biến môi trường thông qua tệp mẫu `.env.example`.
2. **Triển khai Xác thực Google ID Token chuẩn mật mã (Cryptographic Verification):**
   * Thay thế hoàn toàn việc đọc payload token thô sơ `ReadJwtToken` bằng thư viện `GoogleJsonWebSignature.ValidateAsync` kết hợp bộ khóa công khai Google JSON Web Keys (JWKS).
   * Kiểm tra nghiêm ngặt: chữ ký số, thời hạn hiệu lực, audience client ID, issuer và trạng thái xác thực email (`EmailVerified`).
3. **Cấu hình Pipeline Authentication & Authorization chuẩn mực:**
   * Cấu hình dịch vụ `AddAuthentication`, `AddJwtBearer` với các tham số xác minh `TokenValidationParameters` chặt chẽ, thiết lập `ClockSkew = TimeSpan.Zero`.
   * Định nghĩa chính sách phân quyền RBAC: `Policies.RequireAuthor` và `Policies.RequireAdmin`.
   * Sắp xếp đúng thứ tự middleware trong ASP.NET Core pipeline: `UseCors()` $\rightarrow$ `UseAuthentication()` $\rightarrow$ `UseAuthorization()`.
4. **Khởi tạo dữ liệu vai trò và tài khoản Admin có kiểm soát (`RoleSeeder`):**
   * Tự động khởi tạo 2 vai trò chuẩn: `Roles.Admin` và `Roles.Author`.
   * Khởi tạo tài khoản quản trị mặc định `admin@culinaryblog.com` vào cơ sở dữ liệu PostgreSQL.
5. **Xây dựng bộ kiểm thử bảo mật tầng biên (`AuthSecurityTests`):**
   * Viết đầy đủ 10 Unit Tests kiểm tra các kịch bản thất bại bảo mật: token bị sửa đổi chữ ký, token hết hạn, client ID sai lệch, email chưa kích hoạt.
   * Đảm bảo bản build Release đạt **0 lỗi, 0 cảnh báo** (`dotnet build --warnaserror`).

---

## 2. Phân định quyền sở hữu mã nguồn (Ownership Subtree)

Theo quy định kiến trúc trong `docs/tree.md` và `docs/KeHoach.md`, các thành phần được sửa đổi và sở hữu thuộc Issue #11 bao gồm:

* **Configuration & Environment:**
  * `.env.example`: Khai báo các biến môi trường bắt buộc `JWT__SECRETKEY`, `ADMIN__PASSWORD`, `GOOGLE__CLIENTID`.
  * `src/CulinaryBlog.API/appsettings.json` & `appsettings.Development.json`: Loại bỏ secret nhạy cảm.
* **Authentication Pipeline & Middleware:**
  * `src/CulinaryBlog.API/Program.cs`: Cấu hình `AddAuthentication`, `AddJwtBearer`, `AddAuthorization`, middleware pipeline.
  * `src/CulinaryBlog.API/Authorization/Policies.cs`: Định nghĩa `Policies.RequireAuthor`, `Policies.RequireAdmin`.
  * `src/CulinaryBlog.API/ErrorHandling/GlobalExceptionHandler.cs`: Xử lý `UnauthorizedException` chuẩn RFC 7807/9457.
* **Application Abstractions & Contracts:**
  * `src/CulinaryBlog.Application/Abstractions/Identity/IGoogleTokenValidator.cs`: Khai báo interface xác thực Google token.
  * `src/CulinaryBlog.Application/Abstractions/Identity/IJwtService.cs` & `ICurrentUser.cs`.
  * `src/CulinaryBlog.Application/Auth/Common/Roles.cs`: Định nghĩa hằng số `Roles.Admin`, `Roles.Author`.
* **Infrastructure Layer:**
  * `src/CulinaryBlog.Infrastructure/Identity/GoogleTokenValidator.cs`: Cài đặt logic xác thực Google JWKS.
  * `src/CulinaryBlog.Infrastructure/Identity/JwtService.cs`: Sinh Access Token HMAC-SHA256, sinh crypto-random Refresh Token.
  * `src/CulinaryBlog.Infrastructure/Persistence/Seeders/RoleSeeder.cs`: Khởi tạo role và admin user.
* **Test Suites:**
  * `tests/CulinaryBlog.Application.Tests/Auth/RefreshTokenTests.cs`: Kiểm thử vòng đời token.
  * `tests/CulinaryBlog.Integration.Tests/AuthSecurityTests.cs`: 10 bài test bảo mật chuyên sâu.

---

## 3. Kiến trúc kỹ thuật và Chi tiết triển khai

### 3.1. Loại bỏ Secret mặc định & Nạp cấu hình từ Môi trường
* Trước khi sửa đổi, `appsettings.json` lưu trữ trực tiếp `SecretKey = "YourDefaultSecretKeyThatIsExposedToGitHub123456"`. Kẻ tấn công có thể dễ dàng sử dụng khóa này để ký giả mạo JWT Access Token và đăng nhập với tư cách bất kỳ người dùng nào (kể cả Admin).
* **Giải pháp khắc phục:**
  1. Loại bỏ hoàn toàn secret key và mật khẩu admin khỏi các tệp `appsettings*.json`.
  2. Bắt buộc nạp cấu hình qua biến môi trường hoặc Secret Manager:
     ```bash
     JWT__SECRETKEY=c2VjdXJlX2p3dF9zZWNyZXRfa2V5X2Zvcl9kZXZlbG9wbWVudF9vbmx5XzEyMw==
     ADMIN__PASSWORD=Admin@SecurePass2026!
     GOOGLE__CLIENTID=your-google-client-id.apps.googleusercontent.com
     ```
  3. Thêm kiểm tra nghiêm ngặt khi ứng dụng khởi chạy (`Program.cs`): Nếu `SecretKey` có độ dài dưới 256 bits (32 bytes), hệ thống lập tức ném ngoại lệ dừng khởi động để ngăn chặn cấu hình yếu kém.

### 3.2. Cài đặt `GoogleTokenValidator` chuẩn mật mã Google JWKS
* **Interface `IGoogleTokenValidator`:**
  ```csharp
  public interface IGoogleTokenValidator
  {
      Task<GoogleTokenValidationResult> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
  }
  ```
* **Triển khai tại `GoogleTokenValidator.cs`:**
  * Sử dụng thư viện chính thức `Google.Apis.Auth` (`GoogleJsonWebSignature`).
  * Cấu hình `ValidationSettings`:
    ```csharp
    var settings = new GoogleJsonWebSignature.ValidationSettings
    {
        Audience = new[] { _configuration["Google:ClientId"] },
        IssuedAtClockTolerance = TimeSpan.FromSeconds(30),
    };
    ```
  * Xác thực đa tầng:
    1. Kiểm tra chữ ký số công khai từ Google JSON Web Keys (JWKS endpoint `https://www.googleapis.com/oauth2/v3/certs`).
    2. Kiểm tra `Audience` khớp chính xác với `Google:ClientId` của ứng dụng CulinaryBlog (chống tấn công mượn ID Token từ ứng dụng khác).
    3. Kiểm tra `Issuer` thuộc danh sách hợp lệ: `accounts.google.com` hoặc `https://accounts.google.com`.
    4. Kiểm tra thời hạn hết hạn `ExpirationTimeSeconds`.
    5. Kiểm tra bắt buộc `payload.EmailVerified == true`: Nếu người dùng Google chưa xác thực hộp thư điện tử, từ chối ngay lập tức bằng `UnauthorizedException("GOOGLE_EMAIL_NOT_VERIFIED")`.

### 3.3. Cấu hình ASP.NET Core Authentication Pipeline
* Đăng ký Authentication và cấu hình JWT Bearer trong `Program.cs`:
  ```csharp
  builder.Services.AddAuthentication(options =>
  {
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  })
  .AddJwtBearer(options =>
  {
      options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
      options.SaveToken = true;
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
          ValidateIssuer = true,
          ValidIssuer = jwtOptions.Issuer,
          ValidateAudience = true,
          ValidAudience = jwtOptions.Audience,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.Zero, // Chống việc tiếp tục sử dụng token đã hết hạn
      };
  });
  ```
* Cấu hình Authorization Policies tại `Policies.cs`:
  ```csharp
  public static class Policies
  {
      public const string RequireAuthor = "RequireAuthor";
      public const string RequireAdmin = "RequireAdmin";

      public static void AddAuthorizationPolicies(AuthorizationOptions options)
      {
          options.AddPolicy(RequireAuthor, policy =>
              policy.RequireRole(Roles.Author, Roles.Admin));
          options.AddPolicy(RequireAdmin, policy =>
              policy.RequireRole(Roles.Admin));
      }
  }
  ```

### 3.4. Khởi tạo dữ liệu mẫu vai trò (`RoleSeeder`)
* Cài đặt `RoleSeeder` tự động seed 2 vai trò `Roles.Admin` và `Roles.Author` thông qua `RoleManager<IdentityRole>`.
* Kiểm tra tài khoản admin mặc định: Nếu chưa tồn tại, tạo tài khoản `admin@culinaryblog.com` với mật khẩu được nạp an toàn từ cấu hình `ADMIN__PASSWORD`, tự động gán vai trò `Roles.Admin`.

---

## 4. Kết quả kiểm thử và Nghiệm thu (Test Verification)

Toàn bộ các tiêu chí chấp nhận (Acceptance Criteria) của Issue #11 đã được kiểm định tự động:

### 4.1. Biên dịch dự án (Release Build)
* Lệnh chạy: `dotnet build CulinaryBlog.sln -c Release --warnaserror`
* **Kết quả:** `Build succeeded. 0 Warning(s), 0 Error(s).`

### 4.2. Bộ kiểm thử bảo mật chuyên sâu (`AuthSecurityTests`)
* Lệnh chạy: `dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj -c Release --filter "FullyQualifiedName~AuthSecurityTests"`
* **Kết quả:** `Passed: 10, Failed: 0, Total: 10 (100% Passed)`.
* Danh mục 10 ca kiểm thử bảo mật:
  1. `ValidateTokenWithTamperedSignatureReturnsFalse`: Kiểm tra chữ ký JWT bị giả mạo bị từ chối xác thực.
  2. `ValidateTokenWithExpiredTimestampReturnsFalse`: Kiểm tra token hết hạn bị từ chối ngay lập tức (ClockSkew = Zero).
  3. `ValidateTokenWithWrongIssuerOrAudienceReturnsFalse`: Kiểm tra sai lệch nhà phát hành hoặc đối tượng nhận bị chặn.
  4. `GoogleTokenValidatorWithInvalidSignatureThrowsUnauthorizedException`: Kiểm tra Google token giả chữ ký bị ném lỗi `UnauthorizedException`.
  5. `GoogleTokenValidatorWithMismatchedAudienceThrowsUnauthorizedException`: Kiểm tra Google token từ Client ID khác bị từ chối.
  6. `GoogleTokenValidatorWithUnverifiedEmailThrowsUnauthorizedException`: Kiểm tra email chưa xác thực từ Google bị từ chối.
  7. `GenerateAccessTokenIncludesRequiredClaims`: Kiểm tra Access Token sinh ra chứa đầy đủ claims (`sub`, `email`, `role`, `name`).
  8. `GenerateRefreshTokenProducesCryptographicallySecureRandomBytes`: Kiểm tra Refresh Token sinh ra có độ hỗn loạn (entropy) cao và duy nhất.
  9. `HashTokenWithSha256ProducesConsistentHexDigest`: Kiểm tra hàm băm SHA-256 sinh chuỗi băm chuẩn 64 ký tự hex.
  10. `RoleSeederCreatesAdminUserAndRolesWhenConfigured`: Kiểm tra RoleSeeder khởi tạo đúng vai trò và tài khoản quản trị.

### 4.3. Kiểm định Clean Architecture
* Lệnh chạy: `dotnet test tests/CulinaryBlog.Architecture.Tests/CulinaryBlog.Architecture.Tests.csproj -c Release`
* **Kết quả:** `Passed: 3, Failed: 0, Total: 3 (100% Passed)`.

---

## 5. Kết luận Issue #11

Issue #11 đã hoàn thành xuất sắc toàn bộ mục tiêu của Tuần 1:
* Triệt tiêu hoàn toàn rủi ro bảo mật nghiêm trọng (hardcoded secrets).
* Xây dựng nền móng xác thực JWT và Google Token an toàn, đạt chuẩn công nghiệp.
* Sẵn sàng cung cấp contract và pipeline bảo mật cho Issue #15 (Tuần 2) và cho các phân hệ khác (Recipe của Trường, Category của Hiếu) tích hợp.
