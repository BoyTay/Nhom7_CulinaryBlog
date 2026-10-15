# Báo cáo tiến độ Issue #31 — W06 Bo: Auth/Profile Frontend

**Tuần thực hiện:** Tuần 6 (13/10/2026 – 19/10/2026)  
**Ngày cập nhật:** 19/10/2026  
**Thành viên thực hiện:** Phùng Nguyễn Hoài Bơ (`HUBERTPHUNG` / Bo)  
**Mã số sinh viên:** 2312585  
**Nhóm:** 07 – Đồ án Culinary Blog  
**Nhánh làm việc (Branch):** [`feature/fr-auth-bo`](https://github.com/BoyTay/Nhom7_CulinaryBlog/tree/feature/fr-auth-bo)  
**Phân hệ phụ trách:** Frontend Xác thực & Quản lý Danh tính (`FR-AUTH`)  
**GitHub Issue:** [#31 — W06 Bo — Auth/Profile frontend](https://github.com/BoyTay/Nhom7_CulinaryBlog/issues/31)  
**Ranh giới mã nguồn (`docs/tree.md`):** `frontend/(auth)/{login,register,profile,session,protected-routes}`  

---

## 1. Tổng quan mục tiêu & Yêu cầu của Issue #31

Căn cứ vào kế hoạch phân công công việc tại `docs/KeHoach.md`, `docs/WEEKLY_TEAM_PLAN.md` và tài liệu bàn giao thiết kế `docs/design/DESIGN_HANDOFF.md`:
Issue #31 yêu cầu xây dựng toàn diện phân hệ giao diện xác thực (Frontend Auth) với các mục tiêu cốt lõi:

1. **Tuân thủ kiến trúc bảo mật ADR-0003:**
   - **In-Memory Access Token:** Access token JWT tuyệt đối không được lưu vào `localStorage`, `sessionStorage` hay cookies thông thường nhằm loại bỏ hoàn toàn nguy cơ bị đánh cắp qua lỗ hổng XSS (Cross-Site Scripting). Token chỉ tồn tại trong bộ nhớ RAM (closure scope / React state).
   - **HttpOnly Refresh Cookie:** Refresh token được lưu trong cookie bảo mật `rt` (`HttpOnly`, `Secure`, `SameSite=Strict`). Trình duyệt tự động gửi cookie này khi gọi các endpoint `/refresh` và `/logout` qua proxy same-origin `/api/v1/*`.
   - **Silent Refresh:** Tự động khôi phục phiên đăng nhập khi người dùng tải lại trang hoặc truy cập ứng dụng lần đầu bằng cách gọi `POST /api/v1/auth/refresh`.

2. **Khớp 100% với tài liệu thiết kế Design Handoff (`docs/design/`):**
   - Áp dụng lớp phạm vi `.ds-scope` và bảng màu tokens: primary `#2D5D43`, primary-tint `#E4EEE7`, dark-text `#1F4630`, bg `#F6F5F0`, surface `#FFFFFF`, line `#E2E0D6`, danger `#A8321C`, danger-tint `#FBE9E5`, success-tint `#D6EADB`.
   - Typography chuẩn: **Bricolage Grotesque** 600/700 cho tiêu đề, **Be Vietnam Pro** 400/500/600 cho nội dung tiếng Việt.
   - Artboard `Login.dc.html`: Layout Split-screen (cột trái xanh `#2D5D43` với "Vào bếp cùng cộng đồng.", cột phải 400px có tablist switch giữa Đăng nhập/Đăng ký, alert lỗi/khóa tài khoản, divider "hoặc", Google Sign-In GIS SDK).
   - Artboard `Profile.dc.html`: Header điều hướng, thẻ hồ sơ 720px, avatar 88px, badge vai trò, form chỉnh sửa `displayName`, `avatarUrl`, `bio`, email readonly, alert "Đã lưu thay đổi", nút "Lưu thay đổi" và "Đăng xuất".

3. **Bảo vệ tuyến đường (Protected Routes):**
   - Xây dựng component `ProtectedRoute` bảo vệ các trang yêu cầu xác thực, ngăn chặn hiện tượng Flash of Unauthenticated Content (FOUC), hỗ trợ phân quyền theo vai trò (RBAC).

4. **Đảm bảo các tiêu chí bàn giao (Delivery Gates 1-6):**
   - Giữ mã nguồn hoàn toàn trong ranh giới sở hữu: `src/CulinaryBlog.Web/lib/auth/`, `components/auth/`, `app/(auth)/`, `app/profile/`, `components/shared/site-header.tsx`. Không xâm phạm vào code Category/Recipe/Search.
   - Cung cấp đầy đủ 4 trạng thái UX (Idle, Loading, Validation, Error/Lockout).
   - Kiểm tra `typecheck`, `lint`, `test`, `build` thành công 100% không cảnh báo.

---

## 2. Kế hoạch triển khai chia 4 Commits

Issue #31 được thực hiện hoàn chỉnh qua 4 commits độc lập và khép kín:

```text
Commit 1: feat(auth-fe): in-memory session store, auth client and auth provider
Commit 2: feat(auth-fe): build login and register pages with validation and google sign-in
Commit 3: feat(auth-fe): build profile management page and protected route guard (#31)
Commit 4: test(auth-fe): comprehensive validation tests, error boundaries, empty states and documentation (#31)
```

---

## 3. Tiến độ thực hiện chi tiết

### 3.1. Commit 1: In-Memory Session Store, Auth Client & Auth Provider
- **Trạng thái:** **Hoàn thành**
- **Nội dung thực hiện:**
  1. `src/CulinaryBlog.Web/lib/auth/auth-store.ts`:
     - Cài đặt in-memory token store với closure scope, cung cấp các hàm `getAccessToken()`, `setAccessToken()`, `clearAccessToken()`.
     - Tuyệt đối không chạm vào `localStorage` hay `sessionStorage`.
     - Cung cấp cơ chế đăng ký lắng nghe thay đổi token (`onTokenChange`).
  2. `src/CulinaryBlog.Web/lib/auth/auth-client.ts`:
     - Tích hợp gọi các endpoints xác thực từ backend: `/login`, `/register`, `/google`, `/refresh`, `/logout`, `/me`, `PATCH /me`.
     - Tự động gắn header `Authorization: Bearer <accessToken>` và cấu hình `credentials: "include"`.
     - Xử lý lỗi `ApiProblemDetails` chuẩn RFC 7807 từ ASP.NET Core backend.
  3. `src/CulinaryBlog.Web/lib/auth/auth-context.tsx` & `use-auth.ts`:
     - Cung cấp `AuthProvider` quản lý trạng thái `user`, `isAuthenticated`, `isLoading`.
     - Tự động chạy **Silent Refresh** khi mount để khôi phục phiên nếu cookie `rt` còn hạn.
     - Cung cấp các thao tác `login`, `loginWithGoogle`, `register`, `logout`, `updateProfile`.
  4. `src/CulinaryBlog.Web/app/layout.tsx`:
     - Tích hợp `AuthProvider` vào cây component gốc, nằm trong `QueryProvider` mà không làm xáo trộn cấu hình hiện có của nhóm.

### 3.2. Commit 2: Login, Register Pages & Google Sign-In
- **Trạng thái:** **Hoàn thành**
- **Nội dung thực hiện:**
  1. `src/CulinaryBlog.Web/app/(auth)/auth.css`:
     - Xây dựng layout Split-screen chuẩn artboard `Login.dc.html` với panel trái nền xanh `#2D5D43`, typography Bricolage Grotesque, panel phải 400px.
     - Hỗ trợ responsive cho cả mobile và desktop.
  2. `src/CulinaryBlog.Web/components/auth/google-sign-in-button.tsx`:
     - Tích hợp Google Identity Services (GIS SDK `https://accounts.google.com/gsi/client`) chuẩn chữ ký Google.
     - Lắng nghe callback credential (ID token JWT), gọi `loginWithGoogle(idToken)`.
     - Cung cấp giao diện fallback an toàn khi mất kết nối hoặc chưa nạp `NEXT_PUBLIC_GOOGLE_CLIENT_ID`.
  3. `src/CulinaryBlog.Web/components/auth/login-form.tsx` & `src/CulinaryBlog.Web/app/(auth)/login/page.tsx`:
     - Trang đăng nhập `/login` dạng Split-screen với tablist switch giữa Đăng nhập và Đăng ký.
     - Client validation (định dạng email, bắt buộc mật khẩu).
     - Hỗ trợ chuyển hướng `returnUrl` sau khi đăng nhập thành công.
     - Xử lý các kịch bản lỗi chuyên sâu: Banner cảnh báo tạm khóa tài khoản (401 `ACCOUNT_LOCKED_OUT`), cảnh báo giới hạn tần suất (429 Rate Limit), và lỗi thông tin đăng nhập không hợp lệ.
     - Tích hợp `Suspense` boundary với component skeleton loading chống FOUC.
  4. `src/CulinaryBlog.Web/components/auth/register-form.tsx` & `src/CulinaryBlog.Web/app/(auth)/register/page.tsx`:
     - Trang đăng ký `/register` với kiểm tra độ mạnh mật khẩu (tối thiểu 8 ký tự, chữ hoa, chữ thường, chữ số, ký tự đặc biệt).
     - Kiểm tra xác nhận mật khẩu và ánh xạ lỗi từ server Problem Details (409 Duplicate email, 400 validation).
     - Tích hợp `Suspense` boundary với skeleton loader.

### 3.3. Commit 3: Profile Management & Protected Route Guard
- **Trạng thái:** **Hoàn thành**
- **Nội dung thực hiện:**
  1. `src/CulinaryBlog.Web/components/auth/protected-route.tsx`:
     - Route Guard bọc quanh các trang yêu cầu đăng nhập.
     - Chống FOUC bằng skeleton loading trang nhã trong lúc kiểm tra phiên (`isLoading`).
     - Tự động điều hướng về `/login?returnUrl=...` khi người dùng là khách.
     - Hỗ trợ phân quyền dựa trên vai trò `requiredRoles` (RBAC).
  2. `src/CulinaryBlog.Web/components/auth/profile-form.tsx` & `src/CulinaryBlog.Web/app/profile/profile.css`:
     - Hiện thực chính xác theo artboard `Profile.dc.html`:
       - Avatar 88px bo tròn hiển thị chữ cái đầu hoặc ảnh đại diện từ `avatarUrl`.
       - Badge vai trò người dùng (Tác giả, Quản trị viên, Độc giả).
       - Cho phép sửa `Tên hiển thị` (2–100 ký tự), `Ảnh đại diện (URL)`, `Giới thiệu ngắn (Bio)`.
       - Trường `Email` và `Mã định danh tài khoản` ở chế độ readonly với nhãn giải thích "Email không thể đổi tại đây."
       - Trạng thái thông báo thành công `role="status"` "Đã lưu thay đổi."
       - Nút "Lưu thay đổi" (primary green `#2D5D43`) và nút "Đăng xuất" (outline danger `#A8321C`).
  3. `src/CulinaryBlog.Web/app/profile/page.tsx`:
     - Trang `/profile` áp dụng scope `.ds-scope`, tích hợp `SiteHeader`, tiêu đề H1 và `ProfileForm`, bọc hoàn toàn bởi `ProtectedRoute`.
  4. `src/CulinaryBlog.Web/components/shared/site-header.tsx` & `src/CulinaryBlog.Web/components/auth/user-nav.tsx`:
     - Header điều hướng nhận biết trạng thái đăng nhập: hiển thị avatar pill liên kết tới `/profile` khi đã đăng nhập, và hiển thị "Đăng nhập" / "Đăng ký" khi là khách.
     - Tích hợp `UserNav` vào trang chủ `app/page.tsx`.

### 3.4. Commit 4: Validation Tests, Empty States & Documentation
- **Trạng thái:** **Hoàn thành**
- **Nội dung thực hiện:**
  1. `src/CulinaryBlog.Web/tests/auth/auth-store.test.ts`:
     - Kiểm thử in-memory storage: lưu token, đọc token, xóa token.
     - Kiểm thử cơ chế đăng ký và hủy lắng nghe (`onTokenChange`).
     - Kiểm chứng bảo mật ADR-0003: khẳng định tuyệt đối không tương tác hay ghi token vào `localStorage` / `sessionStorage`.
  2. `src/CulinaryBlog.Web/tests/auth/auth-client.test.ts`:
     - Kiểm thử `authFetch`: tự động gắn `credentials: "include"`, `Accept: application/json`.
     - Kiểm thử tự động gắn `Authorization: Bearer <token>` khi có token trong `authStore`.
     - Kiểm thử ánh xạ lỗi ASP.NET Core `ApiProblemDetails` (401, 400, 409, 429) thành instance `ApiError`.
     - Kiểm thử các hàm API client: `loginApi`, `registerApi`, `logoutApi`, `updateProfileApi`.
  3. `src/CulinaryBlog.Web/tests/auth/auth-validation.test.ts`:
     - Kiểm thử các quy tắc validation client-side: email format, độ phức tạp mật khẩu (8 ký tự, hoa, thường, số, ký tự đặc biệt), chống thẻ HTML trong tên hiển thị, kiểm tra giao thức hợp lệ của `avatarUrl` (chỉ chấp nhận `http://` hoặc `https://`).
  4. `src/CulinaryBlog.Web/package.json`:
     - Bổ sung lệnh `"test": "node --test --experimental-strip-types tests/**/*.test.ts"`. Toàn bộ 20/20 unit tests chạy trực tiếp và vượt qua 100%.

---

## 4. Minh chứng kiểm thử & Chất lượng mã nguồn (Verification)

| Kiểm tra | Lệnh thực thi | Kết quả | Ghi chú |
|---|---|---|---|
| **TypeScript Typecheck** | `npm run typecheck` | **PASS (Exit code 0)** | 0 lỗi kiểu dữ liệu (kể cả test files) |
| **ESLint Validation** | `npm run lint` | **PASS (Exit code 0)** | 0 warning, 0 error |
| **Frontend Unit Tests** | `npm test` | **PASS (20/20 passed)** | 100% tests cho store, client, validation |
| **Next.js Production Build** | `npm run build` | **PASS (Exit code 0)** | Tạo thành công routes: `/`, `/login`, `/register`, `/profile`, `/search`, `/admin/categories` |
| **ADR-0003 In-memory Token** | Unit Test & Code Review | **ĐẠT (PASS)** | Bộ nhớ RAM (closure), không chạm Web Storage |
| **Backend Auth Unit Tests** | `dotnet test --filter "FullyQualifiedName~Auth"` | **PASS (41/41 passed)** | Toàn bộ auth validator, service, token tests |
| **Architecture Tests** | `dotnet test tests/CulinaryBlog.Architecture.Tests` | **PASS (3/3 passed)** | Ranh giới kiến trúc Clean Architecture toàn vẹn |

---

## 5. Kết luận & Trạng thái nghiệm thu

- Issue #31 phân hệ Auth & Profile Frontend đã được hoàn tất trọn vẹn theo đúng kế hoạch 4 commits, tuân thủ nghiêm ngặt kiến trúc bảo mật ADR-0003, khớp 100% tài liệu thiết kế `docs/design/DESIGN_HANDOFF.md` và các artboards `Login.dc.html`, `Profile.dc.html`.
- Toàn bộ thay đổi được lưu trữ an toàn trong các commits local trên nhánh `feature/fr-auth-bo`, sẵn sàng cho buổi review của nhóm.
