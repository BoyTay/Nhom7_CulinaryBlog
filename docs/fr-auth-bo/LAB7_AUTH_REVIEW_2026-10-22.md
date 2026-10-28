# Tổng hợp Auth/frontend cho báo cáo Lab 7

- Sinh viên: **Phùng Nguyễn Hoài Bo — 2312585**.
- Rà soát ban đầu: **22/10/2026** trên `7d9edb0`; hoàn thành và kiểm tra lại: **28/10/2026** trên `feature/fr-auth-bo`.
- Phạm vi: Auth/Identity/Security và các trang `(auth)`, `profile` theo `docs/tree.md`.
  Category/nền tảng thuộc Hiếu; Recipe/File thuộc Trường; Search/Jobs thuộc Kiên; báo cáo của Bo không nhận các module đó.

## Kết luận

Phần Auth của Lab 7 đã đủ: đăng ký, đăng nhập, đăng xuất, đổi mật khẩu, quên/đặt lại mật khẩu qua email,
hồ sơ có tải ảnh đại diện. Bảy điểm phát hiện trong lần rà soát 22/10 đã được sửa (mục dưới).

## Đối chiếu yêu cầu

| Yêu cầu | Kết quả |
| --- | --- |
| Đăng ký | API, form, kiểm tra đầu vào khớp chính sách mật khẩu của server, lỗi 422 hiện theo trường. |
| Đăng nhập | API/form, thông báo sai thông tin, 429 và banner khóa tài khoản đọc `ProblemDetails.type`. |
| Đăng xuất | Thu hồi refresh token, xóa cookie; access token hết hạn thì refresh một lần rồi gọi lại. |
| Đổi mật khẩu | `POST /api/v1/auth/change-password`: kiểm tra mật khẩu hiện tại, thu hồi mọi refresh token, xóa cookie, buộc đăng nhập lại. |
| Quên/đặt lại mật khẩu | `POST /forgot-password` luôn trả 202 cùng một thông điệp (không lộ email tồn tại); token chỉ gửi qua email, hiệu lực 30 phút, dùng một lần. `POST /reset-password` đặt mật khẩu mới, mở khóa tài khoản, thu hồi mọi phiên. |
| Hồ sơ | Trang được bảo vệ; sửa tên/bio, xóa trắng avatar/bio được lưu đúng; `POST /me/avatar` tải ảnh JPEG/PNG/WebP/AVIF ≤ 5 MB lên MinIO, kiểm tra chữ ký tệp, xóa ảnh cũ của chính người dùng. |

## Các điểm của lần rà soát 22/10 và cách sửa

1. **Thiếu đổi mật khẩu, quên/đặt lại mật khẩu:** đã có command, endpoint, email (`SmtpPasswordResetEmailSender`) và trang `/forgot-password`, `/reset-password`, form đổi mật khẩu trong hồ sơ.
2. **Banner khóa tài khoản:** `isAccountLocked` đọc `problem.type`; Google cũng trả `ACCOUNT_LOCKED` (W08-A2, A3).
3. **Validation 422:** login/register/profile ánh xạ lỗi theo trường cho cả 400 và 422; `auth-errors.ts` dùng chung cho các form mới.
4. **Không xóa được avatar/bio:** form gửi chuỗi rỗng, server lưu thành `null`.
5. **Không tự refresh:** `AuthProvider` làm mới token trước khi hết hạn 60 giây, khi tab hiện lại, và tuần tự hóa giữa các tab (W08-A4, A5).
6. **Nút Google dự phòng:** đã có CSS cho `auth-google-fallback-btn`/`auth-google-icon`.
7. **returnUrl:** `safeReturnUrl` chỉ nhận đường dẫn nội bộ, chặn `https://…`, `//…` và `/\…` (trình duyệt coi `\` như `/`).

## Kiểm thử ngày 28/10

| Kiểm tra | Kết quả |
| --- | --- |
| `dotnet build CulinaryBlog.sln -warnaserror` | 0 warning, 0 error |
| Architecture tests | 3/3 |
| Application tests | 171/171 (gồm 4 test tải avatar) |
| Integration tests (PostgreSQL local) | 195 đạt, 2 bỏ qua (MinIO); gồm test quên/đặt lại/đổi mật khẩu |
| Frontend `npm run typecheck`, `npm run lint` | sạch |
| Frontend `npm test` | 98/98 |

E2E trên Docker Compose (Next.js, API, PostgreSQL, MinIO, MailHog) bằng Playwright: quên mật khẩu → email trong MailHog →
đặt lại → link dùng lại bị từ chối → đăng nhập; đổi mật khẩu sai/đúng; tải avatar. Ảnh lưu ở `TestResults/lab7-final/` (gitignore)
và được nhúng trong báo cáo Word.

## Ngoài phạm vi của Bo

Trang chủ, trang category công khai (Hiếu) và lỗi trang chi tiết công thức do enum dạng số (Trường, W08-T1) được giao lại cho
người phụ trách; Bo không sửa các module này.
