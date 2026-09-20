# Kế hoạch triển khai Culinary Blog

## Nguyên tắc bàn giao

- `main` luôn build được và là nền kiến trúc dùng chung.
- Kiến trúc, hợp đồng dùng chung, migration nền, Docker, CI và tài liệu được tích hợp trước module nghiệp vụ.
- Mỗi phần thay đổi độc lập có một commit riêng, theo Conventional Commits.
- Mỗi thành viên tự tạo nhánh cá nhân từ `main` khi bắt đầu module; nhóm trưởng không tạo sẵn nhánh cho thành viên.
- Module thành viên phát triển trên nhánh cá nhân và không merge vào `main` trong giai đoạn chấm riêng.
- Không commit secret. Cấu hình mẫu nằm trong `.env.example`; secret thật được cấp qua environment variables hoặc GitHub Actions secrets.

## Giai đoạn 1 – Nền kiến trúc trên `main`

1. Chuẩn hóa SRS và ghi Architecture Decision Records.
2. Dựng solution .NET Clean Architecture: Domain, Application, Infrastructure, API.
3. Dựng Next.js 15 App Router shell và shared API client.
4. Dựng PostgreSQL, Redis, MinIO, MailHog, Seq và reverse proxy bằng Docker Compose.
5. Thêm error handling RFC 7807, correlation ID, health checks, structured logging và OpenTelemetry foundation.
6. Thêm unit/integration/architecture test projects.
7. Thêm CI kiểm tra backend, frontend, formatting và dependency rules.

Điều kiện hoàn thành: restore/build/test thành công, Docker Compose hợp lệ, không có secret trong Git và tài liệu setup đủ để thành viên bắt đầu module.

## Giai đoạn 2 – Module của thành viên

Mỗi thành viên cập nhật `main`, tự tạo nhánh cá nhân rồi thực hiện đúng thứ tự dưới đây. Tên nhánh khuyến nghị chỉ để thống nhất cách đặt tên; thành viên chịu trách nhiệm tự tạo và quản lý nhánh của mình.

### Bo – FR-AUTH và quản lý người dùng

Nhánh khuyến nghị: `feature/fr-auth-bo`.

1. Thêm ASP.NET Core Identity, entity người dùng và `RefreshToken`; tạo EF Core configuration và migration của module.
2. Cài đặt đăng ký, đăng nhập email/mật khẩu và validation tương ứng.
3. Cài đặt JWT access token, refresh token rotation và logout/revocation.
4. Tích hợp Google Identity Services theo ranh giới tại ADR-0003.
5. Thêm RBAC, resource-based authorization và abstraction `ICurrentUser` dùng chung.
6. Cài đặt xem/cập nhật hồ sơ cá nhân.
7. Hoàn thiện unit test, integration test, tài liệu API và giao diện Auth/Profile.

Đầu ra cần bàn giao cho module khác: kiểu `AuthorId`, các role/policy, `ICurrentUser` và cơ chế bảo vệ endpoint.

### Trường – FR-RCP và FR-FILE

Nhánh khuyến nghị: `feature/fr-rcp-file-truong`.

1. Xây dựng Recipe aggregate, Nutrition, Step, Ingredient, Image và các enum/invariant.
2. Tạo EF Core configuration; dùng `CategoryId` và `AuthorId` dạng scalar để giảm phụ thuộc chéo giữa module.
3. Cài đặt CRUD Recipe, xem danh sách/chi tiết và vòng đời Draft/Published/Archived.
4. Cài đặt optimistic concurrency bằng PostgreSQL `xmin` theo ADR-0002.
5. Cài đặt CRUD Steps và Ingredients.
6. Tích hợp MinIO: upload, xóa và đặt ảnh chính; phát contract/event cho tác vụ thumbnail.
7. Hoàn thiện authorization Owner/Admin, unit test, integration test, migration và giao diện Recipe/File.

Phần migration có khóa ngoại đến Category/User chỉ chốt sau khi hợp đồng `CategoryId` và `AuthorId` ổn định.

### Kiên – FR-SRCH và FR-JOB

Nhánh khuyến nghị: `feature/fr-srch-job-kien`.

1. Chốt query contract dùng chung: bộ lọc, sắp xếp, `PagedResult<T>` và giới hạn page/pageSize.
2. Cài đặt danh sách, lọc, sắp xếp và phân trang trên schema Recipe ổn định.
3. Cài đặt PostgreSQL Full-Text Search với `tsvector`, `tsquery`, `unaccent` và GIN index.
4. Thêm migration/index/trigger tìm kiếm sau migration Recipe để tránh xung đột schema.
5. Tích hợp Hangfire và persistence PostgreSQL.
6. Cài đặt email chào mừng, tạo thumbnail và sinh sitemap qua contract/event của Auth, Recipe và File.
7. Hoàn thiện retry/idempotency, unit test, integration test và giao diện tìm kiếm.

Kiên có thể chuẩn bị abstraction và job skeleton sớm, nhưng chỉ hoàn thiện FTS/thumbnail/sitemap sau khi schema và event của các module nguồn ổn định.

### Hiếu – nền tảng, FR-CAT và FR-OBS

Nhánh làm việc do nhóm trưởng tự quản lý; phần nền dùng chung và các mốc tích hợp chính được đưa lên `main`.

1. Hoàn thiện nền Clean Architecture, Docker Compose, migration baseline, middleware, CI/CD và tài liệu bàn giao.
2. Xây dựng Category aggregate, EF Core configuration và migration.
3. Cài đặt CRUD Categories, cache invalidation và phân quyền Admin.
4. Hoàn thiện giao diện quản trị Categories.
5. Hoàn thiện health checks, structured logging, tracing và dashboard quan sát.
6. Chốt các contract dùng chung mà Recipe, Search và Jobs cần sử dụng; kiểm tra build/test của baseline.

Nhóm trưởng triển khai FR-CAT và FR-OBS sau khi nền kiến trúc ổn định; mỗi module vẫn được chia thành commit backend, test và frontend riêng để dễ review.

## Thứ tự tích hợp khuyến nghị

1. Hiếu bàn giao baseline trên `main`; mỗi thành viên tự tạo nhánh từ commit baseline này.
2. Bo hoàn thiện contract Identity/Authorization và Hiếu hoàn thiện FR-CAT; hai phần này có thể làm song song.
3. Trường chốt FR-RCP + FR-FILE sau khi biết contract `AuthorId`, policy và `CategoryId`.
4. Kiên hoàn thiện FR-SRCH sau khi schema Recipe ổn định.
5. Kiên hoàn thiện FR-JOB sau khi có event/contract từ Auth, Recipe và File.
6. Mỗi thành viên tự hoàn thiện test, tài liệu và giao diện trên nhánh của mình; cuối cùng thực hiện hardening và end-to-end tests theo phạm vi chấm.

## Quy ước commit

- `docs:` đặc tả, ADR và hướng dẫn.
- `build:` solution, dependency, Docker và công cụ build.
- `feat:` capability chạy được cho người dùng hoặc hệ thống.
- `test:` test mới hoặc thay đổi test.
- `ci:` GitHub Actions và kiểm tra chất lượng.
- `fix:` sửa lỗi hành vi đã có.
