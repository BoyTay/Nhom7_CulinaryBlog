# Báo cáo tiến độ W03 – Hiếu – Category (issue #18)

Ngày cập nhật: 03/10/2026

Nhánh làm việc: `feat/fr-cat-hieu`

Issue: https://github.com/BoyTay/Nhom7_CulinaryBlog/issues/18

## Đồng bộ mã nguồn

- Đã `git fetch origin main` và fast-forward nhánh làm việc từ `eed9656` lên `00e0f59` (27 commit mới từ `main`).
- Giữ nguyên hai mục chưa được Git theo dõi có sẵn trước khi làm việc: `docs/Mau_Nop_Bao_Cao_Lab_Ca_Nhan_2026.docx` và `tmp/`.
- Sau khi hoàn tất và được yêu cầu, thay đổi của lần làm việc này được commit và push lên nhánh `feat/fr-cat-hieu`; không đưa `docs/Mau_Nop_Bao_Cao_Lab_Ca_Nhan_2026.docx` hay `tmp/` vào commit.

## Công việc đã thực hiện

- Bổ sung mô tả OpenAPI/Scalar cho các endpoint Category: thời gian cache của danh sách, quyền Admin, mã lỗi `CATEGORY_NOT_FOUND`, `CATEGORY_NAME_EXISTS`, `CATEGORY_DELETE_HAS_RECIPES`, `VALIDATION_ERROR` và các mã HTTP tương ứng. Các endpoint và CRUD cơ bản đã tồn tại trên nhánh trước lần làm việc này.
- Mở rộng integration tests cho 5 yêu cầu FR-CAT-001 đến FR-CAT-005: danh sách công khai, chi tiết và phân trang không hợp lệ, tạo và phân quyền, sửa giữ slug và làm mới cache danh sách, xóa có công thức bị từ chối rồi soft delete và làm mới cache. Thêm kiểm tra tài liệu OpenAPI cho response và mã lỗi.
- Bắt đầu giao diện Admin Category tại `/admin/categories`: danh sách, trạng thái tải/rỗng/lỗi, biểu mẫu tạo và sửa, xác thực trường cơ bản, phản hồi khi lưu, giao diện đáp ứng màn hình nhỏ.
- Sửa hợp đồng `CategoryDto` phía frontend từ `publishedRecipeCount` thành `recipeCount` để khớp JSON thực tế của API.
- Đổi URL API mặc định phía frontend sang `/api/v1` cùng origin và thêm rewrite Next.js đến API khi chạy frontend độc lập; khi chạy qua Nginx, `/api` đã được proxy trực tiếp đến API. Biến `NEXT_PUBLIC_API_BASE_URL` vẫn cho phép cấu hình ghi đè.

## Kiểm tra

- `dotnet build CulinaryBlog.sln -c Release --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test tests/CulinaryBlog.Integration.Tests/CulinaryBlog.Integration.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CategoryEndpointTests`: 6/6 đạt.
- `dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj -c Release --no-build`: 76/76 đạt.
- Frontend: `npm run typecheck`, `npm run lint`, `npm run build` đều thành công; route `/admin/categories` được build.
- Bộ integration chung: 30/32 đạt. Hai ca `ApiSmokeTests.CreateRecipeAsAuthorReturnsCreated` và `ApiSmokeTests.GetRecipeDetailReturnsCreatedRecipe` trong module Recipe nhận HTTP 500 thay vì 201. Chưa xác định nguyên nhân trong lần làm việc này; cần chủ sở hữu Recipe hoặc nhánh tích hợp kiểm tra trước khi coi toàn bộ suite đã đạt.

## Giới hạn và bước tiếp theo

- Frontend chưa có provider phiên đăng nhập dùng chung. Biểu mẫu Admin hiện yêu cầu nhập access token Admin, chỉ giữ trong state của trang. Khi luồng đăng nhập frontend được bàn giao, thay phần nhập token bằng phiên đăng nhập chung.
- Chưa triển khai thao tác xóa trong giao diện; API xóa và integration test đã có. Issue yêu cầu bắt đầu danh sách/biểu mẫu Admin, nên phần UI hiện tập trung vào xem, tạo và sửa.
- Cache 60 phút của danh sách Category và việc xóa cache sau Create/Update/Delete đã có trong code từ trước; các integration test mới xác nhận làm mới danh sách sau Update/Delete. Chi tiết Category chứa danh sách công thức theo người dùng và phân trang, nên hiện được đọc trực tiếp để không dùng chung dữ liệu cá nhân.
- Đã được phép push lên nhánh cá nhân. Sau khi push, cần kiểm tra CI và gắn link commit/PR vào issue trước khi đóng.

## Khắc phục môi trường chạy ngày 03/10/2026

- Khi chạy bằng Docker, đăng nhập Admin ban đầu trả HTTP 500 vì database trong volume cũ chưa có các bảng `AspNetUsers` và `Categories` (chỉ có `__EFMigrationsHistory`).
- Cổng `5432` trên Windows đang có một PostgreSQL riêng, nên `dotnet ef database update` chạy trực tiếp trên máy trỏ sai server. Đã sinh SQL idempotent từ 5 migration và thực thi trong container `culinary-blog-postgres-1`.
- Đồng bộ mật khẩu role `culinary` trong PostgreSQL container với giá trị cấu hình Compose hiện tại để kết nối TCP nhất quán. Không xóa volume hoặc dữ liệu.
- Khởi động lại API để seed tài khoản Admin. Đã xác nhận đăng nhập Admin thành công, API trả 3 danh mục, `/health/ready` trả 200 và `/admin/categories` trả 200.
