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

Căn cứ vào kế hoạch phân công công việc tại `docs/KeHoach.md` và `docs/WEEKLY_TEAM_PLAN.md` (Tuần 6: 26/10–01/11/2026):
Issue #31 yêu cầu thành viên Bo xây dựng toàn diện phân hệ giao diện xác thực (Frontend Auth) với các mục tiêu cốt lõi:

1. **Tuân thủ kiến trúc bảo mật ADR-0003:**
   - **In-Memory Access Token:** Access token JWT tuyệt đối không được lưu vào `localStorage`, `sessionStorage` hay cookies thông thường nhằm loại bỏ hoàn toàn nguy cơ bị đánh cắp qua lỗ hổng XSS (Cross-Site Scripting). Token chỉ tồn tại trong bộ nhớ RAM (closure scope / React state).
   - **HttpOnly Refresh Cookie:** Refresh token được lưu trong cookie bảo mật `rt` (`HttpOnly`, `Secure`, `SameSite=Strict`). Trình duyệt tự động gửi cookie này khi gọi các endpoint `/refresh` và `/logout` qua proxy same-origin `/api/v1/*`.
   - **Silent Refresh:** Tự động khôi phục phiên đăng nhập khi người dùng tải lại trang hoặc truy cập ứng dụng lần đầu bằng cách gọi `POST /api/v1/auth/refresh`.

2. **Xây dựng bộ giao diện xác thực hoàn chỉnh:**
   - Trang Đăng nhập (`/login`) và Đăng ký (`/register`) với đầy đủ ràng buộc validation, client-side error handling và server Problem Details RFC 7807.
   - Xử lý các trạng thái tài khoản chuyên biệt: Cảnh báo tạm khóa tài khoản (401 `ACCOUNT_LOCKED_OUT`), giới hạn tần suất yêu cầu (429 Rate Limit Exceeded).
   - Tích hợp nút đăng nhập **Google Sign-In** chuẩn Google Identity Services (GIS SDK).

3. **Quản lý hồ sơ và bảo vệ tuyến đường (Protected Routes):**
   - Xây dựng component `ProtectedRoute` bảo vệ các trang yêu cầu xác thực, ngăn chặn hiện tượng Flash of Unauthenticated Content (FOUC), hỗ trợ phân quyền theo vai trò (RBAC).
   - Trang Hồ sơ cá nhân (`/profile`): hiển thị thông tin người dùng, ảnh đại diện, danh sách vai trò (Admin, Chef, User) và cho phép chỉnh sửa thông tin qua `PATCH /api/v1/auth/me`.
   - Cơ chế Đăng xuất an toàn: gọi API hủy cookie `rt`, xóa sạch token trong RAM và đưa người dùng về trạng thái khách.

4. **Đảm bảo các tiêu chí bàn giao (Delivery Gates 1-6):**
   - Giữ mã nguồn hoàn toàn trong ranh giới sở hữu: `src/CulinaryBlog.Web/lib/auth/`, `components/auth/`, `app/(auth)/`, `app/profile/`. Không xâm phạm vào code Category/Recipe/Search.
   - Cung cấp đầy đủ 4 trạng thái UX (Idle, Loading, Validation, Error/Lockout).
   - Kiểm tra `typecheck`, `lint` và `build` thành công 100% không cảnh báo.

---

## 2. Kế hoạch triển khai chia 4 Commits

Theo phương pháp chia nhỏ nhiệm vụ độc lập và khép kín, Issue #31 được phân bổ thành 4 commits:

```text
Commit 1: feat(auth-fe): in-memory session store, auth client and auth provider
Commit 2: feat(auth-fe): build login and register pages with validation and google sign-in
Commit 3: feat(auth-fe): build profile management page and protected route guard
Commit 4: test(auth-fe): comprehensive validation tests, error boundaries, empty states and documentation
```

---

## 3. Tiến độ thực hiện chi tiết

### 3.1. Commit 1: In-Memory Session Store, Auth Client & Auth Provider
- **Trạng thái:** **Hoàn thành**
- **Nội dung thực hiện:**
  1. `src/CulinaryBlog.Web/lib/auth/auth-store.ts`:
     - Cài đặt in-memory token store với closure scope, cung cấp các hàm `getAccessToken()`, `setAccessToken()`, `clearAccessToken()`.
     - Tuyệt đối không chạm vào `localStorage` hay `sessionStorage`.
     - Cung cấp cơ chế đăng ký lắng nghe thay đổi token (`subscribeTokenChange`).
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

### 3.2. Kế hoạch Commit 2: Login, Register Pages & Google Sign-In
- **Trạng thái:** Chuẩn bị triển khai
- **Nội dung dự kiến:**
  - `components/auth/google-sign-in-button.tsx`: Tích hợp Google Identity Services SDK.
  - `components/auth/login-form.tsx` & `app/(auth)/login/page.tsx`: Form đăng nhập, xử lý 401 `ACCOUNT_LOCKED_OUT` banner và 429 Rate limit.
  - `components/auth/register-form.tsx` & `app/(auth)/register/page.tsx`: Form đăng ký với Identity password policy rules.
  - `app/(auth)/auth.css`: Bộ quy chuẩn giao diện riêng cho phân hệ xác thực.

### 3.3. Kế hoạch Commit 3: Profile Management & Protected Route Guard
- **Trạng thái:** Chuẩn bị triển khai
- **Nội dung dự kiến:**
  - `components/auth/protected-route.tsx`: Route guard ngăn chặn truy cập trái phép.
  - `app/profile/page.tsx` & `components/auth/profile-form.tsx`: Trang thông tin cá nhân và cập nhật hồ sơ (`PATCH /me`).
  - Tích hợp trạng thái đăng nhập trên Header điều hướng (`app/page.tsx`).

### 3.4. Kế hoạch Commit 4: Tests, UX Skeletons & Báo cáo tổng kết
- **Trạng thái:** Chuẩn bị triển khai
- **Nội dung dự kiến:**
  - Viết unit tests kiểm tra in-memory isolation và Problem Details mapping.
  - Bổ sung loading skeletons và empty states.
  - Xác thực nghiệm thu toàn diện `typecheck`, `lint`, `build`.

---

## 4. Minh chứng kiểm thử & Chất lượng mã nguồn (Verification)

| Kiểm tra | Lệnh thực thi | Kết quả | Ghi chú |
|---|---|---|---|
| **TypeScript Typecheck** | `npm run typecheck` | **PASS (Exit code 0)** | 0 lỗi kiểu dữ liệu |
| **ESLint Validation** | `npm run lint` | **PASS (Exit code 0)** | 0 warning, 0 error |
| **Next.js Production Build** | `npm run build` | **PASS (Exit code 0)** | Tạo thành công toàn bộ static/dynamic routes |
| **ADR-0003 In-memory Token** | Manual & Code Review | **ĐẠT** | Không dùng Web Storage |
