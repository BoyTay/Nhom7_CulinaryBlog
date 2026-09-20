<div align="center">

# 🍳 Culinary Blog

**Nền tảng chia sẻ công thức nấu ăn hiện đại**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-15-000000?style=for-the-badge&logo=nextdotjs)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![MinIO](https://img.shields.io/badge/MinIO-Object%20Storage-C72E49?style=for-the-badge&logo=minio&logoColor=white)](https://min.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)

**Môn học: Advanced Web Application Development**

**Trường Đại học Đà Lạt – Học kỳ 1, Năm học 2026–2027**

</div>

---

## 📖 Giới thiệu

**Culinary Blog** là một nền tảng web full-stack cho phép người dùng chia sẻ, khám phá và lưu trữ các công thức nấu ăn. Dự án được xây dựng theo kiến trúc **Clean Architecture**, kết hợp **CQRS** và **Vertical Slice** ở backend, sử dụng **Next.js 15 App Router** ở frontend.

### ✨ Tính năng chính

| Module | Tính năng |
|---|---|
| 🔐 **Xác thực** | Đăng ký/đăng nhập qua Email và Google OAuth 2.0, JWT + Refresh Token Rotation |
| 👤 **Người dùng** | Quản lý hồ sơ cá nhân, phân quyền RBAC và Resource-Based |
| 🗂️ **Danh mục** | CRUD danh mục món ăn dành cho Admin |
| 📝 **Công thức** | CRUD công thức với hình ảnh, nguyên liệu, hướng dẫn từng bước và thông tin dinh dưỡng |
| 🖼️ **Tệp tin** | Upload ảnh lên MinIO, đặt ảnh chính, xóa ảnh và tạo thumbnail |
| 🔍 **Tìm kiếm** | Full-Text Search hỗ trợ tiếng Việt với `tsvector`, `tsquery`, GIN index và `unaccent` |
| ⚙️ **Background Jobs** | Hangfire: gửi email chào mừng, tạo thumbnail và sinh sitemap |
| 📊 **Quan sát** | Health Checks, Structured Logging và Distributed Tracing |

---

## 📂 Cấu trúc Solution (Clean Architecture)

Dự án áp dụng **Clean Architecture**, CQRS và Vertical Slice. `main` chỉ giữ nền dùng chung; các module nghiệp vụ được phát triển trên nhánh phụ trách riêng.

```text
CulinaryBlog.sln
├── src/
│   ├── CulinaryBlog.Domain/          <- Entities, value objects, domain events
│   ├── CulinaryBlog.Application/     <- CQRS contracts, validation, use cases
│   ├── CulinaryBlog.Infrastructure/  <- EF Core, PostgreSQL, external services
│   ├── CulinaryBlog.API/             <- Minimal APIs, middleware, observability
│   └── CulinaryBlog.Web/             <- Next.js 15 App Router
├── deploy/nginx/                      <- Reverse proxy
├── design-system/                     <- UI tokens và quy tắc accessibility
├── docs/adr/                          <- Architecture Decision Records
└── tests/
    ├── CulinaryBlog.Application.Tests/
    ├── CulinaryBlog.Architecture.Tests/
    └── CulinaryBlog.Integration.Tests/
```

## 🚀 Khởi động môi trường phát triển

Yêu cầu: .NET SDK 10, Node.js 22+, npm 11+ và Docker Compose v2.

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Sau khi các service healthy:

- Ứng dụng: `http://localhost`
- Scalar API reference: `http://localhost/scalar`
- MinIO Console: `http://localhost:9001`
- Seq: `http://localhost:5341`
- MailHog: `http://localhost:8025`

Chạy riêng khi phát triển:

```powershell
dotnet tool restore
dotnet build CulinaryBlog.sln
dotnet test CulinaryBlog.sln
dotnet run --project src/CulinaryBlog.API

Set-Location src/CulinaryBlog.Web
npm ci
npm run dev
```

Tạo migration mới sau khi thay đổi model:

```powershell
dotnet ef migrations add <MigrationName> --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.API --output-dir Persistence/Migrations
```

Kế hoạch triển khai và quyết định kiến trúc nằm tại [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md) và [`docs/adr/`](docs/adr/).

---

## 👥 Thành viên nhóm

| MSSV | Họ và tên | GitHub | Email | SĐT | Chức vụ |
|---|---|---|---|---|---|
| 2312616 | Phan Trung Hiếu | [@BoyTay](https://github.com/BoyTay) | 2312616@dlu.edu.vn | 0947636963 | 👑 Nhóm trưởng |
| 2312585 | Phùng Nguyễn Hoài Bo | [@HubertPhung](https://github.com/HubertPhung) | 2312585@dlu.edu.vn | 0988752291 | Thành viên |
| 2014503 | Phan Văn Nhật Trường | [@Girrint](https://github.com/Girrint) | 2014503@dlu.edu.vn | 0786660502 | Thành viên |
| 2312653 | Nguyễn Trung Kiên | [@Trug-Kin](https://github.com/Trug-Kin) | 2312653@dlu.edu.vn | 0919136592 | Thành viên |

---

## 📋 Phân công công việc

| Thành viên | Module/Chức năng phụ trách |
|---|---|
| **Hiếu** | **Nhóm trưởng – Nền tảng và Module Danh mục (FR-CAT)**<br>– Thiết lập Clean Architecture, Docker Compose, Database Migration, Middleware và CI/CD.<br>– Xây dựng CRUD Categories (Backend + Frontend Admin).<br>– Xây dựng module quan sát hệ thống: Health Checks, Logging và Tracing. |
| **Bo** | **Module Xác thực và Người dùng (FR-AUTH)**<br>– Đăng ký, đăng nhập bằng email và Google OAuth.<br>– Refresh Token Rotation và Logout.<br>– Phân quyền RBAC và Resource-Based.<br>– Quản lý hồ sơ cá nhân. |
| **Trường** | **Module Quản lý Công thức (FR-RCP) và Quản lý Tệp tin (FR-FILE)**<br>– CRUD Recipe, Steps, Ingredients và Images.<br>– Upload ảnh lên MinIO, đặt ảnh chính và xóa ảnh.<br>– Xử lý concurrency bằng RowVersion. |
| **Kiên** | **Module Tìm kiếm (FR-SRCH) và Background Jobs (FR-JOB)**<br>– Full-Text Search với `tsvector`, `tsquery`, GIN index và `unaccent`.<br>– Lọc, sắp xếp và phân trang.<br>– Hangfire: email chào mừng, thumbnail và sitemap. |

---

<div align="center">

Made with ❤️ by **Nhóm 7**  
Advanced Web Application Development 2026

</div>
