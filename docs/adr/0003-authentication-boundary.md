# ADR-0003: Ranh giới xác thực web và Google OAuth

- Trạng thái: Accepted
- Ngày: 2026-09-20

## Quyết định

- Google Identity Services chạy ở frontend và gửi Google ID token đến `POST /api/v1/auth/google`; backend xác minh token trước khi liên kết hoặc tạo tài khoản.
- Access token JWT sống 15 phút và chỉ giữ trong bộ nhớ của frontend.
- Refresh token sống 7 ngày, được rotation, chỉ lưu hash ở database và raw token được gửi bằng cookie `HttpOnly`, `Secure`, `SameSite=Strict` trên cùng origin qua Nginx.
- Các endpoint refresh/logout đọc cookie thay vì nhận refresh token trong JSON body. State-changing requests giữ kiểm tra `Origin` và CORS allow-list để tăng phòng vệ CSRF.

## Hệ quả

Frontend không đọc được refresh token, giảm tác động của XSS. Local development cần proxy cùng origin hoặc cấu hình cookie/CORS chính xác.
