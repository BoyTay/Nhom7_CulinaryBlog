# Kế hoạch triển khai Culinary Blog

## Nguyên tắc bàn giao

- `main` luôn build được và là nền kiến trúc dùng chung.
- Kiến trúc, hợp đồng dùng chung, migration nền, Docker, CI và tài liệu được tích hợp trước module nghiệp vụ.
- Mỗi phần thay đổi độc lập có một commit riêng, theo Conventional Commits.
- Module thành viên phát triển trên nhánh riêng và không merge vào `main` trong giai đoạn bàn giao này.
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

## Giai đoạn 2 – Module theo nhánh thành viên

| Nhánh | Phụ trách | Phạm vi |
|---|---|---|
| `feature/fr-auth-bo` | Bo | FR-AUTH: Identity, JWT, refresh rotation, Google ID token, RBAC/resource authorization, profile. |
| `feature/fr-rcp-file-truong` | Trường | FR-RCP + FR-FILE: recipe aggregate, steps, ingredients, images, MinIO và concurrency `xmin`. |
| `feature/fr-srch-job-kien` | Kiên | FR-SRCH + FR-JOB: PostgreSQL FTS, lọc/sắp xếp/phân trang, Hangfire jobs. |

Nhóm trưởng triển khai FR-CAT và FR-OBS sau khi nền kiến trúc ổn định; mỗi module vẫn được chia thành commit backend, test và frontend riêng để dễ review.

## Thứ tự tích hợp khuyến nghị

1. FR-AUTH vì các module ghi dữ liệu cần identity và authorization.
2. FR-CAT vì Recipe phụ thuộc Category.
3. FR-RCP + FR-FILE.
4. FR-SRCH sau khi schema Recipe ổn định.
5. FR-JOB và các tác vụ hậu xử lý.
6. Hardening: performance, security, accessibility, SEO và end-to-end tests.

## Quy ước commit

- `docs:` đặc tả, ADR và hướng dẫn.
- `build:` solution, dependency, Docker và công cụ build.
- `feat:` capability chạy được cho người dùng hoặc hệ thống.
- `test:` test mới hoặc thay đổi test.
- `ci:` GitHub Actions và kiểm tra chất lượng.
- `fix:` sửa lỗi hành vi đã có.

