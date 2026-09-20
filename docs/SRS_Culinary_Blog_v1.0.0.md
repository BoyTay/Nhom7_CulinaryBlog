**GIÁO TRÌNH PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO** Phiên bản V4  ·  .NET 10 + Next.js App Router 

# **TÀI LIỆU ĐẶC TẢ YÊU CẦU PHẦN MỀM** 

_Software Requirements Specification (SRS) Tiêu chuẩn IEEE 830 / ISO/IEC/IEEE 29148:2018_ 

## **Dự án: Blog Ẩm thực và Nấu ăn** 

### **_Culinary Blog_** 

|**Phiên bản tài liệu**|1.0.1|
|---|---|
|**Ngày phát hành**|20/09/2026|
|**Trạng thái**|Đã duyệt (Approved)|
|**Công nghệ Backend**|.NET 10 Minimal APIs, C#|
|**Công nghệ Frontend**|Next.js App Router, TypeScript|
|**Cơ sở dữ liệu**|PostgreSQL 16|
|**Object Storage**|MinIO (S3-Compatible)|
|**Cache**|Redis 7|



_Tài liệu này được biên soạn theo tiêu chuẩn IEEE 830 / ISO/IEC/IEEE 29148:2018._ 
## **LỊCH SỬ THAY ĐỔI TÀI LIỆU** 

|**Phiên**<br>**bản**|**Ngày**|**Tác giả / Vai trò**|**Nội dung thay đổi**|**Trạng**<br>**thái**|
|---|---|---|---|---|
|1.0.1|20/09/2026|Nhóm trưởng / Kiến trúc sư|CR-001: chuẩn hóa hợp đồng API, OAuth, validation, soft delete, optimistic concurrency, dependency rule và phiên bản frontend.|Approved|
|1.0.0|04/06/2026|Senior BA / Architect|Phát hành lần đầu – Bản hoàn<br>chỉnh theo IEEE 830 / ISO 29148.|Approved|
|0.9.0|20/05/2026|Senior BA|Bổ sung Chương 7 (Data Model),<br>Chương 8 (API Spec) và Phụ lục.|Under<br>Review|
|0.8.0|05/05/2026|Senior BA|Hoàn thiện Chương 3 (FR), bổ<br>sung FR-FILE, FR-JOB, FR-OBS.|Draft|
|0.5.0|15/04/2026|Senior BA|Phác thảo ban đầu: Chương 1–4<br>(skeleton).|Draft|



**Phê duyệt tài liệu:** Tài liệu phiên bản 1.0.1 đã được Nhóm trưởng xác nhận ngày 20/09/2026. Mọi thay đổi tiếp theo phải thông qua quy trình Change Request (CR) và được cập nhật vào bảng lịch sử thay đổi.

### **CR-001 – Chuẩn hóa quyết định kiến trúc trước triển khai**

- Backend dùng .NET 10; frontend dùng Next.js 15 App Router, TypeScript và Node.js 22 trong Docker/CI.
- Đăng ký dùng hợp đồng `{ displayName, email, password }`; `UserName` nội bộ được sinh duy nhất từ email.
- Google OAuth dùng Google Identity Services ở frontend; backend nhận `idToken`, xác minh chữ ký/audience/issuer và phát hành token nội bộ.
- Lỗi cú pháp, binding và FluentValidation trả HTTP 400; lỗi quy tắc nghiệp vụ hoặc optimistic concurrency trả HTTP 422.
- Recipe và Category dùng soft delete. Dữ liệu con chỉ hard delete theo cascade khi bản ghi cha được dọn dẹp vật lý bởi tác vụ bảo trì.
- Optimistic concurrency trên PostgreSQL dùng system column `xmin`, ánh xạ thành concurrency token; không dùng SQL Server-style `byte[] RowVersion`.
- `RecipeImage` là entity/bảng riêng. Chỉ `RecipeNutrition` là owned entity nằm trong bảng `Recipes`.
- Policy `VerifiedAuthor` không thuộc MVP vì chưa có luồng xác nhận email. MVP dùng role `Author` và resource ownership; email confirmation là phần mở rộng sau.
- Domain chỉ phụ thuộc .NET BCL. FluentValidation, MediatR và các package orchestration nằm trong Application.
## **MỤC LỤC** 

|LỊCH SỬ THAY ĐỔI TÀI LIỆU .................................................................................................. 2|
|---|
|MỤC LỤC ................................................................................................................................... 3|
|CHƯƠNG 1. GIỚI THIỆU ......................................................................................................... 6|
|1.1. Mục đích Tài liệu ............................................................................................................. 6|
|1.2. Phạm vi Sản phẩm.......................................................................................................... 6|
|1.2.1. Tên và Định danh ..................................................................................................... 6|
|1.2.2. Mô tả Sản phẩm ....................................................................................................... 6|
|1.2.3. Những gì KHÔNG thuộc phạm vi............................................................................. 7|
|1.3. Định nghĩa, Từ viết tắt và Ký hiệu .................................................................................. 7|
|1.4. Tài liệu Tham chiếu......................................................................................................... 8|
|1.5. Tổng quan Tài liệu ........................................................................................................ 10|
|CHƯƠNG 2. MÔ TẢ TỔNG QUAN HỆ THỐNG .................................................................... 11|
|2.1. Bối cảnh Sản phẩm....................................................................................................... 11|
|2.1.1. Vị trí trong Hệ sinh thái ........................................................................................... 11|
|2.1.2. Quan hệ với Hệ thống Ngoài.................................................................................. 11|
|2.2. Chức năng Sản phẩm Tổng quát ................................................................................. 12|
|2.3. Các Lớp Người dùng và Đặc điểm............................................................................... 12|
|2.4. Môi trường Vận hành .................................................................................................... 13|
|2.4.1. Môi trường Server (Production) ............................................................................. 13|
|2.4.2. Môi trường Phát triển (Development) .................................................................... 13|
|2.4.3. Yêu cầu Trình duyệt Client..................................................................................... 14|
|2.5. Ràng buộc Thiết kế và Hiện thực ................................................................................. 14|
|2.6. Giả định và Phụ thuộc................................................................................................... 15|
|2.6.1. Giả định................................................................................................................... 15|
|2.6.2. Phụ thuộc Bên ngoài .............................................................................................. 15|
|CHƯƠNG 3. YÊU CẦU CHỨC NĂNG CHI TIẾT ................................................................... 17|
|3.1. Module Xác thực và Quản lý Người dùng (FR-AUTH) ................................................ 17|
|FR-AUTH-001: Đăng ký Tài khoản (User Registration)................................................... 17|
|FR-AUTH-002: Đăng nhập bằng Email/Mật khẩu (Local Login) ..................................... 18|
|FR-AUTH-003: Đăng nhập bằng Google OAuth 2.0 ....................................................... 19|
|FR-AUTH-004: Làm mới Access Token (Token Refresh) ............................................... 20|
|FR-AUTH-005: Đăng xuất (Logout / Token Revocation) ................................................. 21|
|FR-AUTH-006: Xem Hồ sơ Cá nhân (View Profile)......................................................... 22|
|FR-AUTH-007: Cập nhật Hồ sơ Cá nhân (Update Profile) ............................................. 23|
|3.2. Module Quản lý Danh mục (FR-CAT) .......................................................................... 23|
|FR-CAT-001: Xem Danh sách Danh mục........................................................................ 23|
|FR-CAT-002: Xem Chi tiết Danh mục và Công thức ....................................................... 24|
|FR-CAT-003: Tạo Danh mục Mới [Admin]....................................................................... 25|


|FR-CAT-004: Cập nhật Danh mục [Admin] ..................................................................... 26|
|---|
|FR-CAT-005: Xóa Danh mục [Admin] .............................................................................. 26|
|3.3. Module Quản lý Công thức Nấu ăn (FR-RCP)............................................................. 27|
|FR-RCP-001: Xem Danh sách Công thức (Paginated + Filtered + Sorted) ................... 27|
|FR-RCP-002: Xem Chi tiết Công thức ............................................................................. 28|
|FR-RCP-003: Tạo Công thức Nấu ăn Mới [Author/Admin] ............................................. 29|
|FR-RCP-004: Cập nhật Công thức [Author-Owner/Admin] ............................................. 30|
|FR-RCP-005: Xuất bản / Hủy Xuất bản Công thức ......................................................... 31|
|FR-RCP-006: Lưu trữ Công thức (Archive) ..................................................................... 32|
|FR-RCP-007: Xóa Công thức [Author-Owner/Admin] ..................................................... 32|
|FR-RCP-008: Quản lý Ảnh Công thức (Upload / Set Primary / Delete) .......................... 33|
|FR-RCP-009: Quản lý Nguyên liệu (CRUD RecipeIngredient) ....................................... 34|
|FR-RCP-010: Quản lý Các bước Thực hiện (CRUD RecipeStep).................................. 35|
|3.4. Module Tìm kiếm và Phân trang (FR-SRCH) ............................................................... 36<br>|
|FR-SRCH-001: Tìm kiếm Toàn văn bản (Full-Text Search)............................................ 36|
|FR-SRCH-002/003/004: Lọc, Sắp xếp và Phân trang (Tóm tắt) ..................................... 37|
|3.5. Module Quản lý Tệp tin (FR-FILE) ............................................................................... 37|
|3.6. Module Background Jobs (FR-JOB)............................................................................. 38|
|3.7. Module Quan sát Hệ thống (FR-OBS).......................................................................... 39|
|4. Yêu cầu Phi Chức năng (NFR)............................................................................................ 40|
|4.1. Hiệu năng (NFR-PERF) ................................................................................................ 40|
|4.2. Bảo mật (NFR-SEC) ..................................................................................................... 41|
|4.3. Khả năng Sử dụng (NFR-USE) .................................................................................... 42|
|4.4. Độ tin cậy (NFR-REL) ................................................................................................... 42|
|4.5. Khả năng Bảo trì (NFR-MAINT).................................................................................... 43|
|4.6. Khả năng Mở rộng (NFR-SCALE) ................................................................................ 44|
|4.7. Tối ưu SEO (NFR-SEO) ............................................................................................... 44|
|5. Yêu cầu Giao diện Ngoài ..................................................................................................... 46|
|5.1. Giao diện Người dùng (UI) ........................................................................................... 46|
|5.2. Giao diện Phần mềm – REST API ............................................................................... 47|
|5.3. Giao diện Dịch vụ Bên thứ ba....................................................................................... 47|
|5.4. Giao diện Phần cứng .................................................................................................... 48|
|6. Kiến trúc Hệ thống ............................................................................................................... 50|
|6.1. Tổng quan Kiến trúc...................................................................................................... 50|
|6.2. Kiến trúc Backend – Clean Architecture....................................................................... 50|
|6.3. CQRS + MediatR Pipeline ............................................................................................ 51|
|6.4. Mô hình Quan hệ Thực thể (ERD tóm tắt) ................................................................... 52|
|6.5. Triển khai – Docker Compose ...................................................................................... 52|
|7. Mô hình Dữ liệu ................................................................................................................... 54|


|7.1. BaseEntity (Abstract) .................................................................................................... 54|
|---|
|7.2. Recipe ........................................................................................................................... 54|
|7.2.1. RecipeNutrition (Owned Entity — cột trong bảng Recipes) .................................. 56|
|7.3. RecipeStep .................................................................................................................... 57|
|7.4. RecipeIngredient ........................................................................................................... 57|
|7.5. RecipeImage ................................................................................................................. 58|
|7.6. Category ........................................................................................................................ 58|
|7.7. ApplicationUser (extends IdentityUser) ........................................................................ 58|
|7.8. RefreshToken................................................................................................................ 59|
|8. Đặc tả REST API ................................................................................................................. 61|
|8.1. Authentication Module (/auth) ....................................................................................... 61|
|8.2. Categories Module (/categories)................................................................................... 62|
|8.3. Recipes Module (/recipes) ............................................................................................ 63|
|8.4. Recipe Images (/recipes/{id}/images) ........................................................................... 64|
|8.5. Recipe Steps (/recipes/{id}/steps) ................................................................................ 64|
|8.6. Recipe Ingredients (/recipes/{id}/ingredients)............................................................... 65|
|8.7. Health Check Endpoints ............................................................................................... 65|
|Phụ lục A – HTTP Status Codes ............................................................................................. 67|
|Phụ lục B – Application Error Codes ....................................................................................... 67|
|Phụ lục C – Từ điển Thuật ngữ ............................................................................................... 69|


## **CHƯƠNG 1. GIỚI THIỆU** 

#### **1.1. Mục đích Tài liệu** 

Tài liệu Đặc tả Yêu cầu Phần mềm (Software Requirements Specification – SRS) này được biên soạn theo tiêu chuẩn IEEE 830-1998 và ISO/IEC/IEEE 29148:2018 nhằm mô tả đầy đủ, chính xác và nhất quán toàn bộ yêu cầu chức năng (Functional Requirements) và yêu cầu phi chức năng (Non-Functional Requirements) của dự án ứng dụng web Blog Ẩm thực và Nấu ăn (Culinary Blog). 

Tài liệu này phục vụ các đối tượng sau: 

- **Nhóm phát triển Backend (.NET 10/C#):** Căn cứ thiết kế API, domain model, và business rules. 

- **Nhóm phát triển Frontend (Next.js/TypeScript):** Căn cứ thiết kế giao diện, luồng người dùng và tích hợp API. 

- **Kỹ sư Kiểm thử (QA/QC):** Cơ sở xây dựng test cases, kiểm thử chấp nhận (acceptance testing). 

- **Kiến trúc sư Hệ thống:** Tham chiếu khi đưa ra quyết định kiến trúc (architecture decisions). 

- **Giảng viên và Sinh viên:** Tài liệu học thuật mẫu cho dự án thực hành xuyên suốt giáo trình. 

- **Stakeholder / Product Owner:** Phê duyệt phạm vi và ưu tiên tính năng. 

**Phạm vi hiệu lực:** Tài liệu này có hiệu lực từ phiên bản 1.0.0 và là tài liệu nền tảng (baseline) cho toàn bộ vòng đời phát triển dự án. Mọi thay đổi yêu cầu sau khi tài liệu được phê duyệt phải tuân theo quy trình quản lý thay đổi (Change Management Process). 

#### **1.2. Phạm vi Sản phẩm** 

##### **1.2.1. Tên và Định danh** 

|**Thuộc tính**|**Giá trị**|
|---|---|
|Tên sản phẩm|Culinary Blog – Blog Ẩm thực và Nấu ăn|
|Định danh dự án|CULINARY-BLOG-V1|
|Loại hệ thống|Ứng dụng Web Full-Stack (API-Driven Architecture)|
|Phiên bản sản phẩm|1.0.0|
|Môi trường đích|Cloud/On-premise (Docker Compose + Nginx)|



##### **1.2.2. Mô tả Sản phẩm** 

Culinary Blog là một nền tảng web cho phép người dùng chia sẻ, khám phá và lưu trữ các công thức nấu ăn từ nhiều nền ẩm thực khác nhau. Ứng dụng cung cấp hệ sinh thái hoàn chỉnh bao gồm: 

- **Nền tảng chia sẻ công thức:** Tác giả (Author) đăng tải công thức với hình ảnh, danh sách nguyên liệu chi tiết, hướng dẫn từng bước thực hiện và thông tin dinh dưỡng. 
- **Tổ chức nội dung:** Phân loại công thức theo danh mục (Category), độ khó (Difficulty Level), thời gian chuẩn bị và nấu. 

- **Tìm kiếm thông minh:** Full-Text Search tiếng Việt sử dụng PostgreSQL tsvector/tsquery với unaccent extension. 

- **Bảo mật đa lớp:** Xác thực JWT stateless, phân quyền theo vai trò (RBAC) và theo tài nguyên (Resource-Based Authorization), đăng nhập Google OAuth 2.0. 

- **Tối ưu hiệu năng và SEO:** Redis distributed cache, Next.js ISR, Open Graph Protocol, JSON-LD Schema.org Recipe markup. 

- **Quan sát hệ thống:** Structured logging (Serilog), distributed tracing (OpenTelemetry), health check endpoints. 

##### **1.2.3. Những gì KHÔNG thuộc phạm vi** 

Các tính năng sau đây nằm ngoài phạm vi phiên bản 1.0.0: 

- Hệ thống bình luận (Comment System) và đánh giá sao (Rating System). 

- Tính năng lưu/đánh dấu công thức yêu thích (Bookmark/Favorite). 

- Thông báo real-time (SignalR/WebSocket). 

- Ứng dụng di động native (iOS/Android). 

- Thanh toán / Tính năng thương mại điện tử. 

- • Hệ thống nhắn tin trực tiếp giữa người dùng. 

- GraphQL API (định hướng sau khóa học). 

#### **1.3. Định nghĩa, Từ viết tắt và Ký hiệu** 

|**Thuật ngữ / Viết tắt**|**Định nghĩa đầy đủ**|
|---|---|
|SRS|Software Requirements Specification – Đặc tả Yêu cầu Phần<br>mềm.|
|FR|Functional Requirement – Yêu cầu chức năng.|
|NFR|Non-Functional Requirement – Yêu cầu phi chức năng.|
|API|Application Programming Interface – Giao diện lập trình ứng dụng.|
|REST|Representational State Transfer – Kiểu kiến trúc API phổ biến<br>nhất.|
|JWT|JSON Web Token – Chuẩn token xác thực stateless (RFC 7519).|
|RBAC|Role-Based Access Control – Kiểm soát truy cập dựa trên vai trò.|
|CQRS|Command Query Responsibility Segregation – Pattern tách biệt<br>lệnh và truy vấn.|
|DDD|Domain-Driven Design – Phương pháp thiết kế phần mềm lấy<br>domain làm trung tâm.|
|ORM|Object-Relational Mapper – Công cụ ánh xạ object-database (EF<br>Core).|
|FTS|Full-Text Search – Tìm kiếm toàn văn bản.|
|ISR|Incremental Static Regeneration – Kỹ thuật tái tạo trang tĩnh của<br>Next.js.|


|**Thuật ngữ / Viết tắt**|**Định nghĩa đầy đủ**|
|---|---|
|LCP|Largest Contentful Paint – Core Web Vital đo tốc độ tải nội dung<br>lớn nhất.|
|CLS|Cumulative Layout Shift – Core Web Vital đo độ ổn định bố cục<br>trang.|
|INP|Interaction to Next Paint – Core Web Vital đo thời gian phản hồi<br>tương tác.|
|CI/CD|Continuous Integration / Continuous Delivery – Tích hợp và triển<br>khai liên tục.|
|DXA|Device-independent pixel unit used in OOXML (1 inch = 1440<br>DXA).|
|TTL|Time-To-Live – Thời gian sống của dữ liệu trong cache.|
|SSR|Server-Side Rendering – Render HTML trên server.|
|SSG|Static Site Generation – Tạo trang tĩnh lúc build time.|
|MoSCoW|Must Have / Should Have / Could Have / Won't Have – Mô hình<br>phân loại ưu tiên.|
|RFC|Request For Comments – Tài liệu tiêu chuẩn kỹ thuật (e.g., RFC<br>7807).|
|ERD|Entity Relationship Diagram – Sơ đồ quan hệ thực thể.|
|PBKDF2|Password-Based Key Derivation Function 2 – Thuật toán hash mật<br>khẩu an toàn.|
|CDN|Content Delivery Network – Mạng phân phối nội dung.|
|MIME|Multipurpose Internet Mail Extensions – Chuẩn định dạng tệp trên<br>Internet.|
|JSON-LD|JavaScript Object Notation for Linked Data – Định dạng dữ liệu có<br>cấu trúc cho SEO.|



#### **1.4. Tài liệu Tham chiếu** 

|**STT**|**Tài liệu / Tiêu**<br>**chuẩn**|**Nguồn / URL**|
|---|---|---|
|1|IEEE Std 830-<br>1998 –<br>Recommended<br>Practice for<br>Software<br>Requirements<br>Specifications|https://ieeexplore.ieee.org/document/720574|
|2|ISO/IEC/IEEE<br>29148:2018 –<br>Requirements<br>Engineering|https://www.iso.org/standard/72089.html|
|3|OWASP Top<br>10:2021 – Top 10|https://owasp.org/www-project-top-ten/|


|**STT**|**Tài liệu / Tiêu**<br>**chuẩn**|**Nguồn / URL**|
|---|---|---|
||Web Application<br>Security Risks||
|4|RFC 7807 –<br>Problem Details<br>for HTTP APIs|https://datatracker.ietf.org/doc/html/rfc7807|
|5|RFC 7519 –<br>JSON Web Token<br>(JWT)|https://datatracker.ietf.org/doc/html/rfc7519|
|6|RFC 6749 – The<br>OAuth 2.0<br>Authorization<br>Framework|https://datatracker.ietf.org/doc/html/rfc6749|
|7|.NET 10 Minimal<br>APIs – Microsoft<br>Learn|https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis|
|8|ASP.NET Core<br>Identity –<br>Microsoft Learn|https://learn.microsoft.com/aspnet/core/security/authentication/identity|
|9|Entity Framework<br>Core 10<br>Documentation|https://learn.microsoft.com/ef/core/|
|10|Next.js 15 App<br>Router<br>Documentation|https://nextjs.org/docs|
|11|PostgreSQL 16<br>Documentation –<br>Full-Text Search|https://www.postgresql.org/docs/16/textsearch.html|
|12|Redis 7<br>Documentation|https://redis.io/docs/|
|13|MinIO S3-<br>Compatible<br>Object Storage|https://min.io/docs/|
|14|Google Web<br>Vitals – Core Web<br>Vitals|https://web.dev/explore/learn-core-web-vitals|
|15|Schema.org<br>Recipe –<br>Structured Data|https://schema.org/Recipe|
|16|OpenTelemetry<br>.NET<br>Documentation|https://opentelemetry.io/docs/languages/dotnet/|
|17|Serilog<br>Documentation|https://serilog.net/|
|18|Hangfire<br>Documentation|https://docs.hangfire.io/|


|**STT**|**Tài liệu / Tiêu**<br>**chuẩn**|**Nguồn / URL**|
|---|---|---|
|19|FluentValidation<br>Documentation|https://docs.fluentvalidation.net/|
|20|Giáo trình Phát<br>triển Ứng dụng<br>Web Nâng cao V4<br>– Nội bộ|N/A (tài liệu nội bộ)|



#### **1.5. Tổng quan Tài liệu** 

Tài liệu SRS này được tổ chức thành 8 chương chính và 3 phụ lục, theo cấu trúc từ tổng quan đến chi tiết: 

- **Chương 2 – Mô tả Tổng quan:** Bối cảnh sản phẩm, chức năng tóm tắt, các lớp người dùng, môi trường vận hành và ràng buộc thiết kế. 

- **Chương 3 – Yêu cầu Chức năng:** 27 FR được đặc tả chi tiết theo format chuẩn, nhóm thành 7 module chức năng. 

- **Chương 4 – Yêu cầu Phi chức năng:** Hiệu năng, bảo mật, khả năng sử dụng, độ tin cậy, khả năng bảo trì/mở rộng và SEO. 

- **Chương 5 – Giao diện Ngoài:** Tích hợp với các hệ thống và dịch vụ ngoài (Google OAuth, MinIO, Redis, SendGrid). 

- **Chương 6 – Kiến trúc Hệ thống:** Clean Architecture Backend, Next.js App Router Frontend, chiến lược caching và deployment. 

- **Chương 7 – Mô hình Dữ liệu:** ERD mô tả văn bản và bảng định nghĩa chi tiết từng entity/table. 

- **Chương 8 – Đặc tả API REST:** Quy ước, chuẩn lỗi RFC 7807, và bảng tổng hợp tất cả ~30 endpoint. 

- **Phụ lục A-C:** HTTP Status Codes, Application Error Codes, và Từ điển thuật ngữ. 
## **CHƯƠNG 2. MÔ TẢ TỔNG QUAN HỆ THỐNG** 

#### **2.1. Bối cảnh Sản phẩm** 

##### **2.1.1. Vị trí trong Hệ sinh thái** 

Culinary Blog vận hành theo mô hình API-Driven Architecture, trong đó Backend (.NET 10) và Frontend (Next.js) là hai hệ thống độc lập giao tiếp hoàn toàn qua HTTP/JSON RESTful API. Không có server-side rendering truyền thống (MVC Razor/Blazor) hay shared view engine giữa hai tầng. 

Sơ đồ bối cảnh hệ thống (Context Diagram): 



<!-- Start of picture text -->
┌─────────────────────────────────────────────────────────────────┐<br>│                    CULINARY BLOG SYSTEM                         │<br>│                                                                 │<br>│   ┌──────────────────┐        ┌───────────────────────────────┐  │<br>│   │  NEXT.JS FRONTEND│◄──────►│    .NET 10 BACKEND API        │  │<br>│   │  (App Router)    │  REST  │    (Minimal APIs + Clean Arch) │  │<br>│   │  Port: 3000      │  JSON  │    Port: 5000                 │  │<br>│   └──────────────────┘        └──────────────┬────────────────┘  │<br>│                                             │                  │<br>│   ┌──────┐ ┌────────┐  ┌────────┐  ┌────────┐ ┌───────────┐   │<br>│   │ Pgsql│ │ Redis  │  │ MinIO  │  │Hangfire│ │Google Auth│   │<br>│   │:5432 │ │:6379   │  │:9000   │  │ Jobs   │ │ OAuth2.0  │   │<br>│   └──────┘ └────────┘  └────────┘  └────────┘ └───────────┘   │<br>└─────────────────────────────────────────────────────────────────┘<br><!-- End of picture text -->

_Hình 2.1. Sơ đồ bối cảnh hệ thống Culinary Blog_ 

##### **2.1.2. Quan hệ với Hệ thống Ngoài** 

|**Hệ thống Ngoài**|**Vai trò**|**Giao thức / Chuẩn**|**Hướng tích hợp**|
|---|---|---|---|
|PostgreSQL 16|Hệ quản trị CSDL quan<br>hệ chính (RDBMS)|TCP + Npgsql Driver<br>(EF Core)|Backend → PostgreSQL|
|Redis 7|Distributed Cache &<br>Session Store|TCP +<br>StackExchange.Redis|Backend → Redis|
|MinIO (S3)|Object Storage cho ảnh<br>công thức|HTTP/S3 API +<br>MinIO .NET SDK|Backend → MinIO|
|Google OAuth<br>2.0|Đăng nhập bên thứ ba<br>(Identity Provider)|HTTPS + OpenID<br>Connect|Client ↔ Google ↔<br>Backend|
|Hangfire|Background Job<br>Processing (embedded)|In-process (.NET)|Backend (internal)|
|Serilog / Seq|Structured Log<br>Aggregation<br>(development)|HTTP Sink → Seq|Backend → Seq|
|OpenTelemetry<br>Collector|Distributed Tracing &<br>Metrics (production)|OTLP / gRPC|Backend → Collector|


|**Hệ thống Ngoài**|**Vai trò**|**Giao thức / Chuẩn**|**Hướng tích hợp**|
|---|---|---|---|
|Nginx (Reverse<br>Proxy)|SSL termination, load<br>balancing, static serving|HTTP/HTTPS|Client → Nginx →<br>Services|



#### **2.2. Chức năng Sản phẩm Tổng quát** 

Culinary Blog cung cấp 7 nhóm chức năng chính, được hiện thực hóa qua 27 Functional Requirements chi tiết tại Chương 3: 

|**Nhóm chức năng**|**Mã nhóm**|**Số**<br>**FR**|**Mô tả tóm tắt**|
|---|---|---|---|
|Xác thực & Quản lý<br>Người dùng|FR-AUTH|7|Đăng ký, đăng nhập (email + Google), JWT<br>refresh token, logout, quản lý profile.|
|Quản lý Danh mục|FR-CAT|5|CRUD danh mục công thức (Category) – phân<br>quyền Admin.|
|Quản lý Công thức nấu<br>ăn|FR-RCP|10|CRUD recipe, publish/archive, quản lý<br>ảnh/bước/nguyên liệu.|
|Tìm kiếm & Phân trang|FR-SRCH|4|Full-Text Search (PostgreSQL), filter, sort,<br>offset pagination.|
|Quản lý Tệp tin|FR-FILE|2|Upload/Delete ảnh trên MinIO S3-compatible.|
|Background Jobs|FR-JOB|3|Email chào mừng, thumbnail generation,<br>sitemap XML (Hangfire).|
|Quan sát Hệ thống|FR-OBS|3|Health checks, structured logging, distributed<br>tracing.|



#### **2.3. Các Lớp Người dùng và Đặc điểm** 

Hệ thống định nghĩa 3 loại tác nhân (Actor) với quyền hạn khác nhau: 

|**Vai trò**|**Mô tả**|**Điều kiện**|**Quyền hạn chính**|**Ưu tiên**<br>**phục**<br>**vụ**|
|---|---|---|---|---|
|Khách (Guest<br>/ Anonymous)|Người dùng chưa<br>xác thực, truy cập<br>ứng dụng mà<br>không có tài<br>khoản.|Không cần tài<br>khoản|Xem danh sách & chi tiết<br>recipe (Published), xem<br>danh mục, tìm kiếm.<br>KHÔNG được tạo/sửa/xóa.|Cao<br>(đây là<br>đại đa<br>số<br>người<br>dùng)|
|Tác giả<br>(Author)|Người dùng đã<br>đăng ký và xác<br>thực thành công.<br>Được tự động gán<br>khi đăng ký.|Có tài khoản &<br>JWT hợp lệ|+ Tất cả quyền của Guest.<br>+ Tạo/sửa/xóa recipe CỦA<br>MÌNH. + Upload ảnh, quản<br>lý steps/ingredients. +<br>Publish/Archive recipe của<br>mình.|Cao<br>(nhà<br>sản<br>xuất nội<br>dung)|
|Quản trị viên<br>(Admin)|Người quản lý hệ<br>thống với quyền<br>cao nhất. Được|Có tài khoản &<br>role Admin|+ Tất cả quyền của Author.<br>+ Quản lý (CRUD) danh<br>mục. + Sửa/xóa bất kỳ|Trung<br>bình (số|


|**Vai trò**|**Mô tả**|**Điều kiện**|**Quyền hạn chính**|**Ưu tiên**<br>**phục**<br>**vụ**|
|---|---|---|---|---|
||gán thủ công qua<br>database seeding.||recipe của bất kỳ Author. +<br>Truy cập Hangfire<br>Dashboard. + Xem<br>structured logs.|lượng<br>ít)|



**Ghi chú về phân quyền:** MVP triển khai 2 tầng phân quyền. (1) Role-Based Authorization: phân biệt quyền dựa trên role (Guest/Author/Admin). (2) Resource-Based Authorization: Author chỉ sửa/xóa được recipe của chính mình (AuthorId == currentUserId). Admin có quyền bypass resource ownership check. Policy xác nhận email là phần mở rộng sau MVP.

#### **2.4. Môi trường Vận hành** 

##### **2.4.1. Môi trường Server (Production)** 

|**Thành phần**|**Yêu cầu tối thiểu**|**Khuyến nghị**|**Ghi chú**|
|---|---|---|---|
|Hệ điều hành|Linux Ubuntu<br>22.04 LTS|Ubuntu 22.04<br>LTS / Debian 12|Docker phải được cài đặt|
|.NET Runtime|.NET 10.0 Runtime<br>(aspnet)|.NET 10.0.x<br>latest patch|Cung cấp qua Docker image<br>mcr.microsoft.com/dotnet/aspnet:10.0|
|Node.js|Node.js 20 LTS<br>(build only)|Node.js 22 LTS|Chỉ cần lúc build Next.js; production<br>dùng standalone output|
|PostgreSQL|PostgreSQL 16.x|PostgreSQL<br>16.x|Extensions: unaccent, pg_trgm bắt<br>buộc|
|Redis|Redis 7.x|Redis 7.2.x|Persistent mode với AOF|
|MinIO|MinIO<br>RELEASE.2024+|MinIO latest<br>stable|Bucket policy: public-read cho recipe<br>images|
|Docker|Docker Engine<br>24.x|Docker Engine<br>27.x + Compose<br>v2|Docker Compose cho local dev và<br>staging|
|Nginx|Nginx 1.24+|Nginx 1.26+<br>(stable)|Reverse proxy, SSL termination|
|RAM|4 GB minimum|8 GB+|RAM cần tăng nếu Redis cache lớn|
|CPU|2 vCPU minimum|4 vCPU+|CPU-intensive: FTS indexing, image<br>processing|
|Disk|20 GB SSD<br>minimum|50 GB+ SSD|MinIO object storage tốn nhiều disk|



##### **2.4.2. Môi trường Phát triển (Development)** 

|**Thành phần**|**Yêu cầu**|
|---|---|
|.NET 10 SDK|dotnet SDK 10.0.x (bao gồm CLI và runtime)|


|**Thành phần**|**Yêu cầu**|
|---|---|
|Node.js|Node.js 20+ LTS với npm 10+|
|Docker Desktop|Docker Desktop 4.x+ (Windows/macOS) hoặc Docker Engine (Linux) –<br>để chạy PostgreSQL, Redis, MinIO local|
|IDE / Editor|Visual Studio 2022 v17.12+ / Rider 2024+ / VS Code với C# Dev Kit<br>extension|
|Git|Git 2.40+ với Git LFS (nếu lưu asset lớn)|
|Postman / Scalar|Postman hoặc Scalar UI (tích hợp sẵn, chạy tại /scalar) để test API|



##### **2.4.3. Yêu cầu Trình duyệt Client** 

|**Trình duyệt**|**Phiên bản tối thiểu**|**Ghi chú**|
|---|---|---|
|Google Chrome|90+|Khuyến nghị chính – tốt nhất cho<br>Developer Tools|
|Mozilla Firefox|88+|Hỗ trợ đầy đủ|
|Microsoft Edge|90+ (Chromium)|Hỗ trợ đầy đủ (Chromium-based)|
|Safari|14+ (macOS 11+)|Hỗ trợ đầy đủ; Safari 13 trở xuống<br>KHÔNG đảm bảo|
|Mobile Chrome (Android)|90+|Responsive design, touch-friendly|
|Mobile Safari (iOS)|iOS 14+|Hỗ trợ đầy đủ|
|Internet Explorer|Mọi phiên bản|KHÔNG hỗ trợ (EOL)|



#### **2.5. Ràng buộc Thiết kế và Hiện thực** 

Các ràng buộc sau đây là bắt buộc và không thể thương lượng trong suốt quá trình phát triển: 

|**Mã ràng**<br>**buộc**|**Loại**|**Mô tả ràng buộc**|
|---|---|---|
|CONS-001|Kiến trúc|Backend PHẢI tuân thủ Clean Architecture với 4 tầng riêng biệt:<br>Domain, Application, Infrastructure, Presentation. Tầng Domain<br>không được phụ thuộc bất kỳ thư viện ngoài nào.|
|CONS-002|Pattern|CQRS với MediatR là pattern bắt buộc cho tầng Application. Mỗi<br>use case được hiện thực dưới dạng Command hoặc Query<br>Handler riêng biệt.|
|CONS-003|Ngôn ngữ /<br>Framework|Backend: .NET 10 Minimal APIs (không dùng MVC Controllers).<br>Frontend: Next.js App Router (không dùng Pages Router).|
|CONS-004|Bảo mật|Xác thực PHẢI sử dụng JWT stateless (access token 15 phút,<br>refresh token 7 ngày). Mật khẩu PHẢI được hash với PBKDF2 qua<br>ASP.NET Core Identity.|
|CONS-005|API Design|API PHẢI tuân thủ RESTful design. Phản hồi lỗi PHẢI theo RFC<br>7807 (application/problem+json). API versioning qua URL path<br>(/api/v1/).|


|**Mã ràng**<br>**buộc**|**Loại**|**Mô tả ràng buộc**|
|---|---|---|
|CONS-006|Database|PostgreSQL là DBMS duy nhất. Migrations qua EF Core Code-<br>First. Không viết raw SQL trực tiếp (dùng LINQ hoặc Raw SQL có<br>parameterization qua EF Core).|
|CONS-007|File Upload|Kích thước tệp tải lên tối đa 5 MB. Định dạng chỉ chấp nhận:<br>image/jpeg, image/png, image/webp, image/avif. Kiểm tra MIME<br>type (không chỉ extension).|
|CONS-008|Validation|Input validation PHẢI qua FluentValidation kết hợp MediatR<br>Pipeline Behavior. Không validation trong Endpoint handler.|
|CONS-009|Container|Ứng dụng PHẢI được đóng gói Docker. Dockerfile multi-stage<br>build (SDK → aspnet runtime). Docker Compose cho local<br>development.|
|CONS-010|Logging|Structured logging với Serilog là bắt buộc. Mọi log entry PHẢI có<br>CorrelationId, RequestPath, UserId (khi đã xác thực).|



#### **2.6. Giả định và Phụ thuộc** 

##### **2.6.1. Giả định** 

- Môi trường development có kết nối Internet để pull Docker images và package NuGet/npm. 

- PostgreSQL, Redis và MinIO được cung cấp qua Docker Compose trong development và dưới dạng managed service (hoặc VPS) trong production. 

- Người dùng cuối có trình duyệt hiện đại và kết nối Internet đủ ổn định để load ảnh từ MinIO. 

- Dữ liệu test (seed) được tạo bằng thư viện Bogus với 50 recipe mẫu và 5 tác giả mẫu. 

- Email service (SendGrid hoặc SMTP) được cấu hình sẵn khi triển khai production để gửi email chào mừng. 

- **Giới hạn dữ liệu kỳ vọng (initial scale):** ≤ 10,000 công thức, ≤ 5,000 người dùng, ≤ 50 danh mục – phù hợp với single-server deployment. 

##### **2.6.2. Phụ thuộc Bên ngoài** 

|**Phụ thuộc**|**Phiên bản**|**Mức độ ảnh hưởng**<br>**nếu không khả dụng**|**Kế hoạch dự phòng**|
|---|---|---|---|
|Google OAuth<br>2.0 API|v2 (OpenID<br>Connect)|Cao – Mất chức năng<br>đăng nhập Google|Vẫn có đăng nhập<br>email/password. Hiển thị thông<br>báo "Google login tạm thời<br>không khả dụng".|
|MinIO / S3|MinIO<br>RELEASE.2024+|Cao – Không<br>upload/xem được ảnh|Fallback về local FileSystem<br>storage (development only).<br>Production cần MinIO.|
|Redis|7.x|Trung bình – Mất<br>cache, hiệu năng giảm|Hệ thống tiếp tục hoạt động<br>nhưng mọi request đều query<br>database. Cache miss graceful<br>degradation.|


|**Phụ thuộc**|**Phiên bản**|**Mức độ ảnh hưởng**<br>**nếu không khả dụng**|**Kế hoạch dự phòng**|
|---|---|---|---|
|PostgreSQL|16.x|Rất cao – Toàn bộ hệ<br>thống ngừng|Backup định kỳ (pg_dump).<br>Readiness probe sẽ fail, Nginx<br>trả 503.|
|Hangfire (in-<br>process)|v1.8+|Thấp – Background<br>jobs không chạy|Fire-and-forget jobs sẽ bị mất;<br>Recurring jobs bỏ qua chu kỳ.<br>Không ảnh hưởng core<br>functionality.|


## **CHƯƠNG 3. YÊU CẦU CHỨC NĂNG CHI TIẾT** 

Chương này đặc tả chi tiết 27 Functional Requirements (FR) được nhóm thành 7 module chức năng. Mỗi FR được mô tả theo template chuẩn bao gồm: Mã yêu cầu, Tên, Nhóm chức năng, Tác nhân, Mức ưu tiên (MoSCoW), Mô tả, Điều kiện tiên quyết, Luồng chính, Luồng thay thế/Ngoại lệ, HTTP Endpoint, Kết quả mong đợi và HTTP Status Code. 

**Quy ước mức ưu tiên MoSCoW:** M (Must Have – Bắt buộc), S (Should Have – Nên có), C (Could Have – Có thể có), W (Won't Have – Không trong scope hiện tại). 

#### **3.1. Module Xác thực và Quản lý Người dùng (FR-AUTH)** 

Module này quản lý toàn bộ vòng đời xác thực người dùng: từ đăng ký, đăng nhập đa phương thức, duy trì phiên làm việc với cơ chế token rotation, đến quản lý hồ sơ cá nhân. Backend sử dụng ASP.NET Core Identity kết hợp JWT và OAuth 2.0. 

##### **FR-AUTH-001: Đăng ký Tài khoản (User Registration)** 

|**Mã yêu cầu**|FR-AUTH-001|
|---|---|
|**Tên yêu cầu**|Đăng ký Tài khoản Mới|
|**Nhóm chức năng**|Module Xác thực và Quản lý Người dùng (FR-AUTH)|
|**Tác nhân**|Khách (Guest / Anonymous User)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have (Bắt buộc)|
|**Mô tả**|Hệ thống cho phép người dùng chưa có tài khoản tạo một tài khoản<br>mới bằng cách cung cấp thông tin cơ bản. Sau khi đăng ký thành công,<br>người dùng tự động được gán role "Author" và nhận bộ token để truy<br>cập ngay lập tức (auto-login sau đăng ký). Hệ thống kích hoạt job gửi<br>email chào mừng bất đồng bộ qua Hangfire.|
|**Điều kiện tiên quyết**|1. Người dùng chưa đăng nhập vào hệ thống. 2. Endpoint POST<br>/api/v1/auth/register đang hoạt động. 3. PostgreSQL database đang kết<br>nối thành công.|
|**Luồng chính (Happy**<br>**Path)**|1. Client gửi POST /api/v1/auth/register với body: { "displayName": "...", "email": "...", "password": "..." }.<br>2. RegisterCommand được dispatch qua MediatR.<br>3. ValidationBehavior kiểm tra displayName 2–100 ký tự, email đúng format và password đủ mạnh.<br>4. Handler kiểm tra email chưa tồn tại, sinh UserName nội bộ duy nhất từ email và tạo ApplicationUser.<br>5. UserManager.CreateAsync hash password bằng PBKDF2 và gán role "Author".<br>6. Tạo access token 15 phút và refresh token 7 ngày; chỉ lưu SHA-256 hash của refresh token.<br>7. Enqueue WelcomeEmailJob.<br>8. Trả HTTP 201 với access token, thời hạn và user; raw refresh token được đặt trong cookie bảo mật theo ADR-0003.|


|---|---|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Email đã tồn tại: Tại bước 4, nếu email đã được đăng ký → Throw<br>ConflictException → GlobalExceptionMiddleware trả về HTTP 409<br>Conflict với RFC 7807 body.<br>A2 – Password không đủ mạnh: Tại bước 3 hoặc 6,<br>UserManager.CreateAsync trả về IdentityError → Throw<br>ValidationException → HTTP 422 Unprocessable Entity với danh sách<br>lỗi chi tiết.<br>A3 – Dữ liệu đầu vào không hợp lệ: Tại bước 3, FluentValidation fail →<br>HTTP 422 với từng field lỗi (theo RFC 7807 ValidationProblemDetails).<br>A4 – Database không kết nối: EF Core ném DbUpdateException →<br>HTTP 500 Internal Server Error (GlobalExceptionMiddleware log lỗi,<br>không expose stack trace).|
|**HTTP Method &**<br>**Endpoint**|POST  /api/v1/auth/register|
|**Kết quả mong đợi**|Tài khoản mới được tạo trong database, role "Author" được gán,<br>refresh token được persist, email chào mừng được đẩy vào Hangfire<br>queue. Client nhận được access token và refresh token.|
|**HTTP Status Code trả**<br>**về**|201 Created – Đăng ký thành công. 409 Conflict – Email đã tồn tại. 422<br>Unprocessable Entity – Dữ liệu không hợp lệ. 500 Internal Server Error<br>– Lỗi hệ thống.|



##### **FR-AUTH-002: Đăng nhập bằng Email/Mật khẩu (Local Login)** 

|**Mã yêu cầu**|FR-AUTH-002|
|---|---|
|**Tên yêu cầu**|Đăng nhập bằng Email và Mật khẩu|
|**Nhóm chức năng**|Module Xác thực và Quản lý Người dùng (FR-AUTH)|
|**Tác nhân**|Tác giả đã đăng ký (Author) hoặc Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have (Bắt buộc)|
|**Mô tả**|Hệ thống cho phép người dùng đã có tài khoản đăng nhập bằng email<br>và mật khẩu. Mỗi lần đăng nhập thành công tạo ra một cặp access<br>token mới (JWT, 15 phút) và refresh token mới (7 ngày). Cơ chế Token<br>Rotation: refresh token cũ KHÔNG bị xóa ngay mà được đánh dấu đã<br>sử dụng bằng cách set RevokedAt = DateTime.UtcNow (để phát hiện token reuse attack).|
|**Điều kiện tiên quyết**|1. Người dùng đã có tài khoản hợp lệ trong hệ thống. 2. Tài khoản<br>chưa bị khóa (LockoutEnabled = false hoặc chưa đến lockout<br>deadline).|


|**Luồng chính (Happy**<br>**Path)**|1. Client gửi POST /api/v1/auth/login với body: { "email": "...",<br>"password": "..." }.<br>2. LoginCommand được dispatch qua MediatR.<br>3. ValidationBehavior kiểm tra email format và password không rỗng.<br>4. LoginCommandHandler tìm user:<br>UserManager.FindByEmailAsync(email).<br>5. Xác minh mật khẩu: UserManager.CheckPasswordAsync(user,<br>password) – so sánh với PBKDF2 hash.<br>6. Kiểm tra tài khoản không bị lockout:<br>UserManager.IsLockedOutAsync(user).<br>7. Tạo access token mới: JwtService.GenerateAccessToken(user,<br>roles).<br>8. Tạo refresh token mới: JwtService.GenerateRefreshToken(userId).<br>9. Lưu refresh token mới vào database.<br>10. Ghi nhận đăng nhập thành công:<br>UserManager.ResetAccessFailedCountAsync(user).<br>11. Trả về HTTP 200 OK với AuthResponseDto.|
|---|---|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Tài khoản không tồn tại hoặc mật khẩu sai: HTTP 401<br>Unauthorized với message generic "Email hoặc mật khẩu không đúng"<br>(KHÔNG tiết lộ tài khoản có tồn tại hay không – tránh User<br>Enumeration Attack).<br>A2 – Tài khoản bị lockout: HTTP 423 Locked với thông báo thời gian<br>unlock còn lại.<br>A3 – Vượt quá số lần thử sai (5 lần): AccessFailedCount tăng lên, sau<br>5 lần → tài khoản bị lockout 15 phút (cấu hình qua LockoutOptions).|
|**HTTP Method &**<br>**Endpoint**|POST  /api/v1/auth/login|
|**Kết quả mong đợi**|Access token và refresh token mới được tạo và trả về. Refresh token<br>được lưu vào database.|
|**HTTP Status Code trả**<br>**về**|200 OK – Đăng nhập thành công. 401 Unauthorized – Sai email/mật<br>khẩu. 422 Unprocessable Entity – Dữ liệu không hợp lệ. 423 Locked –<br>Tài khoản bị khóa.|



##### **FR-AUTH-003: Đăng nhập bằng Google OAuth 2.0** 

|**Mã yêu cầu**|FR-AUTH-003|
|---|---|
|**Tên yêu cầu**|Đăng nhập / Đăng ký bằng Google OAuth 2.0|
|**Nhóm chức năng**|Module Xác thực và Quản lý Người dùng (FR-AUTH)|
|**Tác nhân**|Khách (Guest) – lần đầu / Người dùng đã đăng ký trước đó qua Google|
|**Mức ưu tiên**<br>**(MoSCoW)**|S – Should Have|
|**Mô tả**|Hệ thống hỗ trợ đăng nhập qua tài khoản Google sử dụng OAuth 2.0<br>Authorization Code Flow với PKCE. Nếu đây là lần đăng nhập Google<br>đầu tiên, hệ thống tự động tạo tài khoản mới từ thông tin Google profile<br>(email, display name, avatar URL) và gán role "Author". Nếu email đã<br>tồn tại từ đăng ký thủ công trước đó, hệ thống liên kết Google login với<br>tài khoản hiện có.|


|**Điều kiện tiên quyết**|1. Google OAuth 2.0 Credentials (ClientId, ClientSecret) đã được cấu<br>hình trong appsettings. 2. Redirect URI đã được đăng ký trong Google<br>Cloud Console. 3. Người dùng có tài khoản Google hợp lệ.|
|---|---|
|**Luồng chính (Happy**<br>**Path)**|1. Frontend dùng Google Identity Services để lấy ID token với scopes openid, email, profile.<br>2. Frontend gửi POST /api/v1/auth/google với body `{ "idToken": "..." }`.<br>3. Backend xác minh chữ ký, issuer, audience và expiry của ID token với Google.<br>4. Handler tìm external login theo Google subject (`sub`).<br>5. Nếu chưa có tài khoản, tạo ApplicationUser từ profile và gán role "Author"; nếu email đã tồn tại, liên kết login với tài khoản hiện có theo chính sách an toàn.<br>6. Tạo access token và refresh token nội bộ.<br>7. Trả HTTP 200 với AuthResponseDto; raw refresh token được đặt trong cookie bảo mật theo ADR-0003.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Google token không hợp lệ hoặc hết hạn: HTTP 401<br>Unauthorized.<br>A2 – Email Google bị revoke quyền: HTTP 400 Bad Request.<br>A3 – Google API không khả dụng: HTTP 502 Bad Gateway với<br>message thích hợp.|
|**HTTP Method &**<br>**Endpoint**|POST  /api/v1/auth/google|
|**Kết quả mong đợi**|Người dùng được đăng nhập (hoặc tự động đăng ký), nhận<br>AuthResponseDto. Tài khoản mới (nếu có) được tạo với role "Author".|
|**HTTP Status Code trả**<br>**về**|200 OK – Đăng nhập/đăng ký thành công. 401 Unauthorized – Token<br>Google không hợp lệ. 400 Bad Request – Thiếu thông tin Google<br>profile.|



##### **FR-AUTH-004: Làm mới Access Token (Token Refresh)** 

|**Mã yêu cầu**|FR-AUTH-004|
|---|---|
|**Tên yêu cầu**|Làm mới Access Token bằng Refresh Token|
|**Nhóm chức năng**|Module Xác thực và Quản lý Người dùng (FR-AUTH)|
|**Tác nhân**|Tác giả (Author) / Quản trị viên (Admin) – có refresh token hợp lệ|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have (Bắt buộc)|
|**Mô tả**|Khi access token hết hạn (sau 15 phút), client sử dụng refresh token<br>còn hiệu lực để lấy cặp token mới mà không cần người dùng đăng<br>nhập lại. Cơ chế Token Rotation bắt buộc: mỗi lần refresh, refresh<br>token cũ bị vô hiệu hóa (RevokedAt = DateTime.UtcNow — RevokedAt IS NOT NULL đồng nghĩa đã bị revoke, theo schema bảng RefreshToken) và một refresh token MỚI được tạo ra. Đây là biện<br>pháp chống Refresh Token Reuse Attack.|


|**Điều kiện tiên quyết**|1. Client có refresh token hợp lệ (chưa hết hạn, chưa bị revoke, chưa<br>bị thay thế). 2. Người dùng tương ứng vẫn còn tồn tại trong database<br>và chưa bị khóa.|
|---|---|
||1. Client gửi POST /api/v1/auth/refresh với body: { "refreshToken": "..."<br>}.<br>2. RefreshTokenCommand dispatch qua MediatR.<br>3. Handler tìm refresh token trong database: bao gồm User navigation<br>property.|
|**Luồng chính (Happy**<br>**Path)**|4. Kiểm tra: token tồn tại, RevokedAt IS NULL (chưa bị revoke), ExpiresAt ><br>DateTime.UtcNow, user vẫn active.<br>5. Đánh dấu token cũ: RevokedAt = DateTime.UtcNow, ReplacedByTokenHash =<br>SHA256(newToken).<br>6. Tạo access token mới cho user.<br>7. Tạo refresh token mới, lưu vào database.<br>8. Trả về HTTP 200 OK với AuthResponseDto chứa cặp token mới.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Refresh token không tìm thấy trong database: HTTP 401<br>Unauthorized.<br>A2 – Refresh token đã hết hạn: HTTP 401 Unauthorized, client phải<br>đăng nhập lại.<br>A3 – Refresh token đã bị revoke (Reuse Attack detected): HTTP 401<br>Unauthorized. LOG SECURITY ALERT với mức WARNING. Có thể<br>kích hoạt revoke toàn bộ refresh tokens của user đó (paranoid mode).<br>A4 – User bị xóa hoặc bị khóa sau khi token được cấp: HTTP 401<br>Unauthorized.|
|**HTTP Method &**<br>**Endpoint**|POST  /api/v1/auth/refresh|
|**Kết quả mong đợi**|Refresh token cũ bị invalidate. Access token mới (15 phút) và refresh<br>token mới (7 ngày) được tạo và trả về.|
|**HTTP Status Code trả**<br>**về**|200 OK – Refresh thành công. 401 Unauthorized – Token không hợp<br>lệ, hết hạn hoặc đã bị revoke.|



##### **FR-AUTH-005: Đăng xuất (Logout / Token Revocation)** 

|**Mã yêu cầu**|FR-AUTH-005|
|---|---|
|**Tên yêu cầu**|Đăng xuất và Thu hồi Refresh Token|
|**Nhóm chức năng**|Module Xác thực và Quản lý Người dùng (FR-AUTH)|
|**Tác nhân**|Tác giả (Author) / Quản trị viên (Admin) đang đăng nhập|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Người dùng đăng xuất khỏi hệ thống. Vì JWT access token là stateless và không thể revoke trực tiếp trước khi hết hạn, logout revoke refresh token trong database, xóa refresh-token cookie và frontend xóa access token khỏi bộ nhớ.|
|**Điều kiện tiên quyết**|1. Người dùng đang đăng nhập với access token hợp lệ trong<br>Authorization header. 2. Client gửi refresh token muốn revoke.|
|**Luồng chính (Happy**<br>**Path)**|1. Client gửi POST /api/v1/auth/logout với Authorization: Bearer<br>{accessToken} header và body: { "refreshToken": "..." }.<br>2. Middleware xác thực JWT (UseAuthentication) xác minh access<br>token.<br>3. LogoutCommandHandler tìm refresh token trong database.<br>4. Nếu tìm thấy và thuộc về user hiện tại: đánh dấu RevokedAt = DateTime.UtcNow<br>(RevokedAt IS NOT NULL = đã bị revoke).<br>5. Lưu thay đổi vào database.<br>6. Trả về HTTP 204 No Content.|


|---|---|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Refresh token không tìm thấy: Vẫn trả về HTTP 204 (idempotent<br>– không tiết lộ trạng thái).<br>A2 – Access token đã hết hạn: Vẫn cho phép logout nếu refresh token<br>hợp lệ; hoặc HTTP 401 nếu không cung cấp refresh token.|
|**HTTP Method &**<br>**Endpoint**|POST  /api/v1/auth/logout|
|**Kết quả mong đợi**|Refresh token bị đánh dấu IsRevoked = true trong database. Các lần<br>refresh tiếp theo với token này sẽ thất bại.|
|**HTTP Status Code trả**<br>**về**|204 No Content – Đăng xuất thành công (hoặc token không tồn tại –<br>idempotent). 401 Unauthorized – Access token không hợp lệ.|



##### **FR-AUTH-006: Xem Hồ sơ Cá nhân (View Profile)** 

|**Mã yêu cầu**|FR-AUTH-006|
|---|---|
|**Tên yêu cầu**|Xem Hồ sơ Cá nhân|
|**Nhóm chức năng**|Module Xác thực và Quản lý Người dùng (FR-AUTH)|
|**Tác nhân**|Tác giả (Author) / Quản trị viên (Admin) đang đăng nhập|
|**Mức ưu tiên**<br>**(MoSCoW)**|S – Should Have|
|**Mô tả**|Trả về thông tin hồ sơ của người dùng hiện đang đăng nhập, dựa trên<br>UserId được trích xuất từ JWT claims. Không bao giờ trả về<br>PasswordHash hoặc SecurityStamp.|
|**Điều kiện tiên quyết**|1. Người dùng đang đăng nhập với access token hợp lệ.|
|**Luồng chính (Happy**<br>**Path)**|1. Client gửi GET /api/v1/auth/me với Authorization: Bearer<br>{accessToken}.<br>2. Middleware xác thực JWT, trích xuất UserId từ claim NameIdentifier.<br>3. GetCurrentUserQuery dispatch qua MediatR.<br>4. Handler tìm user: UserManager.FindByIdAsync(userId).<br>5. Map sang UserProfileDto: { id, fullName, email, userName,<br>avatarUrl, roles, emailConfirmed, createdAt }.<br>6. Trả về HTTP 200 OK với UserProfileDto.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – User đã bị xóa khỏi database sau khi token được cấp: HTTP 404<br>Not Found.|
|**HTTP Method &**<br>**Endpoint**|GET  /api/v1/auth/me|
|**Kết quả mong đợi**|Trả về thông tin hồ sơ đầy đủ của người dùng (không có thông tin nhạy<br>cảm như password hash).|


**HTTP Status Code trả** 200 OK – Thành công. 401 Unauthorized – Chưa đăng nhập. 404 Not **về** Found – User không tồn tại. 

**FR-AUTH-007: Cập nhật Hồ sơ Cá nhân (Update Profile)** 

|**Mã yêu cầu**|FR-AUTH-007|
|---|---|
|**Tên yêu cầu**|Cập nhật Hồ sơ Cá nhân|
|**Nhóm chức năng**|Module Xác thực và Quản lý Người dùng (FR-AUTH)|
|**Tác nhân**|Tác giả (Author) / Quản trị viên (Admin) đang đăng nhập|
|**Mức ưu tiên**<br>**(MoSCoW)**|S – Should Have|
|**Mô tả**|Người dùng có thể cập nhật FullName và AvatarUrl của mình. Email và<br>UserName không thể thay đổi qua endpoint này (đây là quy trình riêng<br>có xác nhận OTP). Sử dụng PATCH (partial update) để chỉ cập nhật<br>các field được cung cấp.|
|**Điều kiện tiên quyết**|1. Người dùng đang đăng nhập. 2. Dữ liệu mới phải hợp lệ (FullName<br>không rỗng, AvatarUrl là URL hợp lệ nếu cung cấp).|
|**Luồng chính (Happy**<br>**Path)**|1. Client gửi PATCH /api/v1/auth/me với body: { "fullName": "...",<br>"avatarUrl": "..." }.<br>2. UpdateProfileCommand dispatch qua MediatR, UserId lấy từ JWT<br>claims.<br>3. ValidationBehavior kiểm tra: fullName 2–100 ký tự, avatarUrl là URL<br>hợp lệ (nếu cung cấp).<br>4. Handler tìm user, cập nhật FullName và/hoặc AvatarUrl.<br>5. UserManager.UpdateAsync(user).<br>6. Trả về HTTP 200 OK với UserProfileDto đã cập nhật.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Dữ liệu không hợp lệ: HTTP 422 Unprocessable Entity.|
|**HTTP Method &**<br>**Endpoint**|PATCH  /api/v1/auth/me|
|**Kết quả mong đợi**|Hồ sơ người dùng được cập nhật trong database. Trả về hồ sơ mới.|
|**HTTP Status Code trả**<br>**về**|200 OK – Cập nhật thành công. 401 Unauthorized – Chưa đăng nhập.<br>422 Unprocessable Entity – Dữ liệu không hợp lệ.|



#### **3.2. Module Quản lý Danh mục (FR-CAT)** 

Module quản lý danh mục (Category) phân loại công thức nấu ăn. Danh mục được tạo và duy trì bởi Admin; Author và Guest chỉ có quyền đọc. Mỗi danh mục có Slug duy nhất phục vụ URL thân thiện SEO. Danh mục được cache với IMemoryCache (TTL 1 giờ) vì thay đổi ít thường xuyên. 

**FR-CAT-001: Xem Danh sách Danh mục** 

|**Mã yêu cầu**|FR-CAT-001|
|---|---|
|**Tên yêu cầu**|Xem Danh sách Tất cả Danh mục|


|**Nhóm chức năng**|Module Quản lý Danh mục (FR-CAT)|
|---|---|
|**Tác nhân**|Tất cả (Guest / Author / Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Trả về danh sách tất cả danh mục công thức hiện có trong hệ thống,<br>kèm số lượng công thức đã xuất bản (Published) trong mỗi danh mục.<br>Kết quả được cache với IMemoryCache (TTL 5 phút) và sắp xếp theo<br>Name tăng dần. (Ghi chú: TTL cache danh mục thống nhất là 60 phút theo FR-CAT-001.)|
|**Điều kiện tiên quyết**|1. Ít nhất một danh mục tồn tại trong database (hoặc trả về mảng rỗng).<br>2. Không yêu cầu xác thực.|
|**Luồng chính (Happy**<br>**Path)**|1. Client gửi GET /api/v1/categories.<br>2. GetCategoriesQuery dispatch qua MediatR.<br>3. Handler kiểm tra IMemoryCache với key "categories:all".<br>4. Cache hit: trả về dữ liệu từ cache.<br>5. Cache miss: dùng query contract của module để đọc danh mục và số công thức đã xuất bản, sau đó map sang CategoryDto[].<br>6. Lưu vào IMemoryCache với TTL 60 phút (sliding expiration).<br>7. Trả về HTTP 200 OK với CategoryDto[].|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Không có danh mục nào: HTTP 200 OK với mảng rỗng [].|
|**HTTP Method &**<br>**Endpoint**|GET  /api/v1/categories|
|**Kết quả mong đợi**|Mảng CategoryDto[] với các field: { id, name, slug, description,<br>recipeCount }. Kết quả được serve từ cache khi có.|
|**HTTP Status Code trả**<br>**về**|200 OK – Thành công (kể cả khi trống).|



##### **FR-CAT-002: Xem Chi tiết Danh mục và Công thức** 

|**Mã yêu cầu**|FR-CAT-002|
|---|---|
|**Tên yêu cầu**|Xem Chi tiết Danh mục và Danh sách Công thức thuộc Danh mục|
|**Nhóm chức năng**|Module Quản lý Danh mục (FR-CAT)|
|**Tác nhân**|Tất cả (Guest / Author / Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Trả về thông tin chi tiết của một danh mục cụ thể (theo Slug) kèm danh<br>sách phân trang các công thức đã xuất bản (Published) thuộc danh<br>mục đó. Guest chỉ thấy Published recipes; Author thấy thêm Draft<br>recipes của chính mình trong danh mục.|
|**Điều kiện tiên quyết**|1. Danh mục với slug tương ứng phải tồn tại. 2. Không yêu cầu xác<br>thực.|
|**Luồng chính (Happy**<br>**Path)**|1. Client gửi GET /api/v1/categories/{slug}?page=1&pageSize=12.<br>2. GetCategoryBySlugQuery dispatch qua MediatR.<br>3. Handler dùng query contract của module để tìm category theo slug.<br>4. Query recipes thuộc category với Status == Published (+ Draft của<br>currentUser nếu đã đăng nhập).<br>5. Apply pagination (OFFSET-based: SKIP (page-1)*pageSize TAKE<br>pageSize).<br>6. Map sang CategoryDetailDto kèm<br>PagedResult<RecipeSummaryDto>.<br>7. Trả về HTTP 200 OK.|


|---|---|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Slug không tồn tại: HTTP 404 Not Found với RFC 7807 body.|
|**HTTP Method &**<br>**Endpoint**|GET  /api/v1/categories/{slug}?page={n}&pageSize={n}|
|**Kết quả mong đợi**|{ category: CategoryDto, recipes: { items: RecipeSummaryDto[],<br>totalCount, page, pageSize, totalPages } }|
|**HTTP Status Code trả**<br>**về**|200 OK – Thành công. 404 Not Found – Slug không tồn tại.|



##### **FR-CAT-003: Tạo Danh mục Mới [Admin]** 

|**Mã yêu cầu**|FR-CAT-003|
|---|---|
|**Tên yêu cầu**|Tạo Danh mục Công thức Mới|
|**Nhóm chức năng**|Module Quản lý Danh mục (FR-CAT)|
|**Tác nhân**|Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Admin tạo danh mục công thức mới. Slug được tự động sinh từ Name<br>(slugify: chuyển sang chữ thường, bỏ dấu, thay khoảng trắng bằng "-").<br>Nếu Slug đã tồn tại, hệ thống thêm suffix số (e.g., "mon-chinh-2"). Sau<br>khi tạo, cache danh mục (IMemoryCache key "categories:all") bị<br>invalidate.|
|**Điều kiện tiên quyết**|1. Người dùng đang đăng nhập với role Admin. 2. Name chưa tồn tại<br>trong database.|
|**Luồng chính (Happy**<br>**Path)**|1. Admin gửi POST /api/v1/categories với Authorization: Bearer<br>{adminJwt} và body: { "name": "...", "description": "..." }.<br>2. RequireAuthorization("Admin") middleware kiểm tra role.<br>3. CreateCategoryCommand dispatch qua MediatR.<br>4. ValidationBehavior: name 2–50 ký tự, không chứa HTML.<br>5. SlugHelper.Generate(name) tạo slug.<br>6. Kiểm tra slug chưa tồn tại. Nếu trùng, thêm "-2", "-3",... cho đến khi<br>unique.<br>7. Category.Create(name, slug, description) tạo entity.<br>8. `ICategoryRepository.AddAsync(entity)`.<br>9. `IDataSession.SaveChangesAsync()`.<br>10. MemoryCache.Remove("categories:all") – invalidate cache.<br>11. Trả về HTTP 201 Created với CategoryDto và Location header.|


|**Luồng thay thế /**|A1 – Thiếu role Admin: HTTP 403 Forbidden.|
|---|---|
|**Ngoại lệ**|A2 – Dữ liệu không hợp lệ: HTTP 422.|
|**HTTP Method &**<br>**Endpoint**|POST  /api/v1/categories|
|**Kết quả mong đợi**|Danh mục mới được tạo trong database. Cache danh mục bị xóa.<br>Location header trỏ đến /api/v1/categories/{newSlug}.|
|**HTTP Status Code trả**<br>**về**|201 Created – Tạo thành công. 403 Forbidden – Không có quyền<br>Admin. 409 Conflict – Name đã tồn tại. 422 Unprocessable Entity – Dữ<br>liệu không hợp lệ.|



##### **FR-CAT-004: Cập nhật Danh mục [Admin]** 

|**Mã yêu cầu**|FR-CAT-004|
|---|---|
|**Tên yêu cầu**|Cập nhật Thông tin Danh mục|
|**Nhóm chức năng**|Module Quản lý Danh mục (FR-CAT)|
|**Tác nhân**|Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Admin cập nhật Name và/hoặc Description của danh mục. Slug<br>KHÔNG thay đổi khi đổi tên (để tránh broken links). Sau khi cập nhật,<br>cache bị invalidate.|
|**Điều kiện tiên quyết**|1. Admin đang đăng nhập. 2. Danh mục với ID tương ứng tồn tại.|
|**Luồng chính (Happy**<br>**Path)**|1. Admin gửi PUT /api/v1/categories/{id} với body: { "name": "...",<br>"description": "..." }.<br>2. Kiểm tra role Admin.<br>3. UpdateCategoryCommand dispatch qua MediatR.<br>4. Tìm category theo ID, cập nhật Name và Description.<br>5. Lưu thay đổi, invalidate cache.<br>6. Trả về HTTP 200 OK với CategoryDto đã cập nhật.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – ID không tồn tại: HTTP 404.<br>A2 – Thiếu role Admin: HTTP 403.|
|**HTTP Method &**<br>**Endpoint**|PUT  /api/v1/categories/{id:guid}|
|**Kết quả mong đợi**|Thông tin danh mục được cập nhật. Cache invalidated.|
|**HTTP Status Code trả**<br>**về**|200 OK – Cập nhật thành công. 403 Forbidden. 404 Not Found. 422<br>Unprocessable Entity.|



##### **FR-CAT-005: Xóa Danh mục [Admin]** 

|**Mã yêu cầu**|FR-CAT-005|
|---|---|
|**Tên yêu cầu**|Xóa Danh mục|
|**Nhóm chức năng**|Module Quản lý Danh mục (FR-CAT)|
|**Tác nhân**|Quản trị viên (Admin)|


|**Mức ưu tiên**<br>**(MoSCoW)**|S – Should Have|
|---|---|
|**Mô tả**|Admin xóa một danh mục. Quy tắc nghiệp vụ: KHÔNG được xóa danh<br>mục còn chứa công thức (dù là Published hay Draft). Admin phải<br>chuyển tất cả công thức sang danh mục khác trước khi xóa. Đây là soft<br>constraint để bảo vệ toàn vẹn dữ liệu.|
|**Điều kiện tiên quyết**|1. Admin đang đăng nhập. 2. Danh mục tồn tại và không còn công thức<br>nào.|
|**Luồng chính (Happy**<br>**Path)**|1. Admin gửi DELETE /api/v1/categories/{id}.<br>2. Kiểm tra role Admin.<br>3. DeleteCategoryCommand dispatch.<br>4. Đếm số recipe trong category: nếu > 0 → Throw<br>ConflictException("Danh mục còn chứa {count} công thức.").<br>5. Xóa entity, lưu thay đổi, invalidate cache.<br>6. Trả về HTTP 204 No Content.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Danh mục có recipe: HTTP 409 Conflict với thông báo số lượng<br>recipe.<br>A2 – ID không tồn tại: HTTP 404.|
|**HTTP Method &**<br>**Endpoint**|DELETE  /api/v1/categories/{id:guid}|
|**Kết quả mong đợi**|Danh mục bị xóa khỏi database. HTTP 204 được trả về.|
|**HTTP Status Code trả**<br>**về**|204 No Content – Xóa thành công. 403 Forbidden. 404 Not Found. 409<br>Conflict – Danh mục còn recipe.|



#### **3.3. Module Quản lý Công thức Nấu ăn (FR-RCP)** 

Module cốt lõi của hệ thống. Recipe là aggregate root chứa các child entity: RecipeStep, RecipeIngredient, RecipeImage và Owned Entity RecipeNutrition. Tất cả mutation (Create/Update/Delete) commit transaction qua `IDataSession.SaveChangesAsync`. Concurrency được xử lý bằng thuộc tính `Version` ánh xạ tới PostgreSQL system column `xmin` để phát hiện lost update khi hai Author cùng sửa một recipe.

**FR-RCP-001: Xem Danh sách Công thức (Paginated + Filtered + Sorted)** 

|**Mã yêu**<br>**cầu**|FR-RCP-001|
|---|---|
|**Tên yêu**<br>**cầu**|Xem Danh sách Công thức Nấu ăn với Phân trang, Lọc và Sắp xếp|
|**Nhóm**<br>**chức**<br>**năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tất cả (Guest / Author / Admin)|
|**Mức ưu**<br>**tiên**|M – Must Have|
|**(MoSCoW)**||


|**Mô tả**|Trả về danh sách phân trang các công thức. Guest và Author khác chỉ thấy Status == Published. Auth<br>thêm Draft/Archived của chính mình. Admin thấy tất cả trạng thái. Hỗ trợ lọc theo CategoryId, Difficult<br>thời gian nấu; sắp xếp theo createdAt, title, cookTime. Kết quả được cache với Output Cache (.NET 1<br>policy "RecipeList" (TTL 15 phút, vary by query string).|
|---|---|
|**Điều kiện**<br>**tiên quyết**|1. Không yêu cầu xác thực (endpoint public cho Published recipes). 2. Tham số page >= 1, pageSize<br>50].|
|**Luồng**<br>**chính**<br>**(Happy**<br>**Path)**|1. Client gửi GET<br>/api/v1/recipes?page=1&pageSize=12&categoryId={guid}&difficulty=Easy&maxCookTime=30&sort= -c<br>2. GetRecipesQuery dispatch qua MediatR.<br>3. Handler xây dựng IQueryable với filters từ query params.<br>4. Áp dụng Authorization filter: nếu Guest → chỉ Published; nếu Author → Published OR (Draft AND A<br>== userId); nếu Admin → tất cả.<br>5. Apply sorting: sort="-createdAt" → ORDER BY CreatedAt DESC; sort="title" → ORDER BY Title AS<br>6. COUNT total trước khi pagination.<br>7. Apply OFFSET-LIMIT pagination.<br>8. Map sang PagedResult<RecipeSummaryDto>.<br>9. Trả về HTTP 200 OK. Output Cache lưu response theo key = {path}?{queryString}.|
|**Luồng**<br>**thay thế /**<br>**Ngoại lệ**|A1 – page hoặc pageSize không hợp lệ: HTTP 422. A2 – categoryId không tồn tại: HTTP 200 với item<br>(không throw 404).|
|**HTTP**<br>**Method &**<br>**Endpoint**|GET<br>/api/v1/recipes?page={n}&pageSize={n}&categoryId={guid}&difficulty={level}&maxCookTime={min}&so|
|**Kết quả**<br>**mong đợi**|PagedResult<RecipeSummaryDto>: { items[], totalCount, page, pageSize, totalPages, hasNextPage,<br>hasPreviousPage }.|
|**HTTP**<br>**Status**<br>**Code trả**<br>**về**|200 OK – Thành công (kể cả items rỗng). 422 Unprocessable Entity – Tham số không hợp lệ.|



##### **FR-RCP-002: Xem Chi tiết Công thức** 

|**Mã yêu**<br>**cầu**|FR-RCP-002|
|---|---|
|**Tên yêu**<br>**cầu**|Xem Chi tiết Công thức Nấu ăn|
|**Nhóm**<br>**chức**<br>**năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tất cả (Guest / Author / Admin)|
|**Mức ưu**<br>**tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Trả về toàn bộ thông tin chi tiết của một công thức cụ thể, bao gồm: thông tin cơ bản, danh sách ngu<br>(RecipeIngredient[]) sắp xếp theo SortOrder, các bước thực hiện (RecipeStep[]) sắp xếp theo StepNu<br>minh họa (RecipeImage[]), thông tin dinh dưỡng (RecipeNutrition), thông tin danh mục và tác giả. Rec<br>chỉ được xem bởi tác giả sở hữu hoặc Admin. Endpoint được cache với Output Cache policy "Recipe<br>(TTL 5 phút) và tagged với "recipes" để hỗ trợ tag-based invalidation.|


|**Điều kiện**<br>**tiên quyết**|1. Recipe với slug tương ứng tồn tại. 2. Nếu Recipe ở trạng thái Draft/Archived: người yêu cầu phải là<br>hoặc Admin.|
|---|---|
|**Luồng**<br>**chính**<br>**(Happy**<br>**Path)**|1. Client gửi GET /api/v1/recipes/{slug}.<br>2. GetRecipeBySlugQuery dispatch qua MediatR.<br>3. Handler query Recipe với Eager Loading:<br>Include(Steps).Include(Ingredients).Include(Images).Include(Category).Include(Author).IncludeOwned(N<br>4. Kiểm tra null → NotFoundException nếu không tìm thấy.<br>5. Kiểm tra Status: nếu Draft/Archived → chỉ tác giả hoặc Admin mới được xem (Authorization check)<br>6. Map sang RecipeDetailDto (bao gồm tất cả nested collections).<br>7. Trả về HTTP 200 OK. Tag output cache entry với ["recipes", $"recipe:{slug}"].|
|**Luồng**<br>**thay thế /**<br>**Ngoại lệ**|A1 – Slug không tồn tại: HTTP 404 Not Found.<br>A2 – Recipe Draft/Archived, người dùng không có quyền: HTTP 403 Forbidden.|
|**HTTP**<br>**Method &**<br>**Endpoint**|GET  /api/v1/recipes/{slug}|
|**Kết quả**<br>**mong đợi**|RecipeDetailDto đầy đủ gồm tất cả nested data (steps, ingredients, images, nutrition, category, autho|
|**HTTP**<br>**Status**<br>**Code trả**<br>**về**|200 OK – Thành công. 403 Forbidden – Không có quyền xem Draft. 404 Not Found – Slug không tồn|



**FR-RCP-003: Tạo Công thức Nấu ăn Mới [Author/Admin]** 

|**Mã yêu cầu**|FR-RCP-003|
|---|---|
|**Tên yêu cầu**|Tạo Công thức Nấu ăn Mới|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả (Author) / Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Author hoặc Admin tạo mới một công thức nấu ăn. Trạng thái ban đầu<br>luôn là Draft (chưa công khai). Slug được tự động sinh từ Title. Steps<br>và Ingredients có thể được tạo cùng lúc (trong cùng request) hoặc<br>thêm riêng lẻ sau qua FR-RCP-009/010.|
|**Điều kiện tiên quyết**|1. Người dùng đang đăng nhập với role Author hoặc Admin. 2.<br>CategoryId tham chiếu đến danh mục đã tồn tại.|
||1. Author gửi POST /api/v1/recipes với body: { title, description,<br>categoryId, prepTimeMinutes, cookTimeMinutes, servings, difficulty,<br>instructions?, nutrition?: {...}, steps?: [...], ingredients?: [...] }.<br>2. Kiểm tra xác thực (RequireAuthorization).|
|**Luồng chính (Happy**<br>**Path)**|3. CreateRecipeCommand dispatch.<br>4. ValidationBehavior: title 5–200 ký tự, prepTime/cookTime/servings ><br>0, categoryId valid Guid.<br>5. SlugHelper.Generate(title), kiểm tra slug unique.<br>6. Recipe.Create(title, description, categoryId, authorId, prepTime,<br>cookTime, servings, difficulty).<br>7. Nếu có steps: thêm từng RecipeStep.Create() vào recipe.Steps.<br>8. Nếu có ingredients: thêm từng RecipeIngredient.Create() vào<br>recipe.Ingredients.<br>9. Nếu có nutrition: recipe.SetNutrition(calories, protein, carbs, fat).<br>10. `IRecipeRepository.AddAsync(recipe)`, sau đó `IDataSession.SaveChangesAsync()`.<br>11. Invalidate Output Cache tag "recipes".<br>12. Trả về HTTP 201 Created với RecipeDto.|


|---|---|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Không có quyền Author/Admin: HTTP 401/403.<br>A2 – CategoryId không tồn tại: HTTP 422 với lỗi "Category không hợp<br>lệ.".<br>A3 – Slug đã tồn tại (title trùng): HTTP 409 Conflict.|
|**HTTP Method &**<br>**Endpoint**|POST  /api/v1/recipes|
|**Kết quả mong đợi**|Recipe mới được tạo với Status = Draft, Slug được sinh tự động.<br>Cache "recipes" bị invalidate.|
|**HTTP Status Code trả**<br>**về**|201 Created – Tạo thành công. 401/403 – Chưa đăng nhập / Không có<br>quyền. 409 Conflict – Slug đã tồn tại. 422 Unprocessable Entity – Dữ<br>liệu không hợp lệ.|



##### **FR-RCP-004: Cập nhật Công thức [Author-Owner/Admin]** 

|**Mã yêu cầu**|FR-RCP-004|
|---|---|
|**Tên yêu cầu**|Cập nhật Thông tin Công thức Nấu ăn|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả sở hữu (Author – Owner) / Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Cập nhật thông tin của một công thức. Resource-Based Authorization<br>được áp dụng: chỉ Author sở hữu recipe (AuthorId == currentUserId)<br>hoặc Admin được phép. Concurrency control dùng `Version` (`xmin`) theo ETag pattern: client phải gửi version hiện tại trong If-Match header; nếu mismatch → conflict.|
|**Điều kiện tiên quyết**|1. Author/Admin đang đăng nhập. 2. Recipe với ID tương ứng tồn tại.<br>3. Client cung cấp `Version` hợp lệ trong If-Match header (hoặc<br>trong request body).|
||1. Author gửi PUT /api/v1/recipes/{id} với body: { title, description,<br>categoryId, prepTime, cookTime, servings, difficulty, instructions,<br>nutrition? }.<br>2. Kiểm tra xác thực.<br>3. UpdateRecipeCommand dispatch.|
|**Luồng chính (Happy**<br>**Path)**|4. Lấy recipe từ database theo ID.<br>5. IAuthorizationService.AuthorizeAsync(user, recipe,<br>Operations.Update) – kiểm tra resource-based auth.<br>6. Gán original value của concurrency token từ `Version`; DbContext sẽ ném DbUpdateConcurrencyException nếu `xmin` không còn khớp.<br>7. Update các field của recipe entity qua domain method<br>recipe.Update(...).<br>8. Cập nhật Nutrition nếu có.<br>9. SaveChangesAsync() – nếu version mismatch tại đây → ném<br>ConcurrencyException → HTTP 422.<br>10. Invalidate cache: EvictByTagAsync("recipes"),<br>EvictByTagAsync($"recipe:{slug}").<br>11. Trả về HTTP 200 OK với RecipeDto đã cập nhật.|


|---|---|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Không phải owner (Author khác): HTTP 403 Forbidden.<br>A2 – Concurrency conflict (`Version`/`xmin` mismatch): HTTP 422 Unprocessable Entity – "Dữ liệu đã bị thay đổi bởi người dùng khác."<br>A3 – ID không tồn tại: HTTP 404.|
|**HTTP Method &**<br>**Endpoint**|PUT  /api/v1/recipes/{id:guid}|
|**Kết quả mong đợi**|Recipe được cập nhật, cache bị invalidate, trả về RecipeDto mới nhất.|
|**HTTP Status Code trả**<br>**về**|200 OK. 403 Forbidden – Không phải owner. 404 Not Found. 409 Conflict – slug trùng. 422 Unprocessable Entity – concurrency conflict.|



##### **FR-RCP-005: Xuất bản / Hủy Xuất bản Công thức** 

|**Mã yêu cầu**|FR-RCP-005|
|---|---|
|**Tên yêu cầu**|Xuất bản (Publish) / Hủy Xuất bản (Unpublish) Công thức|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả sở hữu (Author – Owner) / Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Thay đổi trạng thái công thức: Draft → Published (xuất bản) hoặc<br>Published → Draft (hủy xuất bản). Business rule: KHÔNG thể publish<br>nếu recipe không có ít nhất 1 bước thực hiện (Steps.Count > 0) VÀ ít nhất 1 nguyên liệu (Ingredients.Count > 0). Khi<br>publish, Recipe trở nên công khai và được đưa vào index tìm kiếm.|
|**Điều kiện tiên quyết**|1. Recipe tồn tại, người dùng là owner hoặc Admin. 2. Để publish:<br>recipe phải có ít nhất 1 RecipeStep VÀ ít nhất 1 RecipeIngredient.|
||1. Author gửi PATCH /api/v1/recipes/{id}/publish (để xuất bản) hoặc<br>PATCH /api/v1/recipes/{id}/unpublish.<br>2. PublishRecipeCommand dispatch với isPublish = true/false.<br>3. Kiểm tra resource-based authorization.|
|**Luồng chính (Happy**<br>**Path)**|4. Gọi domain method: recipe.Publish() hoặc recipe.Unpublish().<br>5. recipe.Publish() kiểm tra: Steps.Count == 0 → Throw DomainException("Recipe phải có ít nhất 1 bước thực hiện."); Ingredients.Count == 0 → Throw DomainException("Recipe phải có ít nhất 1 nguyên liệu.").<br>6. Set Status = Published/Draft, UpdatedAt = DateTime.UtcNow.<br>7. SaveChangesAsync(), invalidate cache.<br>8. Trả về HTTP 200 OK với RecipeDto.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Recipe không có bước thực hiện hoặc không có nguyên liệu: HTTP 422 với<br>DomainException message (RECIPE_PUBLISH_INCOMPLETE).<br>A2 – Recipe đã ở trạng thái mong muốn: Idempotent, trả về HTTP 200<br>OK.|


|**HTTP Method &**|PATCH  /api/v1/recipes/{id:guid}/publish  |  PATCH|
|---|---|
|**Endpoint**|/api/v1/recipes/{id:guid}/unpublish|
|**Kết quả mong đợi**|Status recipe được thay đổi thành Published hoặc Draft. Cache bị<br>invalidate.|
|**HTTP Status Code trả**<br>**về**|200 OK – Thành công. 403 Forbidden. 404 Not Found. 422<br>Unprocessable Entity – Thiếu steps.|



##### **FR-RCP-006: Lưu trữ Công thức (Archive)** 

|**Mã yêu cầu**|FR-RCP-006|
|---|---|
|**Tên yêu cầu**|Lưu trữ Công thức (Archive / Unarchive)|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả sở hữu / Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|S – Should Have|
|**Mô tả**|Chuyển Recipe sang trạng thái Archived. Recipe Archived không hiển<br>thị trong danh sách công khai nhưng không bị xóa khỏi database (soft<br>hide). Hữu ích để ẩn recipe cũ không còn phù hợp mà không mất dữ<br>liệu.|
|**Điều kiện tiên quyết**|1. Recipe tồn tại, người dùng có quyền.|
|**Luồng chính (Happy**<br>**Path)**|1. Author gửi PATCH /api/v1/recipes/{id}/archive.<br>2. Kiểm tra authorization.<br>3. recipe.Archive() → Status = Archived.<br>4. SaveChangesAsync(), invalidate cache.<br>5. HTTP 200 OK.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – ID không tồn tại: HTTP 404. A2 – Không có quyền: HTTP 403.|
|**HTTP Method &**<br>**Endpoint**|PATCH  /api/v1/recipes/{id:guid}/archive|
|**Kết quả mong đợi**|Status = Archived. Recipe không còn xuất hiện trong public listing.|
|**HTTP Status Code trả**<br>**về**|200 OK. 403 Forbidden. 404 Not Found.|



**FR-RCP-007: Xóa Công thức [Author-Owner/Admin]** 

|**Mã yêu cầu**|FR-RCP-007|
|---|---|
|**Tên yêu cầu**|Xóa Công thức Nấu ăn (Soft Delete)|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả sở hữu (Author – Owner) / Quản trị viên (Admin)|
|**Mức ưu tiên**|M – Must Have|
|**(MoSCoW)**||
|**Mô tả**|Xóa logic một công thức bằng cách đặt `IsDeleted=true`. Recipe và dữ liệu con không còn xuất hiện trong truy vấn thông thường nhưng vẫn được giữ để bảo toàn dữ liệu. Việc xóa vật lý Recipe, Steps, Ingredients, Images và file MinIO chỉ do tác vụ bảo trì riêng thực hiện sau thời hạn lưu giữ; khi đó dữ liệu con được hard delete theo cascade.|


|---|---|
|**Điều kiện tiên quyết**|1. Recipe tồn tại. 2. Người dùng là owner hoặc Admin.|
|**Luồng chính (Happy**<br>**Path)**|1. Author/Admin gửi DELETE /api/v1/recipes/{id}.<br>2. Kiểm tra xác thực và resource-based authorization.<br>3. Gọi domain method `recipe.Delete(timeProvider.GetUtcNow())` để đặt `IsDeleted=true`.<br>4. Commit thay đổi bằng `IDataSession.SaveChangesAsync`; global query filter sẽ loại Recipe khỏi các truy vấn thông thường.<br>5. Giữ nguyên Steps, Ingredients, Images và file MinIO cho đến khi tác vụ bảo trì dọn dẹp vật lý sau thời hạn lưu giữ.<br>6. EvictByTagAsync("recipes"),<br>EvictByTagAsync($"recipe:{recipe.Slug}") – invalidate cache.<br>7. Trả về HTTP 204 No Content.|
||A1 – ID không tồn tại: HTTP 404.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A2 – Không phải owner: HTTP 403.<br>A3 – Recipe đã soft-delete: xử lý idempotent bằng HTTP 204 hoặc trả HTTP 404 theo query policy đã chọn và áp dụng nhất quán.|
|**HTTP Method &**<br>**Endpoint**|DELETE  /api/v1/recipes/{id:guid}|
|**Kết quả mong đợi**|Recipe được đánh dấu đã xóa và không còn xuất hiện trong API thông thường; dữ liệu con và file ảnh được giữ đến tác vụ dọn dẹp vật lý.|
|**HTTP Status Code trả**<br>**về**|204 No Content – Xóa thành công. 403 Forbidden. 404 Not Found.|



##### **FR-RCP-008: Quản lý Ảnh Công thức (Upload / Set Primary / Delete)** 

|**Mã yêu cầu**|FR-RCP-008|
|---|---|
|**Tên yêu cầu**|Upload Ảnh, Đặt Ảnh Chính, Xóa Ảnh Công thức|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả sở hữu / Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Author quản lý ảnh minh họa cho công thức của mình. Upload dùng<br>multipart/form-data. Ảnh được lưu trên MinIO với path:<br>recipes/{recipeId}/{uuid}.{ext}. Ảnh đầu tiên tự động được đặt làm ảnh<br>chính (IsPrimary = true). Hỗ trợ 3 thao tác: Upload (POST), đặt ảnh<br>chính (PATCH primary), Xóa (DELETE). Validation bắt buộc: MIME<br>type (image/jpeg, image/png, image/webp, image/avif) và kích thước tối<br>đa 5MB.|
|**Điều kiện tiên quyết**|1. Author/Admin đang đăng nhập. 2. Recipe tồn tại và người dùng có<br>quyền.|
|**Luồng chính (Happy**<br>**Path)**|--- UPLOAD ---<br>1. POST /api/v1/recipes/{id}/images với multipart/form-data chứa field<br>"file".<br>2. Validate MIME type: chỉ chấp nhận image/jpeg, image/png,<br>image/webp, image/avif.<br>3. Validate kích thước: file.Length <= 5*1024*1024 bytes (5MB).<br>4. Validate magic bytes: đọc 4 bytes đầu để xác nhận định dạng thực<br>sự (JPEG: FF D8 FF; PNG: 89 50 4E 47).<br>5. IFileStorageService.UploadAsync(file, "recipes/{id}") → trả về URL<br>công khai.<br>6. RecipeImage.Create(url, altText, isPrimary: !recipe.Images.Any()) →<br>thêm vào recipe.<br>7. SaveChangesAsync(), invalidate cache.<br>8. HTTP 201 Created với { url, isPrimary }.<br>--- SET PRIMARY IMAGE ---<br>9. PATCH /api/v1/recipes/{id}/images/{imageId}/primary.<br>10. Tìm image theo imageId, đặt IsPrimary = true, đặt tất cả ảnh khác<br>IsPrimary = false.<br>11. HTTP 200 OK.|


|---|---|
||--- DELETE IMAGE ---<br>12. DELETE /api/v1/recipes/{id}/images/{imageId}.<br>13. Xóa entity khỏi database.<br>14. BackgroundJob.Enqueue xóa file trên MinIO.<br>15. Nếu ảnh bị xóa là IsPrimary và còn ảnh khác: tự động đặt ảnh đầu<br>tiên còn lại làm primary.<br>16. HTTP 204 No Content.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – MIME type không hợp lệ: HTTP 400 Bad Request.<br>A2 – File vượt quá 5MB: HTTP 400 với message "Kích thước file vượt<br>quá giới hạn 5MB.".<br>A3 – Magic bytes không khớp MIME type: HTTP 400 "File không hợp<br>lệ.".<br>A4 – MinIO không khả dụng: HTTP 503 Service Unavailable.|
|**HTTP Method &**<br>**Endpoint**|POST /api/v1/recipes/{id}/images  |  PATCH<br>/api/v1/recipes/{id}/images/{imgId}/primary  |  DELETE<br>/api/v1/recipes/{id}/images/{imgId}|
|**Kết quả mong đợi**|Ảnh được upload lên MinIO, URL lưu vào database. IsPrimary được<br>quản lý chính xác.|
|**HTTP Status Code trả**<br>**về**|Upload: 201 Created. Set Primary: 200 OK. Delete: 204 No Content.<br>400 Bad Request – File không hợp lệ. 403/404 – Lỗi quyền/không tìm<br>thấy.|



##### **FR-RCP-009: Quản lý Nguyên liệu (CRUD RecipeIngredient)** 

|**Mã yêu cầu**|FR-RCP-009|
|---|---|
|**Tên yêu cầu**|Thêm / Cập nhật / Xóa Nguyên liệu Công thức|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả sở hữu / Quản trị viên (Admin)|


|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|---|---|
|**Mô tả**|Author quản lý danh sách nguyên liệu (RecipeIngredient) của công<br>thức. Mỗi nguyên liệu có: Name (tên), Quantity (số lượng), Unit (đơn vị:<br>gram/ml/muỗng/cái/củ...), Notes (ghi chú tuỳ chọn), SortOrder (thứ tự<br>hiển thị). Endpoint hỗ trợ thêm mới (POST), cập nhật (PUT), xóa<br>(DELETE) từng nguyên liệu riêng lẻ.|
|**Điều kiện tiên quyết**|1. Recipe tồn tại và người dùng có quyền. 2. Quantity > 0, Unit không<br>rỗng, Name 1–200 ký tự.|
|**Luồng chính (Happy**<br>**Path)**|--- THÊM NGUYÊN LIỆU ---<br>1. POST /api/v1/recipes/{id}/ingredients với body: { name, quantity, unit,<br>notes?, sortOrder? }.<br>2. Validate, tạo RecipeIngredient.Create(recipeId, name, qty, unit,<br>notes, sortOrder).<br>3. Tải Recipe qua `IRecipeRepository`, thêm ingredient qua aggregate và commit bằng `IDataSession.SaveChangesAsync()`.<br>4. HTTP 201 Created.<br>--- CẬP NHẬT NGUYÊN LIỆU ---<br>5. PUT /api/v1/recipes/{id}/ingredients/{ingId} với body fields cần cập<br>nhật.<br>6. Tìm ingredient, cập nhật, SaveChangesAsync(). HTTP 200 OK.<br>--- XÓA NGUYÊN LIỆU ---<br>7. DELETE /api/v1/recipes/{id}/ingredients/{ingId}.<br>8. Xóa entity, SaveChangesAsync(). HTTP 204 No Content.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Recipe/Ingredient không tồn tại: HTTP 404. A2 – Không có quyền:<br>HTTP 403. A3 – Dữ liệu không hợp lệ: HTTP 422.|
|**HTTP Method &**<br>**Endpoint**|POST/PUT/DELETE  /api/v1/recipes/{id}/ingredients/{ingId?}|
|**Kết quả mong đợi**|Danh sách nguyên liệu được cập nhật chính xác. Cache bị invalidate.|
|**HTTP Status Code trả**<br>**về**|201/200/204 – Thành công. 403/404/422 – Lỗi tương ứng.|



##### **FR-RCP-010: Quản lý Các bước Thực hiện (CRUD RecipeStep)** 

|**Mã yêu cầu**|FR-RCP-010|
|---|---|
|**Tên yêu cầu**|Thêm / Cập nhật / Xóa Bước Thực hiện Công thức|
|**Nhóm chức năng**|Module Quản lý Công thức Nấu ăn (FR-RCP)|
|**Tác nhân**|Tác giả sở hữu / Quản trị viên (Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Author quản lý các bước thực hiện (RecipeStep) của công thức. Mỗi<br>bước có: StepNumber (thứ tự, tự động tăng), Description (mô tả bước),<br>TimerMinutes (thời gian ước tính cho bước, tùy chọn), ImageUrl<br>(ảnh minh họa cho bước riêng, tùy chọn). Khi xóa một bước, hệ thống<br>tự động renumber các bước còn lại để đảm bảo StepNumber liên tục<br>(1, 2, 3...).|


|---|---|
|**Điều kiện tiên quyết**|1. Recipe tồn tại, người dùng có quyền. 2. Description không rỗng, tối<br>đa 2000 ký tự.|
|**Luồng chính (Happy**<br>**Path)**|1. POST /api/v1/recipes/{id}/steps với body: { description,<br>TimerMinutes?, imageUrl? }.<br>2. StepNumber = recipe.Steps.Max(s => s.StepNumber) + 1 (hoặc 1<br>nếu chưa có bước nào).<br>3. RecipeStep.Create(recipeId, stepNumber, description,<br>TimerMinutes).<br>4. SaveChangesAsync(). HTTP 201 Created.<br>--- XÓA BƯỚC ---<br>5. DELETE /api/v1/recipes/{id}/steps/{stepId}.<br>6. Xóa step, sau đó renumber: cập nhật StepNumber của tất cả steps<br>còn lại theo thứ tự.<br>7. SaveChangesAsync(). HTTP 204 No Content.|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Recipe không tồn tại: HTTP 404. A2 – Không có quyền: HTTP<br>403.|
|**HTTP Method &**<br>**Endpoint**|POST/PUT/DELETE  /api/v1/recipes/{id}/steps/{stepId?}|
|**Kết quả mong đợi**|Danh sách steps được cập nhật với StepNumber liên tục. Cache bị<br>invalidate.|
|**HTTP Status Code trả**<br>**về**|201/200/204 – Thành công. 403/404/422 – Lỗi.|



#### **3.4. Module Tìm kiếm và Phân trang (FR-SRCH)** 

**FR-SRCH-001: Tìm kiếm Toàn văn bản (Full-Text Search)** 

|**Mã yêu cầu**|FR-SRCH-001|
|---|---|
|**Tên yêu cầu**|Tìm kiếm Toàn văn bản Công thức (Full-Text Search)|
|**Nhóm chức năng**|Module Tìm kiếm và Phân trang (FR-SRCH)|
|**Tác nhân**|Tất cả (Guest / Author / Admin)|
|**Mức ưu tiên**<br>**(MoSCoW)**|M – Must Have|
|**Mô tả**|Hệ thống cung cấp tính năng tìm kiếm toàn văn bản (FTS) cho công<br>thức sử dụng PostgreSQL tsvector/tsquery với cấu hình tiếng Việt.<br>Trường SearchVector (computed column) được tự động cập nhật bởi<br>PostgreSQL trigger khi Title hoặc Description thay đổi. Kết quả được<br>xếp hạng bởi ts_rank(). Hỗ trợ tìm kiếm gần đúng với unaccent<br>extension (bỏ dấu tiếng Việt: "pho" tìm được "phở").|
|**Điều kiện tiên quyết**|1. PostgreSQL extensions unaccent và pg_trgm đã được install. 2. GIN<br>index trên cột SearchVector đã được tạo. 3. Tham số q không rỗng, tối<br>thiểu 2 ký tự.|


|**Luồng chính (Happy**<br>**Path)**|1. Client gửi GET<br>/api/v1/recipes/search?q=pho+bo&page=1&pageSize=10.<br>2. SearchRecipesQuery dispatch với SearchTerm = "pho bo", Page =<br>1, PageSize = 10.<br>3. Handler xây dựng tsquery từ search terms: "pho:* & bo:*" (prefix<br>matching).<br>4. LINQ query với EF Core: .Where(r =><br>r.SearchVector.Matches(EF.Functions.ToTsQuery("vietnamese",<br>query))).<br>5. Apply ORDER BY ts_rank(SearchVector, query) DESC để kết quả<br>liên quan nhất lên đầu.<br>6. Chỉ trả về Status == Published recipes.<br>7. Apply pagination, trả về PagedResult<RecipeSummaryDto> với field<br>relevanceScore.<br>8. Kết quả KHÔNG cache (vì query string đa dạng) hoặc cache ngắn (5<br>phút) với vary by query.|
|---|---|
|**Luồng thay thế /**<br>**Ngoại lệ**|A1 – Query rỗng hoặc < 2 ký tự: HTTP 422.<br>A2 – Không tìm thấy kết quả: HTTP 200 với items = [] và message gợi<br>ý.<br>A3 – Ký tự đặc biệt trong query (SQL injection attempt): EF Core<br>parameterize tự động; tsquery sanitization loại bỏ ký tự nguy hiểm.|
|**HTTP Method &**<br>**Endpoint**|GET  /api/v1/recipes/search?q={searchTerm}&page={n}&pageSize={n}|
|**Kết quả mong đợi**|PagedResult<RecipeSummaryDto> được xếp hạng theo độ liên quan<br>(ts_rank). Hỗ trợ tìm kiếm không dấu tiếng Việt.|
|**HTTP Status Code trả**<br>**về**|200 OK – Thành công (kể cả kết quả rỗng). 422 – Query không hợp lệ.|



##### **FR-SRCH-002/003/004: Lọc, Sắp xếp và Phân trang (Tóm tắt)** 

Ba FR còn lại của module Search được tích hợp sẵn vào FR-RCP-001 và FR-SRCH-001. Bảng tóm tắt: 

|**Mã FR**|**Tên**|**Tham số Query**|**Mô tả**|
|---|---|---|---|
|FR-<br>SRCH-<br>002|Lọc công thức|categoryId={guid}<br>difficulty={Easy|Medium|Hard}<br>maxCookTime={minutes}<br>minServings={n}|Lọc kết quả theo một hoặc<br>nhiều tiêu chí. Các filter kết<br>hợp bằng AND logic.|
|FR-<br>SRCH-<br>003|Sắp xếp kết quả|sort={field} VD: sort=createdAt<br>(ASC)     sort=-createdAt<br>(DESC)     sort=title, sort=-<br>cookTime|Tiền tố "-" = descending.<br>Mặc định: sort=-createdAt<br>(mới nhất trước).|
|FR-<br>SRCH-<br>004|Phân trang<br>(Offset-based)|page={n} (default: 1)<br>pageSize={n} (default: 12, max:<br>50)|Offset-based pagination<br>(SKIP/TAKE). Response<br>bao gồm totalCount,<br>totalPages, hasNextPage,<br>hasPreviousPage.|



#### **3.5. Module Quản lý Tệp tin (FR-FILE)** 
Module xử lý tất cả thao tác với file binary trên hệ thống lưu trữ đối tượng (Object Storage) MinIO S3-compatible. Abstraction layer IFileStorageService cho phép swap implementation (MinIO ↔ AWS S3 ↔ local filesystem) mà không cần thay đổi Application Layer. 

|**Mã FR**|**Tên**|**Mô tả**|**Ràng buộc kỹ thuật**|
|---|---|---|---|
|FR-<br>FILE-<br>001|Upload File<br>lên MinIO|IFileStorageService.UploadAsync(IFormFile,<br>folder, ct) → string (public URL). Tạo unique<br>filename = {folder}/{Guid.NewGuid()}{ext} để<br>ngăn path traversal. Preserve MIME type<br>gốc.|Max size: 5MB. MIME:<br>JPEG/PNG/WebP/AVIF.<br>Magic bytes validation.<br>Bucket: "culinary-blog".<br>Policy: public-read.|
|FR-<br>FILE-<br>002|Xóa File<br>khỏi MinIO|IFileStorageService.DeleteAsync(fileUrl, ct).<br>Trích xuất object name từ URL, gọi<br>RemoveObjectAsync(). Thường được gọi từ<br>Hangfire background job (fire-and-forget)<br>sau khi xóa recipe.|Nếu object không tồn tại<br>trên MinIO → không<br>throw exception<br>(idempotent). Lỗi kết nối<br>MinIO → Hangfire retry<br>tối đa 3 lần.|



#### **3.6. Module Background Jobs (FR-JOB)** 

Module xử lý các tác vụ nền không đồng bộ sử dụng Hangfire. Hangfire chạy in-process trong .NET API và sử dụng PostgreSQL làm persistent storage cho job queue. Dashboard quản lý jobs tại /hangfire (chỉ Admin). Hỗ trợ 3 loại job: Fire-and-forget (chạy ngay), Delayed (chạy sau N giây/phút) và Recurring (lịch cron). 

|**Mã FR**|**Tên Job**|**Loại**|**Trigger**|**Mô tả**|**Retry Policy**|
|---|---|---|---|---|---|
|FR-<br>JOB-<br>001|Welcome<br>Email Job|Fire-and-<br>forget|Sau FR-AUTH-001 thành<br>công<br>(BackgroundJob.Enqueue)|Gửi email HTML<br>chào mừng đến<br>địa chỉ email vừa<br>đăng ký. Email<br>template bao gồm:<br>tên người dùng,<br>link kích hoạt email<br>(nếu cần), link đến<br>ứng dụng.|Tự động retry<br>3 lần với<br>exponential<br>backoff (1<br>phút, 5 phút,<br>30 phút). Sau 3<br>lần fail →<br>chuyển sang<br>Failed state,<br>log error.|
|FR-<br>JOB-<br>002|Image Resize<br>/ Thumbnail<br>Job|Fire-and-<br>forget|Sau FR-RCP-008 upload<br>ảnh thành công|Tạo thumbnail<br>(300x300px) và<br>medium image<br>(800x600px) từ<br>ảnh gốc. Lưu cả 3<br>phiên bản lên<br>MinIO. Cập nhật<br>URLs vào<br>database.|Retry 3 lần.<br>Nếu fail: ảnh<br>gốc vẫn hiển<br>thị, chỉ thiếu<br>thumbnail.|
|FR-<br>JOB-<br>003|Sitemap<br>Generation<br>Job|Recurring|Hàng ngày lúc 02:00 AM<br>UTC (cron: "0 2 * * *")|Tạo file<br>sitemap.xml chứa<br>URL tất cả<br>Published recipes,<br>categories và<br>pages tĩnh. Upload<br>sitemap.xml lên<br>MinIO hoặc lưu|Retry 2 lần nếu<br>fail. Log kết<br>quả (số URL<br>trong sitemap)<br>qua Serilog.|


|**Mã FR**|**Tên Job**|**Loại**|**Trigger**|**Mô tả**|**Retry Policy**|
|---|---|---|---|---|---|
|||||vào wwwroot. Gửi<br>thông báo đến<br>Google Search<br>Console (ping).||



#### **3.7. Module Quan sát Hệ thống (FR-OBS)** 

Module cung cấp khả năng quan sát (Observability) toàn diện theo ba trụ cột: Logging (Serilog), Metrics (OpenTelemetry), và Distributed Tracing (OpenTelemetry). Đây là yêu cầu bắt buộc cho production deployment. 

|**Mã FR**|**Tên**|**Mô tả**|**Kỹ thuật / Công cụ**|
|---|---|---|---|
|FR-<br>OBS-<br>001|Health Check<br>Endpoints|Hệ thống cung cấp 3 endpoint<br>health check với mục đích khác<br>nhau: • GET /health – tổng hợp<br>tất cả components (database,<br>Redis, MinIO). • GET<br>/health/live – Liveness probe<br>(chỉ kiểm tra process còn<br>sống). • GET /health/ready –<br>Readiness probe (kiểm tra kết<br>nối database và Redis).|IHealthCheck,<br>AspNetCore.HealthChecks.NpgSql,<br>AspNetCore.HealthChecks.Redis,<br>AspNetCore.HealthChecks.Minio.<br>Liveness chỉ trả healthy. Readiness<br>fail khi DB/Redis down →<br>Kubernetes/Nginx ngừng route<br>traffic.|
|FR-<br>OBS-<br>002|Structured<br>Logging|Mọi HTTP request được log<br>với: CorrelationId (X-<br>Correlation-ID header), HTTP<br>method/path/status, elapsed<br>time (ms), UserId (khi đã xác<br>thực). MediatR Pipeline<br>Behavior (LoggingBehavior) log<br>tất cả Commands/Queries vào.<br>Performance alert khi request ><br>500ms.|Serilog + CorrelationIdMiddleware.<br>Sinks: Console (structured JSON),<br>File (rolling daily), Seq<br>(development). Log levels: Debug<br>(development), Information<br>(production), Warning/Error (luôn<br>luôn).|
|FR-<br>OBS-<br>003|Distributed<br>Tracing &<br>Metrics|OpenTelemetry instrumentation<br>cho: HTTP request traces<br>(ActivitySource), EF Core<br>database operation traces,<br>custom business metrics<br>(recipe created/published<br>count). Traces được export đến<br>Seq (development) hoặc<br>Jaeger/Grafana Tempo<br>(production).|OpenTelemetry .NET SDK, OTLP<br>exporter. Activity.TraceId được<br>include trong structured log (log<br>correlation với trace). Metrics:<br>request count, duration histogram,<br>error rate.|


## **4. Yêu cầu Phi Chức năng (NFR)** 

Phần này mô tả các thuộc tính chất lượng hệ thống theo mô hình ISO/IEC 25010 (FURPS+). Mỗi yêu cầu phi chức năng được gán mã định danh, mức ưu tiên và tiêu chí đo lường định lượng cụ thể. Các NFR này ràng buộc thiết kế kiến trúc và lựa chọn công nghệ toàn bộ hệ thống. 

|**Mã NFR**|**Danh mục**|**Số yêu**<br>**cầu**|**Ưu tiên**|
|---|---|---|---|
|NFR-PERF|Hiệu năng (Performance)|5|Cao|
|NFR-SEC|Bảo mật(Security)|7|Rất cao|
|NFR-USE|Khả năng sử dụng<br>(Usability)|4|Trung<br>bình|
|NFR-REL|Độ tin cậy (Reliability)|3|Cao|
|NFR-<br>MAINT|Khả năng bảo trì<br>(Maintainability)|4|Trung<br>bình|
|NFR-<br>SCALE|Khả năng mở rộng<br>(Scalability)|3|Cao|
|NFR-SEO|Tối ưu SEO (SEO)|4|Cao|



#### **4.1. Hiệu năng (NFR-PERF)** 

Toàn bộ các chỉ số hiệu năng được đo trong môi trường production với tải thực tế. Các ngưỡng dưới đây áp dụng cho trường hợp cache warm (Redis hit rate ≥ 80%). 

|**NFR-PERF-001**<br>**Response Time API**|Thời gian phản hồi API: • p50 ≤ 150ms — cho tất cả GET<br>endpoints với dữ liệu cache. • p95 ≤ 500ms — cho tất cả API<br>endpoints (kể cả write operations). • p99 ≤ 1000ms — không<br>vượt quá 1 giây trong mọi trường hợp. Đo bằng:<br>OpenTelemetry+ Grafana / k6 load test.|
|---|---|
|**NFR-PERF-002**<br>**Throughput**|Hệ thống xử lý đồng thời ≥ 100 concurrent users mà không<br>degradation: • Trên phần cứng: 2 vCPU, 4GB RAM (single<br>instance). • Horizontal scaling: thêm instance tăng tuyến tính.<br>Đo bằng: k6 smoke test → load test → stress test.|
|**NFR-PERF-003 Cache**<br>**Effectiveness**|Redis Cache hit rate ≥ 80% trong điều kiện steady-state. Các<br>đối tượng cache: • Category list: TTL = 60 phút (ít thay đổi, đồng nhất với FR-CAT-001). •<br>Recipe detail: TTL = 5 phút (cache-aside pattern). • Search<br>results: TTL = 1 phút. Cache invalidation: Event-driven — xóa<br>cache khi Create/Update/Delete.|
|**NFR-PERF-004**<br>**Database Query**|Tất cả queries đến PostgreSQL: • Không có N+1 query<br>problem — bắt buộc dùng .Include()/.ThenInclude() và<br>projection. • Index: đảm bảo mọi WHERE/ORDER BY column<br>đều có B-tree index tương ứng. • Slow query log: cảnh báo khi<br>query > 100ms (Serilog performance behavior). • EXPLAIN<br>ANALYZE:phảipass review trước khi merge.|


|---|---|
|**NFR-PERF-005**|Next.js frontend đạt chuẩn Google Core Web Vitals (đo bằng|
|**Frontend**|Lighthouse CI): • LCP (Largest Contentful Paint) ≤ 2.5s. • CLS|
|**Performance (Core**<br>**Web Vitals)**|(Cumulative Layout Shift) ≤ 0.1. • INP (Interaction to Next<br>Paint) ≤ 200ms. • First Load JS Bundle ≤ 200KB (gzipped). Kỹ<br>thuật: ISR (Incremental Static Regeneration), Image<br>Optimization (next/image), Code Splitting.|



#### **4.2. Bảo mật (NFR-SEC)** 

Toàn bộ yêu cầu bảo mật tuân thủ OWASP Top 10 (2021) và được kiểm thử qua security review trước khi release production. 

|**NFR-SEC-001**<br>**Password & Hashing**|Mật khẩu phải được hash bằng ASP.NET Core Identity mặc<br>định (PBKDF2-HMACSHA512, iteration count ≥ 100.000).<br>Không bao giờ lưu plaintext password. Yêu cầu độ phức tạp: ≥<br>8 ký tự, chứa ít nhất 1 chữ hoa + 1 chữ thường + 1 số + 1 ký<br>tự đặc biệt(cấu hìnhqua IdentityOptions.Password).|
|---|---|
|**NFR-SEC-002 JWT**<br>**Token Security**|Access Token: JWT signed bằng HS256, TTL = 15 phút, claim:<br>userId, email, roles, jti. Refresh Token: 128-bit<br>cryptographically secure random bytes, hash SHA-256 trước<br>khi lưu DB, TTL = 7 ngày. Rotation: Refresh token bị revoke<br>ngay sau khi dùng, cấp token mới (Refresh Token Rotation).<br>Detection: Nếu refresh token đã bị revoke được dùng lại →<br>revoke toàn bộ family (Reuse Detection).|
|**NFR-SEC-003 Rate**<br>**Limiting**|Giới hạn yêu cầu theo IP để ngăn brute force và DDoS: • Auth<br>endpoints (/auth/*): 10 request/phút/IP. • API chung: 100<br>request/phút/IP. • Upload endpoints: 5 request/phút/IP.<br>Implementation: ASP.NET Core Rate Limiting middleware<br>(Fixed Window, sliding window cho auth). HTTP 429 khi vượt<br>giới hạn với Retry-After header.|
|**NFR-SEC-004 Input**<br>**Validation & File**<br>**Upload Security**|Toàn bộ input được validate tại Application Layer<br>(FluentValidation) TRƯỚC khi xử lý: • SQL Injection: EF Core<br>parameterized queries (không raw SQL với user input). • XSS:<br>Input sanitization + Content-Security-Policy header. • MIME<br>Validation: Đọc magic bytes (không tin vào Content-Type<br>header) khi upload. • File size: Kiểm tra trước khi read stream<br>(không buffer toàn bộ vào memory trước). • Path Traversal:<br>GUID-based filename generation (không dùng tên file của<br>user).|
|**NFR-SEC-005 HTTPS**<br>**& CORS**|Toàn bộ traffic phải qua HTTPS (TLS 1.2+): • Nginx: redirect<br>HTTP → HTTPS, HSTS header (max-age=31536000). •<br>CORS Policy: Chỉ cho phép origin được cấu hình qua<br>appsettings (không wildcard *). • Allowed Origins:<br>http://localhost:3000 (dev), https://domain.com (prod). •<br>Cookie: SameSite=Strict, Secure=true (nếu dùng cookie cho<br>refresh token).|


|**NFR-SEC-006**<br>**Authorization &**<br>**Resource Ownership**|Kiểm tra phân quyền tại Application Layer (không chỉ ở<br>Presentation Layer): • Authorization Handler:<br>RecipeAuthorizationHandler xác minh ResourceOwnership<br>(Author chỉ xóa recipe của mình). • Role-based policies:<br>"AuthorPolicy", "AdminPolicy" (không hardcode role string). •<br>Sensitive endpoints (DELETE, PATCH publish): double-check<br>user ID trước khi commit. • Audit trail: Log mọi write operation<br>với userId + timestamp (Serilog).|
|---|---|
|**NFR-SEC-007 Secrets**<br>**Management**|Không bao giờ commit secrets vào Git: • Development:<br>ASP.NET Core User Secrets (dotnet user-secrets). •<br>Production: Environment variables (Docker Compose env_file /<br>Kubernetes Secrets). • Rotation: Khuyến nghị rotate JWT<br>signing key mỗi 90 ngày. • Scanning: Pre-commit hook kiểm<br>tra với truffleHog/gitleaks.|



#### **4.3. Khả năng Sử dụng (NFR-USE)** 

|**NFR-USE-001**<br>**Responsive Design**|Giao diện hiển thị chính xác trên tất cả breakpoints: • Mobile:<br>320px – 767px (single column, touch-friendly). • Tablet: 768px<br>– 1199px (2-column grid). • Desktop: ≥ 1200px (full layout).<br>Framework: Tailwind CSS utility-first. Không sử dụng CSS<br>framework override. Kiểm thử: Chrome DevTools responsive<br>mode + BrowserStack(iOS, Android).|
|---|---|
|**NFR-USE-002**<br>**Accessibility (a11y)**|Tuân thủ WCAG 2.1 Level AA: • Semantic HTML5: <article>,<br><nav>, <main>, <aside>. • ARIA attributes: aria-label, aria-<br>expanded, role trên interactive elements. • Keyboard<br>navigation: tất cả chức năng dùng được bằng bàn phím (Tab,<br>Enter, Escape). • Color contrast ratio ≥ 4.5:1 (text) và ≥ 3:1 (UI<br>components). • Screen reader: test với NVDA (Windows) và<br>VoiceOver(macOS/iOS).|
|**NFR-USE-003 Error**<br>**Messages**|Thông báo lỗi phải rõ ràng và actionable: • API: trả về RFC<br>7807 Problem Details (type, title, status, detail, errors{}). •<br>Frontend: hiển thị ngay bên cạnh field lỗi (React Hook Form<br>inline validation). • Server errors (5xx): hiển thị thông báo thân<br>thiện, không lộ stack trace. • I18n-ready: error messages sử<br>dụngerror code(khônghardcode tiếngViệt/Anh).|
|**NFR-USE-004**<br>**Loading States**|Mọi async operation phải có visual feedback: • Loading<br>skeleton: hiển thị trong khi fetch data (không blank screen). •<br>Optimistic update: UI cập nhật ngay, rollback nếu API fail. •<br>Toast notification: xác nhận thành công/thất bại sau write<br>operation. • Progress indicator: upload ảnh hiển thị progress<br>bar (%) realtime.|



#### **4.4. Độ tin cậy (NFR-REL)** 
|**NFR-REL-001 Uptime**<br>**SLA**|Hệ thống có uptime ≥ 99.5% (≈ 3.65 giờ downtime/năm). •<br>Maintenance window: công bố trước 48 giờ qua banner thông<br>báo. • Health check: /health/ready probe mỗi 10 giây<br>(Kubernetes readiness probe). • Monitoring: Uptime Robot /<br>Better Uptime gửi alert khi down > 1 phút.|
|---|---|
|**NFR-REL-002 Error**<br>**Handling & Resilience**|Hệ thống xử lý lỗi gracefully, không crash toàn bộ: • Global<br>Exception Handler Middleware: bắt tất cả unhandled<br>exceptions → trả 500 Problem Details + log. • Database<br>connection pool: tự reconnect, timeout 30s. • Redis failover:<br>nếu Redis down → fallback database (không cache), không<br>throw exception. • Hangfire retry: mỗi job tối đa 3 retry với<br>exponential backoff. • Circuit Breaker: (tùy chọn nâng cao)<br>Polly cho external HTTP calls.|
|**NFR-REL-003 Data**<br>**Durability**|Dữ liệu không bị mất trong trường hợp restart hoặc crash: •<br>PostgreSQL WAL (Write-Ahead Logging): đảm bảo ACID. •<br>Backup: pg_dump tự động hàng ngày lúc 03:00 AM, lưu 30<br>ngày. • MinIO: dữ liệu file trên volume persistent (không<br>ephemeral container storage). • Refresh tokens: lưu DB<br>(không Redis) để survive restart. • Recipe và Category dùng `IsDeleted` để soft delete; dữ liệu con chỉ bị xóa vật lý theo cascade bởi tác vụ bảo trì sau thời hạn lưu giữ.|



#### **4.5. Khả năng Bảo trì (NFR-MAINT)** 

|**NFR-MAINT-001 Code**<br>**Quality**|Toàn bộ code phải pass static analysis trước khi merge: •<br>.NET: SonarAnalyzer, StyleCop, EditorConfig (indent, naming<br>conventions). • TypeScript/React: ESLint (Airbnb ruleset),<br>Prettier. • Không có compiler warnings trong build CI. • Code<br>review: ít nhất 1 reviewerphê duyệt Pull Request.|
|---|---|
|**NFR-MAINT-002 Test**<br>**Coverage**|Độ phủ test tối thiểu: • Unit tests: ≥ 80% line coverage<br>(Application layer commands, queries, validators). • Integration<br>tests: tất cả API endpoints có ít nhất 1 happy path + 1 error<br>case. • E2E tests: 5 critical user flows (register, login, create<br>recipe, publish, search). Tool: xUnit (backend), Jest + Testing<br>Library (frontend), Playwright(E2E).|
|**NFR-MAINT-003**<br>**Documentation**|Tài liệu kỹ thuật bắt buộc: • README.md: hướng dẫn setup<br>dev environment (Docker Compose) trong < 5 phút. • API<br>documentation: tự động sinh từ XML comments +<br>Scalar/Swagger UI tại /scalar. • Architecture Decision Records<br>(ADR): ghi lại mọi quyết định kiến trúc quan trọng. •<br>CHANGELOG.md: cập nhật mỗi release (theo Keep a<br>Changelog+ SemVer).|
|**NFR-MAINT-004**<br>**Clean Architecture**<br>**Compliance**|Tuân thủ nghiêm ngặt dependency rules của Clean Architecture: • Domain layer chỉ dùng .NET BCL và không tham chiếu project/package orchestration khác. • Application layer tham chiếu Domain và được dùng MediatR/FluentValidation. Không reference Infrastructure. • Infrastructure layer tham chiếu Application và Domain để hiện thực ports. • Vi phạm được phát hiện qua architecture tests. • CQRS: Commands thay đổi state, Queries đọc data — không trộn lẫn.|



#### **4.6. Khả năng Mở rộng (NFR-SCALE)** 

|**NFR-SCALE-001**<br>**Stateless Backend**|API được thiết kế stateless để hỗ trợ horizontal scaling: • JWT<br>authentication (không session server-side). • Distributed cache<br>(Redis, không in-memory IMemoryCache) cho mọi shared<br>state. • Distributed lock (RedLock) cho các tác vụ singleton<br>(sitemap generation). • Hangfire: chạy với multiple workers<br>(IBackgroundJobServer), PostgreSQL làm sharedqueue.|
|---|---|
|**NFR-SCALE-002**<br>**Database Scaling**|Chiến lược database scaling: • Connection pooling: Npgsql<br>built-in pool (max 100 connections/instance). • Read replica<br>(tùy chọn): EF Core split queries + IQueryable routing qua<br>IDbContextFactory. • Index strategy: B-tree cho equality/range,<br>GIN cho full-text search (tsvector). • Table partitioning: (nâng<br>cao) partition Recipe byCreatedAt khi > 1 triệu rows.|
|**NFR-SCALE-003**<br>**Infrastructure Scaling**|Hạ tầng có thể scale theo chiều ngang: • Docker: mỗi service<br>là container riêng biệt (API, Postgres, Redis, MinIO, Nginx). •<br>Nginx: load balancer upstream pool cho nhiều API instances. •<br>MinIO: Distributed Mode (4+ nodes) cho production storage<br>scaling. • CDN: static assets (Next.js _next/static) được serve<br>qua CDN (Cloudflare).|



#### **4.7. Tối ưu SEO (NFR-SEO)** 

|**NFR-SEO-001**<br>**Structured Data**|Mỗi trang công thức nấu ăn phải có JSON-LD Schema.org<br>Recipe markup: • @type: "Recipe" • Thuộc tính: name,<br>description, image, author, datePublished, prepTime,<br>cookTime, totalTime, recipeYield, recipeIngredient[],<br>recipeInstructions[], nutrition. • Validate: Google Rich Results<br>Test — phải pass 100%. • Kết quả: Rich Snippets trên Google<br>Search (star rating, time, ingredients).|
|---|---|
|**NFR-SEO-002 Meta**<br>**Tags & Open Graph**|Mỗi trang phải có đầy đủ: • `\<title\>`: "{Recipe Name} \| Culinary<br>Blog" (≤ 60 ký tự). • `\<meta name="description"\>`: mô tả 150–<br>160 ký tự. • Open Graph: og:title, og:description, og:image<br>(1200×630px), og:url, og:type. • Twitter Card:<br>summary_large_image. • Canonical URL: tránh duplicate<br>content (slug-based URL). • Robots: index, follow (published) \|<br>noindex (draft/archived).|
|**NFR-SEO-003**<br>**Sitemap & Robots**|Sitemap XML tự động: • Sinh bởi FR-JOB-003 (Hangfire<br>Recurring Job, hàng ngày 02:00 AM UTC). • Bao gồm: tất cả<br>Published recipes + category pages + trang tĩnh. • Format:<br>sitemap.xml chuẩn, có `\<loc\>`, `\<lastmod\>`, `\<changefreq\>`,<br>`\<priority\>`. • Robots.txt: cho phép tất cả crawlers, khai báo<br>Sitemap URL. • Ping Google Search Console sau khi update<br>sitemap.|
|**NFR-SEO-004 URL**<br>**Structure**|URL phải thân thiện SEO: • Recipes: /recipes/{slug} — slug là<br>chữ thường, gạch nối, không dấu. • Categories:<br>/categories/{slug}. • Slug generation: tự tự động từ title, unique,<br>không thay đổi sau khi publish. • Redirect: Nếu slug thay đổi<br>(draft) → 301 redirect từ slug cũ sang slug mới. • Không dùng<br>query params cho nội dung chính (chỉ dùng cho<br>filter/sort/pagination).|
## **5. Yêu cầu Giao diện Ngoài** 

Chương này mô tả tất cả giao diện giữa hệ thống Culinary Blog với các thực thể bên ngoài: người dùng cuối, phần cứng, phần mềm bên thứ ba và giao tiếp mạng. Mọi giao tiếp đều qua HTTPS (TLS 1.2+) trong môi trường production. 

#### **5.1. Giao diện Người dùng (UI)** 

Hệ thống cung cấp giao diện web duy nhất trên nền Next.js App Router, hoạt động như Single Page Application (SPA) với Server-Side Rendering (SSR) và Incremental Static Regeneration (ISR). 

|**Màn hình / Route**|**Mô tả**|**Loại Rendering**|**Yêu cầu Auth**|
|---|---|---|---|
|/|Trang chủ: danh<br>sách recipe nổi bật<br>+ categories|ISR<br>(revalidate=3600)|Không|
|/recipes|Danh sách tất cả<br>recipes với<br>filter/sort/search|SSR (dynamic)|Không|
|/recipes/[slug]|Chi tiết recipe:<br>ingredients, steps,<br>nutrition, JSON-LD|ISR<br>(revalidate=300)|Không|
|/categories|Danh sách category|ISR<br>(revalidate=3600)|Không|
|/categories/[slug]|Danh sách recipe<br>theo category|ISR<br>(revalidate=600)|Không|
|/auth/login|Form đăng nhập<br>(email/password +<br>Google OAuth<br>button)|CSR|Không (redirect<br>nếu đã login)|
|/auth/register|Form đăng ký tài<br>khoản mới|CSR|Không|
|/dashboard|Trang tổng quan<br>của Author/Admin|CSR|Bắt buộc<br>(Author/Admin)|
|/dashboard/recipes|Quản lý danh sách<br>recipe của user|CSR|Bắt buộc|
|/dashboard/recipes/new|Form tạo recipe mới<br>(multi-stepwizard)|CSR|Bắt buộc<br>(Author/Admin)|
|/dashboard/recipes/[id]/edit|Form chỉnh sửa<br>recipe|CSR|Bắt buộc<br>(Owner/Admin)|
|/dashboard/categories|Quản lý categories<br>(chỉ Admin)|CSR|Bắt buộc<br>(Admin)|


|**Màn hình / Route**|**Mô tả**|**Loại Rendering**|**Yêu cầu Auth**|
|---|---|---|---|
|/profile|Xem và chỉnh sửa<br>thông tin cá nhân|CSR|Bắt buộc|
|/search|Trang kết quả full-<br>text search|SSR|Không|



#### **5.2. Giao diện Phần mềm – REST API** 

Backend cung cấp RESTful API theo chuẩn JSON. Toàn bộ endpoints được tiền tố /api/v1. Xem chi tiết tại Chương 8. 

|**Giao thức**|HTTP/1.1 và HTTP/2 qua HTTPS (TLS 1.2+). Nginx<br>termination SSL.|
|---|---|
|**Base URL (dev)**|http://localhost:5000/api/v1|
|**Base URL(prod)**|https://api.culinaryblog.com/api/v1|
|**Content-Type**|application/json; charset=utf-8 (request và response).<br>Multipart/form-data cho file upload endpoints.|
|**Authentication**|Bearer Token trong Authorization header: Authorization:<br>Bearer <access_token>. Refresh token: trong request body<br>(khôngdùngcookie để tránh CSRF).|
|**Response Format**|Success: { "data": {...}, "meta": { "page":1, "pageSize":10,<br>"total":100 } } Error: RFC 7807 Problem Details { "type", "title",<br>"status", "detail", "errors":{} }|
|**Versioning**|URL Path versioning: /api/v1/. Khi có breaking changes →<br>/api/v2/(v1 được duytrì tối thiểu 6 tháng).|
|**CORS Headers**|Access-Control-Allow-Origin: <configured-origins> Access-<br>Control-Allow-Methods: GET, POST, PUT, PATCH, DELETE,<br>OPTIONS Access-Control-Allow-Headers: Content-Type,<br>Authorization, X-Correlation-ID|
|**Rate Limit Headers**|X-RateLimit-Limit: 100 X-RateLimit-Remaining: 87 X-<br>RateLimit-Reset: 1700000000 (Unix timestamp) Retry-After: 30<br>(seconds, khi 429)|
|**Correlation ID**|X-Correlation-ID header: sinh tự động nếu không có trong<br>request, trả về trong response. Gán vào tất cả log entries<br>(Serilog MDC).|



#### **5.3. Giao diện Dịch vụ Bên thứ ba** 

|**Dịch vụ**|**Mục đích**|**Giao thức / SDK**|**Cấu hình / Secrets**|
|---|---|---|---|
|Google OAuth|Đăng nhập|OAuth 2.0 Authorization Code + PKCE.|GoogleClientId, GoogleClie|
|2.0|/ đăng ký|Redirect URI: /api/v1/auth/google/callback.|(User Secrets / env var). G|
||bằng tài|Scopes: openid, email, profile.|Console → OAuth 2.0 Clie|


|**Dịch vụ**|**Mục đích**|**Giao thức / SDK**|**Cấu hình / Secrets**|
|---|---|---|---|
||khoản<br>Google|||
|MinIO (S3-<br>compatible)|Lưu trữ file<br>ảnh công<br>thức|AWS SDK for .NET (AWSSDK.S3).<br>Endpoint override cho MinIO. Presigned<br>URL cho direct browser upload (optional).|MinIO__Endpoint, MinIO__<br>MinIO__SecretKey,<br>MinIO__BucketName. Doc<br>minio:9000.|
|Hangfire|Background<br>job<br>processing|Nuget: Hangfire.Core,<br>Hangfire.AspNetCore,<br>Hangfire.PostgreSql. In-process server.<br>Dashboard: /hangfire (Admin only, policy-<br>protected).|Dùng chung ConnectionSt<br>PostgreSQL. HANGFIRE_<br>hangfire.|
|Serilog + Seq|Structured<br>logging &<br>log<br>aggregation|Serilog.Sinks.Console (JSON),<br>Serilog.Sinks.File, Serilog.Sinks.Seq.<br>HTTP ingest API.|Seq__ServerUrl = http://se<br>(Docker). Production: Elas<br>Monitor.|
|OpenTelemetry|Distributed<br>tracing &<br>metrics|OpenTelemetry .NET SDK. OTLP exporter.<br>Tracing: HttpClient, EF Core, AspNetCore.|OTEL_EXPORTER_OTLP<br>Development: Seq OTLP.<br>Grafana Tempo / Jaeger.|
|SMTP / Email|Gửi<br>welcome<br>email (FR-<br>JOB-001)|MailKit (IEmailSender). Kết nối qua SMTP<br>với TLS.|Smtp__Host, Smtp__Port,<br>Smtp__Username, Smtp__<br>Development: Mailhog (Do|
|Google Search<br>Console|Ping<br>sitemap<br>update|HTTP GET:<br>https://www.google.com/ping?sitemap={url}|Không cần API key. Gọi tr<br>003.|



#### **5.4. Giao diện Phần cứng** 

Hệ thống là web application, không giao tiếp trực tiếp với phần cứng chuyên biệt. Yêu cầu phần cứng tối thiểu cho server: 

|**Thànhphần**|**Development(local)**|**Production(minimum)**|
|---|---|---|
|CPU|2 cores (Intel/AMD/ARM64<br>— Apple M-series được hỗ<br>trợ qua Docker)|2 vCPU (VPS/Cloud instance,<br>x86_64)|
|RAM|8 GB (chạy Docker<br>Compose đầy đủ: API + PG<br>+ Redis + MinIO + Seq)|4 GB (API + dependencies riêng lẻ)|
|Storage|20 GB SSD (cho Docker<br>images + database data +<br>MinIO volumes)|50 GB SSD (production data<br>growth)|
|Network|Kết nối internet (npm/nuget<br>packages, Google OAuth)|Bandwidth ≥ 1 Gbps, IP tĩnh|


|**Thànhphần**|**Development(local)**|**Production(minimum)**|
|---|---|---|
|Browser Client|Chrome 112+, Firefox<br>113+, Safari 16+, Edge<br>112+ (ES2020+)|Tương tự — không hỗ trợ IE11|


## **6. Kiến trúc Hệ thống** 

Chương này mô tả tổng quan kiến trúc phần mềm của hệ thống Culinary Blog. Hệ thống được thiết kế theo mô hình Client-Server với hai tầng riêng biệt: Frontend (Next.js) và Backend (.NET 10 Minimal API), giao tiếp qua REST API. Backend tuân thủ nguyên tắc Clean Architecture kết hợp CQRS pattern. 

#### **6.1. Tổng quan Kiến trúc** 

|**Tầng**|**Technology**|**Vai trò**|**Giao tiếp với**|
|---|---|---|---|
|Client<br>(Browser/Mobile)|Browser<br>(Chrome/Firefox/Safari)|Người dùng tương tác<br>quagiao diện web|Next.js App|
|Frontend|Next.js 15 App<br>Router, TypeScript,<br>Tailwind CSS, TanStack<br>Query, React Hook Form<br>+ Zod|Rendering UI, route<br>management, client-side<br>state. SSR/ISR cho SEO.|Backend<br>REST API|
|Nginx Reverse<br>Proxy|Nginx Alpine (Docker)|SSL termination, load<br>balancing, static file<br>caching, rate limiting<br>basic.|Frontend<br>:3000,<br>Backend API<br>:5000|
|Backend API|ASP.NET Core .NET<br>10 Minimal API|Business logic,<br>authentication, data<br>access, background jobs.|PostgreSQL,<br>Redis, MinIO,<br>Email|
|Cache Layer|Redis 7|Distributed cache cho<br>recipe/category/search<br>results. Rate limiting<br>counters.|Backend API|
|Object Storage|MinIO (S3-compatible)|Lưu file ảnh: original,<br>medium (800×600),<br>thumbnail (300×300).|Backend API<br>(via<br>AWSSDK.S3)|
|Database|PostgreSQL 16|Persistent relational data<br>storage. Full-text search<br>via tsvector.|Backend API<br>(via EF Core)|
|Observability|Serilog + Seq,<br>OpenTelemetry +<br>Grafana/Jaeger|Logging, metrics,<br>distributed tracing.|Backend API|



#### **6.2. Kiến trúc Backend – Clean Architecture** 

Backend tuân thủ Clean Architecture (Robert C. Martin) với nguyên tắc Dependency Rule: dependency chỉ đi vào trong (hướng Domain). Không bao giờ có reference từ Domain/Application ra Infrastructure. 
|**Domain Layer**<br>**(CulinaryBlog.Domain)**|Nhân lõi hệ thống. Chứa: • Entities: Recipe, Category,<br>ApplicationUser, RecipeStep, RecipeIngredient,<br>RecipeImage. • Value Objects: Slug, EmailAddress. •<br>Owned Entities: RecipeNutrition. • Domain Events<br>(optional): RecipePublishedEvent. • Enums:<br>RecipeDifficulty, RecipeStatus. • Interfaces:<br>IRepository<T>, IRecipeRepository,<br>ICategoryRepository. • Không có NuGet dependencies<br>(chỉ .NET BCL).|
|---|---|
|**Application Layer**<br>**(CulinaryBlog.Application)**|Orchestration Layer. Chứa: • Commands (CQRS write):<br>CreateRecipeCommand, PublishRecipeCommand,<br>LoginCommand... • Queries (CQRS read):<br>GetRecipesQuery, GetRecipeBySlugQuery... •<br>Handlers (MediatR IRequestHandler): xử lý logic<br>business cho mỗi command/query. • DTOs / Response<br>models: RecipeDto, UserDto, PagedResult<T>. •<br>Validators (FluentValidation): validation rules cho mỗi<br>command. • Pipeline Behaviors: ValidationBehavior,<br>LoggingBehavior, CachingBehavior,<br>PerformanceBehavior. • Service interfaces:<br>IEmailService, IJwtService, IFileStorageService,<br>ICurrentUser.|
|**Infrastructure Layer**<br>**(CulinaryBlog.Infrastructure)**|Implements application interfaces. Chứa: • EF Core:<br>CulinaryBlogDbContext, configurations, migrations,<br>repositories. • Repository implementations:<br>RecipeRepository (LINQ + EF Core + FTS),<br>CategoryRepository. • JWT Service: JwtService<br>(System.IdentityModel.Tokens.Jwt). • File Storage:<br>MinioFileStorageService (AWSSDK.S3). • Email:<br>MailKitEmailService. • Cache: RedisCacheService<br>(StackExchange.Redis). • Hangfire job registrations. •<br>EF Core Interceptors: AuditInterceptor (auto set<br>CreatedAt/UpdatedAt).|
|**Presentation Layer**<br>**(CulinaryBlog.API)**|HTTP interface. Chứa: • Minimal API Endpoint Groups:<br>AuthEndpoints, RecipesEndpoints,<br>CategoriesEndpoints. • Middleware:<br>GlobalExceptionMiddleware, CorrelationIdMiddleware,<br>RateLimitingMiddleware. • DI Configuration: Program.cs<br>+ Extension methods (AddApplication,<br>AddInfrastructure, AddPresentation). • OpenAPI: Scalar<br>UI tại /scalar, XML documentation comments. •<br>Authentication: JWT Bearer + Google OAuth via Auth.js<br>v5 (frontend) hoặc ASP.NET Google provider.|



#### **6.3. CQRS + MediatR Pipeline** 

CQRS (Command Query Responsibility Segregation) tách biệt read và write models. Mỗi request đi qua MediatR Pipeline Behaviors theo thứ tự: 
|**Thứ**<br>**tự**|**Pipeline Behavior**|**Trách nhiệm**|**Áp dụng cho**|
|---|---|---|---|
|1|LoggingBehavior|Log request type,<br>parameters, elapsed<br>time. Cảnh báo nếu ><br>500ms.|Tất cả Commands và<br>Queries|
|2|ValidationBehavior|Chạy FluentValidation<br>validators đã đăng ký.<br>Throw<br>ValidationException nếu<br>có lỗi.|Tất cả Commands và<br>Queries có Validator|
|3|CachingBehavior|Kiểm tra Redis cache<br>trước khi xử lý.<br>Implements ICacheable<br>interface trên Query.|Queries implements<br>ICacheable (GET<br>endpoints)|
|4|Handler<br>(IRequestHandler)|Thực thi business logic:<br>gọi repositories, raise<br>domain events, tạo<br>response DTO.|Tất cả (bắt buộc)|
|5|CacheInvalidationBehavior|Xóa cache liên quan sau<br>khi Command thành<br>công. Implements<br>ICacheInvalidator.|Commands thay đổi<br>data<br>(Create/Update/Delete)|



#### **6.4. Mô hình Quan hệ Thực thể (ERD tóm tắt)** 

Hệ thống sử dụng PostgreSQL 16 với EF Core Code First. Các aggregate entity kế thừa BaseEntity (Id, CreatedAt, UpdatedAt, IsDeleted); entity cần chống ghi đè đồng thời ánh xạ PostgreSQL system column `xmin` làm concurrency token.

|**Thực thể**|**Quan hệ**|**Bảng PostgreSQL**|
|---|---|---|
|Recipe|Nhiều RecipeStep (1:N) Nhiều<br>RecipeIngredient (1:N) Nhiều<br>RecipeImage (1:N) Một<br>RecipeNutrition (1:1 Owned) Một<br>Category (N:1) Một<br>Author/ApplicationUser (N:1)|"Recipes" "RecipeSteps"<br>"RecipeIngredients"<br>"RecipeImages" (bảng riêng)<br>"Categories" "AspNetUsers"|
|ApplicationUser|Nhiều Recipe (Author, 1:N) Nhiều<br>RefreshToken (1:N)|"AspNetUsers" (Identity)<br>"RefreshTokens"|
|Category|Nhiều Recipe(1:N)|"Categories"|
|RefreshToken|Một ApplicationUser (N:1)|"RefreshTokens"|



#### **6.5. Triển khai – Docker Compose** 

Toàn bộ hệ thống được containerized với Docker Compose. Development dùng dockercompose.yml, Production dùng docker-compose.prod.yml với optimized build + secrets management. 
|**Service**|**Image**|**Port**<br>**(host:container)**|**Volume / Dependency**|
|---|---|---|---|
|nginx|nginx:alpine|80:80, 443:443|Depends: api, frontend<br>Volume: ./nginx/nginx.conf,<br>./ssl/|
|api|culinaryblog-api<br>(Dockerfile)|5000:8080|Depends: postgres, redis,<br>minio Env file: .env.production|
|frontend|culinaryblog-web<br>(Dockerfile)|3000:3000|Depends: api|
|postgres|postgres:16-alpine|5432:5432|Volume:<br>pgdata:/var/lib/postgresql/data<br>Env: POSTGRES_DB, USER,<br>PASSWORD|
|redis|redis:7-alpine|6379:6379|Volume: redisdata:/data<br>Command: redis-server --<br>appendonly yes|
|minio|minio/minio:latest|9000:9000,<br>9001:9001<br>(Console)|Volume: miniodata:/data<br>Command: server /data --<br>console-address :9001|
|seq|datalust/seq:latest|5341:80|Volume: seqdata:/data Dev<br>only — không deploy<br>production|
|mailhog|mailhog/mailhog|8025:8025 (UI),<br>1025:1025<br>(SMTP)|Dev only — test email|


## **7. Mô hình Dữ liệu** 

Chương này đặc tả cấu trúc dữ liệu đầy đủ của hệ thống Culinary Blog. Tất cả entities kế thừa BaseEntity và sử dụng Soft Delete pattern (IsDeleted flag). Database: PostgreSQL 16 với EF Core 10 Code First. 

#### **7.1. BaseEntity (Abstract)** 

Tất cả thực thể kế thừa từ BaseEntity. Không tạo bảng riêng (Table-Per-Hierarchy không được dùng ở đây — mỗi entity có bảng riêng với các cột kế thừa). 

|**Column**|**Kiểu dữ**<br>**liệu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|Id|uuid (Guid)|PRIMARY KEY,<br>DEFAULT<br>gen_random_uuid()|Khóa chính UUID v4 — tránh<br>sequential ID guessing.|
|CreatedAt|timestamptz|NOT NULL,<br>DEFAULT NOW()|Thời điểm tạo bản ghi. Set bởi<br>AuditInterceptor (EF Core).|
|UpdatedAt|timestamptz|NULL|Thời điểm cập nhật cuối. Set<br>bởi AuditInterceptor khi<br>SaveChanges.|
|IsDeleted|boolean|NOT NULL,<br>DEFAULT false|Soft delete flag. Global Query<br>Filter: .Where(x =><br>!x.IsDeleted).|
|Version (`xmin`)|xid|System column,<br>Concurrency Token|Optimistic concurrency control do PostgreSQL tự cập nhật; ánh xạ bằng Npgsql `UseXminAsConcurrencyToken`.|



#### **7.2. Recipe** 

Thực thể trung tâm của hệ thống. Một Recipe thuộc một Category và một Author. Chứa Owned Entity RecipeNutrition và các Collection Navigation Properties. 

|**Column**|**Kiểu dữ**<br>**liệu**|**Ràng buộc**|**Index**|**Mô tả**|
|---|---|---|---|---|
|Id|uuid|PK(kế thừa)|PK|(BaseEntity)|
|Title|varchar(200)|NOT NULL|IDX_Recipe_Title (GIN<br>trigram — optional)|Tiêu đề công<br>thức. Unique<br>không bắt buộc<br>(có thể trùng<br>title khác nhau<br>slug).|
|Slug|varchar(220)|NOT NULL,<br>UNIQUE|IDX_Recipe_Slug<br>(UNIQUE B-tree)|URL-friendly<br>identifier. Sinh|


|**Column**|**Kiểu dữ**<br>**liệu**|**Ràng buộc**|**Index**|**Mô tả**|
|---|---|---|---|---|
|||||từ Title + chuẩn<br>hóa (lowercase,<br>replace space<br>→ -). Không<br>thay đổi sau<br>Publish.|
|Description|text|NOT NULL|—|Mô tả ngắn (≤<br>2000 ký tự).<br>Hiển thị trong<br>card preview và<br>SEO meta<br>description.|
|Instructions|text|NOT NULL|—|Hướng dẫn<br>tổng quan dạng<br>markdown<br>(legacy field).<br>Chi tiết dùng<br>RecipeSteps.|
|PrepTime|integer|NOT NULL,<br>CHECK > 0|—|Thời gian<br>chuẩn bị (phút).|
|CookTime|integer|NOT NULL,<br>CHECK >= 0|—|Thời gian nấu<br>(phút). 0 cho<br>"No cook"<br>recipes.|
|Servings|integer|NOT NULL,<br>CHECK > 0|—|Số khẩu phần<br>(portions).|
|Difficulty|smallint<br>(enum)|NOT NULL,<br>DEFAULT 1|IDX_Recipe_Difficulty|RecipeDifficulty:<br>1=Easy,<br>2=Medium,<br>3=Hard,<br>4=Expert.|
|Status|smallint<br>(enum)|NOT NULL,<br>DEFAULT 0|IDX_Recipe_Status|RecipeStatus:<br>0=Draft,<br>1=Published,<br>2=Archived.|
|CategoryId|uuid|NOT NULL, FK<br>→<br>Categories.Id|IDX_Recipe_CategoryId<br>(B-tree)|Khóa ngoại đến<br>Category. ON<br>DELETE<br>RESTRICT<br>(không xóa<br>category có<br>recipe).|
|AuthorId|varchar(450)|NOT NULL, FK<br>→<br>AspNetUsers.Id|IDX_Recipe_AuthorId<br>(B-tree)|Khóa ngoại đến<br>ApplicationUser<br>(Author).|


|**Column**|**Kiểu dữ**<br>**liệu**|**Ràng buộc**|**Index**|**Mô tả**|
|---|---|---|---|---|
|SearchVector|tsvector|NULL|IDX_Recipe_Search<br>(GIN)|Full-text search<br>vector. Được<br>cập nhật bởi<br>PostgreSQL<br>TRIGGER khi<br>Title/Description<br>thay đổi. Dùng<br>unaccent<br>extension cho<br>tiếng Việt.|
|PublishedAt|timestamptz|NULL|IDX_Recipe_PublishedAt|Thời điểm<br>publish. Set khi<br>Status chuyển<br>sang Published.<br>NULL nếu chưa<br>publish.|
|CreatedAt|timestamptz|NOT NULL|—|(BaseEntity)|
|UpdatedAt|timestamptz|NULL|—|(BaseEntity)|
|IsDeleted|boolean|NOT NULL|IDX_Recipe_IsDeleted<br>(partial)|(BaseEntity) —<br>Global Query<br>Filter.|
|Version (`xmin`)|xid|System column|—|Optimistic concurrency token do PostgreSQL tự cập nhật.|



##### **7.2.1. RecipeNutrition (Owned Entity — cột trong bảng Recipes)** 

Owned Entity — không có bảng riêng. Các cột được nhúng trực tiếp vào bảng Recipes với tiền tố "Nutrition_". 

|**Column trong DB**|**Property C#**|**Kiểu**|**Mô tả**|
|---|---|---|---|
|Nutrition_Calories|Calories|decimal(8,2)?|Năng lượng (kcal /<br>serving). Nullable.|
|Nutrition_Protein|Protein|decimal(8,2)?|Đạm (gram / serving).<br>Nullable.|
|Nutrition_Carbohydrates|Carbohydrates|decimal(8,2)?|Tinh bột (gram / serving).<br>Nullable.|
|Nutrition_Fat|Fat|decimal(8,2)?|Chất béo (gram / serving).<br>Nullable.|
|Nutrition_Fiber|Fiber|decimal(8,2)?|Chất xơ (gram / serving).<br>Nullable.|
|Nutrition_Sodium|Sodium|decimal(8,2)?|Natri (mg / serving).<br>Nullable.|


#### **7.3. RecipeStep** 

Các bước thực hiện chi tiết của một Recipe, được sắp xếp theo StepNumber. 

|**Column**|**Kiểu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|Id|uuid|PK (BaseEntity)|UUID khóa chính.|
|RecipeId|uuid|NOT NULL, FK →<br>Recipes.Id, ON<br>DELETE CASCADE|Khóa ngoại. Cascade delete:<br>xóa Recipe → xóa tất cả<br>Steps.|
|StepNumber|integer|NOT NULL, CHECK ><br>0|Thứ tự bước (1, 2, 3...).<br>UNIQUE cùng RecipeId<br>(composite unique).|
|Title|varchar(200)|NOT NULL|Tên bước ngắn gọn (ví dụ: "Sơ<br>chế nguyên liệu").|
|Description|text|NOT NULL|Mô tả chi tiết bước thực hiện.|
|TimerMinutes|integer|NULL, CHECK >= 0|Thời gian cần cho bước này<br>(phút). NULL nếu không áp<br>dụng.|
|ImageUrl|varchar(500)|NULL|URL ảnh minh họa bước (trên<br>MinIO). Nullable.|



#### **7.4. RecipeIngredient** 

|**Column**|**Kiểu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|Id|uuid|PK(BaseEntity)|UUID khóa chính.|
|RecipeId|uuid|NOT NULL, FK →<br>Recipes.Id, ON<br>DELETE CASCADE|Khóa ngoại với cascade<br>delete.|
|Name|varchar(200)|NOT NULL|Tên nguyên liệu (ví dụ: "Thịt<br>bò thăn").|
|Quantity|decimal(10,3)|NULL|Số lượng (ví dụ: 500). Nullable<br>cho "nguyên liệu vừa đủ".|
|Unit|varchar(50)|NULL|Đơn vị đo lường (gram, ml,<br>thìa canh,quả...). Nullable.|
|Notes|varchar(500)|NULL|Ghi chú tùy chọn (ví dụ: "thái<br>lát mỏng"). Nullable.|
|OrderIndex|integer|NOT NULL,<br>DEFAULT 0|Thứ tự hiển thị trong danh<br>sách nguyên liệu.|


#### **7.5. RecipeImage** 

|**Column**|**Kiểu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|Id|uuid|PK (BaseEntity)|UUID khóa chính.|
|RecipeId|uuid|NOT NULL, FK →<br>Recipes.Id, ON<br>DELETE CASCADE|Khóa ngoại với cascade delete.|
|OriginalUrl|varchar(500)|NOT NULL|URL ảnh gốc trên MinIO (ví dụ:<br>.../recipes/{recipeId}/{guid}.jpg).|
|MediumUrl|varchar(500)|NULL|URL ảnh medium 800×600<br>(sinh bởi FR-JOB-002).<br>Nullable khijob chưa chạy.|
|ThumbnailUrl|varchar(500)|NULL|URL ảnh thumbnail 300×300<br>(sinh bởi FR-JOB-002).<br>Nullable.|
|AltText|varchar(200)|NULL|Alt text cho accessibility.<br>Nullable.|
|IsPrimary|boolean|NOT NULL,<br>DEFAULT false|Ảnh chính (hiển thị đầu tiên).<br>Chỉ có 1 ảnh IsPrimary=true /<br>Recipe.|
|OrderIndex|integer|NOT NULL,<br>DEFAULT 0|Thứ tự hiển thị gallery.|



#### **7.6. Category** 

|**Column**|**Kiểu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|Id|uuid|PK(BaseEntity)|UUID khóa chính.|
|Name|varchar(100)|NOT NULL, UNIQUE|Tên danh mục (ví dụ: "Món<br>khai vị").|
|Slug|varchar(120)|NOT NULL, UNIQUE,<br>IDX_Category_Slug|URL-friendly name. Sinh từ<br>Name.|
|Description|text|NULL|Mô tả danh mục. Nullable.|
|ImageUrl|varchar(500)|NULL|URL ảnh đại diện category.<br>Nullable.|
|OrderIndex|integer|NOT NULL,<br>DEFAULT 0|Thứ tự hiển thị trên navigation.|



**7.7. ApplicationUser (extends IdentityUser)** Kế thừa từ ASP.NET Core Identity IdentityUser<string>. Bảng: "AspNetUsers". Thêm các custom columns: 
|**Column**<br>**(custom)**|**Kiểu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|DisplayName|varchar(100)|NOT NULL|Tên hiển thị công khai<br>(không phải username).|
|AvatarUrl|varchar(500)|NULL|URL ảnh avatar. Nullable.<br>Sinh từ Google Avatar khi<br>đăng ký OAuth.|
|Bio|text|NULL|Tiểu sử ngắn của tác giả.<br>Nullable. Hiển thị trên author<br>profile.|
|IsActive|boolean|NOT NULL,<br>DEFAULT true|Trạng thái tài khoản. Admin<br>có thể deactivate user (ban).|
|CreatedAt|timestamptz|NOT NULL,<br>DEFAULT NOW()|Ngày tạo tài khoản.|



**Identity columns (kế thừa):** Id (varchar 450), UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount. 

#### **7.8. RefreshToken** 

|**Column**|**Kiểu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|Id|uuid|PK|UUID khóa chính.|
|UserId|varchar(450)|NOT NULL, FK →<br>AspNetUsers.Id, ON<br>DELETE CASCADE|Chủ sở hữu token.|
|TokenHash|varchar(64)|NOT NULL, UNIQUE,<br>IDX_RefreshToken_Hash|SHA-256 hash<br>của raw token.<br>Không lưu raw<br>token.|
|ExpiresAt|timestamptz|NOT NULL|Thời hạn token (7<br>ngày kể từ<br>CreatedAt).|
|RevokedAt|timestamptz|NULL|Thời điểm revoke.<br>NULL = còn hiệu<br>lực.|
|ReplacedByTokenHash|varchar(64)|NULL|Hash của token<br>mới (khi rotation).<br>Để trace token<br>family.|
|CreatedAt|timestamptz|NOT NULL, DEFAULT<br>NOW()|Thời điểm tạo.|


|**Column**|**Kiểu**|**Ràng buộc**|**Mô tả**|
|---|---|---|---|
|CreatedByIp|varchar(45)|NULL|IP address tạo<br>token. Lưu để<br>audit.|


## **8. Đặc tả REST API** 

Chương này liệt kê tất cả API endpoints của hệ thống Culinary Blog. Base URL: /api/v1. Tài liệu chi tiết (request/response schemas) được sinh tự động qua Scalar UI tại /scalar. 

|**Convention**|HTTP Method + Path (prefixed /api/v1) auth required = Bearer<br>JWT Access Token bắt buộc role = Role tối thiểu cần thiết<br>(Author⊂Admin)|
|---|---|
|**Pagination**|Query params:<br>?page=1&pageSize=10&sortBy=createdAt&sortOrder=desc<br>Response wrapper: { "data":[], "meta":{ "page", "pageSize",<br>"total", "totalPages" } }|
|**Error Format**|RFC 7807 Problem Details: { "type":"about:blank", "title":"...",<br>"status":400, "detail":"...", "errors":{"field":["msg"]} }|



#### **8.1. Authentication Module (/auth)** 

|**Method**|**Endpoint**|**Mô tả**|**Auth**|**Request**<br>**Body /**<br>**Params**|**Response**|
|---|---|---|---|---|---|
|POST|/auth/register|Đăng ký tài<br>khoản mới|Không|{ email,<br>password,<br>displayName }|201: { userId,<br>email,<br>displayName<br>} 400:<br>validation<br>errors 409:<br>email đã tồn<br>tại|
|POST|/auth/login|Đăng nhập<br>email/password|Không|{ email,<br>password }|200: {<br>accessToken,<br>refreshToken,<br>expiresIn }<br>401: sai<br>credentials<br>429: quá giới<br>hạn rate limit|
|POST|/auth/google|Đăng nhập<br>Google OAuth|Không|{ idToken } —<br>ID Token từ<br>Google Sign-<br>In JS SDK|200: {<br>accessToken,<br>refreshToken,<br>expiresIn }<br>400: invalid<br>token|
|POST|/auth/refresh|Làm mới<br>Access Token|Không (dùng<br>refreshToken)|{ refreshToken<br>}|200: {<br>accessToken,<br>refreshToken,|


|**Method**|**Endpoint**|**Mô tả**|**Auth**|**Request**<br>**Body /**<br>**Params**|**Response**|
|---|---|---|---|---|---|
||||||expiresIn }<br>401: token<br>hết hạn / bị<br>revoke|
|POST|/auth/logout|Đăng xuất,<br>revoke Refresh<br>Token|Bearer JWT|{ refreshToken<br>}|204: No<br>Content 401:<br>Unauthorized|
|GET|/auth/me|Lấy thông tin<br>user hiện tại|Bearer JWT|—|200: { id,<br>email,<br>displayName,<br>avatarUrl,<br>bio, roles }<br>401:<br>Unauthorized|
|PATCH|/auth/me|Cập nhật<br>profile người<br>dùng|Bearer JWT|{<br>displayName?,<br>avatarUrl?,<br>bio? }|200: { id,<br>email,<br>displayName,<br>avatarUrl, bio<br>} 400:<br>validation<br>401:<br>Unauthorized|



#### **8.2. Categories Module (/categories)** 

|**Method**|**Endpoint**|**Mô tả**|**Auth /**<br>**Role**|**Request**|**Response**|
|---|---|---|---|---|---|
|GET|/categories|Lấy danh<br>sách tất<br>cả<br>categories|Không|—|200: [{ id,<br>name, slug,<br>description,<br>imageUrl,<br>recipeCount<br>}]|
|GET|/categories/{slug}|Lấy chi<br>tiết<br>category<br>+ danh<br>sách<br>recipes|Không|?page=1&pageSize=10&sortBy=...|200: {<br>category,<br>recipes:<br>PagedResult<br>} 404:<br>Category not<br>found|
|POST|/categories|Tạo<br>category<br>mới|Bearer<br>+<br>Admin|{ name, description?, imageUrl? }|201: { id,<br>name, slug,<br>description }<br>400:|


|**Method**|**Endpoint**|**Mô tả**<br>**Auth**<br>**Role**|**/**<br>**Request**||**Response**|
|---|---|---|---|---|---|
||||||validation<br>403:<br>Forbidden<br>409: name<br>đã tồn tại|
|PUT|/categories/{id}|Cập nhật<br>category<br>Beare<br>+<br>Admin|r<br> <br>{ name, descr<br>orderIndex? }|iption?, imageUrl?,<br>|200:<br>category<br>updated<br>400/403/404|
|DELETE|/categories/{id}|Xóa<br>category<br>(soft<br>delete)<br>Beare<br>+<br>Admin|r<br> <br>—||204: No<br>Content 403:<br>Forbidden<br>404: Not<br>found 409:<br>Có recipes<br>thuộc<br>category này|
|**.3. Rec**<br>**Method**|**ipes Module (/re**<br>**Endpoint**|**cipes)**<br>**Mô tả**<br>|**Auth / Role**|**Request**||
|GET|/recipes|Danh sách<br>recipes<br>(Published,<br>paginated)<br>|Không|?page&pageSize&so|rtBy&sortOrder&|
|GET|/recipes/{slug}|Chi tiết<br>recipe theo<br>slug (kèm<br>steps,<br>ingredients,<br>images,<br>nutrition)<br> <br>|Không (Draft:<br>Author/Admin)|—||
|GET|/recipes/search|Full-text<br>search<br>côngthức<br>|Không|?q={keyword}&page|&pageSize&cate|
|POST|/recipes|Tạo recipe<br>mới (trạng<br>thái Draft)<br> <br>|Bearer<br>(Author/Admin)|{ title, description, ca<br>nutrition? }|tegoryId, prepTim|
|PUT|/recipes/{id}|Cập nhật<br>thông tin<br>cơ bản<br>recipe<br> <br>|Bearer<br>(Owner/Admin)|{ title?, description?,<br>instructions?, nutritio|categoryId?, pre<br>n? }|
|PATCH|/recipes/{id}/publish|<br>Publish<br>recipe<br> <br>|Bearer<br>(Owner/Admin)|—||



#### **8.3. Recipes Module (/recipes)** 
|**Method**|**Endpoint**|**Mô tả**|**Auth / Role**|**Request**|
|---|---|---|---|---|
|||(Draft →<br>Published)|||
|PATCH|/recipes/{id}/unpublish|Unpublish<br>recipe<br>(Published<br>→ Draft)|Bearer<br>(Owner/Admin)|—|
|PATCH|/recipes/{id}/archive|Archive<br>recipe|Bearer<br>(Owner/Admin)|—|
|DELETE|/recipes/{id}|Xóa mềm recipe<br>(IsDeleted=true)|Bearer<br>(Owner/Admin)|—|



#### **8.4. Recipe Images (/recipes/{id}/images)** 

|**Method**|**Endpoint**|**Mô tả**|**Auth**|**Request**|**Response**|
|---|---|---|---|---|---|
|POST|/recipes/{id}/images|Upload ảnh<br>mới cho<br>recipe|Bearer<br>(Owner/Admin)|multipart/form-<br>data: file<br>(image),<br>altText?,<br>isPrimary?|201: {<br>imageId,<br>originalUrl,<br>altText,<br>isPrimary }<br>400:<br>MIME<br>invalid /<br>size ><br>5MB<br>403/404|
|PATCH|/recipes/{id}/images/{imageId}|Cập nhật<br>metadata<br>ảnh<br>(altText,<br>isPrimary,<br>orderIndex)|Bearer<br>(Owner/Admin)|{ altText?,<br>isPrimary?,<br>orderIndex? }|200:<br>image<br>updated<br>403/404|
|DELETE|/recipes/{id}/images/{imageId}|Xóa ảnh<br>(MinIO file<br>deleted<br>async via<br>Hangfire)|Bearer<br>(Owner/Admin)|—|204: No<br>Content<br>403/404|



#### **8.5. Recipe Steps (/recipes/{id}/steps)** 
|**Method**|**Endpoint**|**Mô tả**|**Auth**|**Request**|**Response**|
|---|---|---|---|---|---|
|POST|/recipes/{id}/steps|Thêm<br>bước<br>mới<br>vào<br>recipe|Bearer<br>(Owner/Admin)|{ stepNumber,<br>title,<br>description,<br>timerMinutes?,<br>imageUrl?}|201:<br>RecipeStepDto<br>400/403/404|
|PUT|/recipes/{id}/steps/{stepId}|Cập<br>nhật<br>một<br>bước|Bearer<br>(Owner/Admin)|{<br>stepNumber?,<br>title?,<br>description?,<br>timerMinutes?,<br>imageUrl?}|200:<br>RecipeStepDto<br>400/403/404|
|DELETE|/recipes/{id}/steps/{stepId}|Xóa<br>một<br>bước|Bearer<br>(Owner/Admin)|—|204: No<br>Content<br>403/404|



#### **8.6. Recipe Ingredients (/recipes/{id}/ingredients)** 

|**Method**|**Endpoint**|**Mô tả**|**Auth**|**Request**|**Response**|
|---|---|---|---|---|---|
|POST|/recipes/{id}/ingredients|Thêm<br>nguyên<br>liệu|Bearer<br>(Owner/Admin)|{ name,<br>quantity?,<br>unit?,<br>notes?,<br>orderIndex?<br>}|201:<br>RecipeIngredientD<br>400/403/404|
|PUT|/recipes/{id}/ingredients/{ingId}|Cập<br>nhật<br>nguyên<br>liệu|Bearer<br>(Owner/Admin)|{ name?,<br>quantity?,<br>unit?,<br>notes?,<br>orderIndex?<br>}|200:<br>RecipeIngredientD<br>400/403/404|
|DELETE|/recipes/{id}/ingredients/{ingId}|Xóa<br>nguyên<br>liệu|Bearer<br>(Owner/Admin)|—|204: No Content<br>403/404|



#### **8.7. Health Check Endpoints** 

|**Method**|**Endpoint**|**Mô tả**|**Auth**|**Response**|
|---|---|---|---|---|
|GET|/health|Tổng hợp health<br>tất cả<br>dependencies<br>(DB, Redis,<br>MinIO)|Không|200: Healthy | 503: Unhealthy {<br>"status":"Healthy",<br>"entries":{"database":{"status":"Healthy"},...}<br>}|


|**Method**|**Endpoint**|**Mô tả**|**Auth**|**Response**|
|---|---|---|---|---|
|GET|/health/live|Liveness probe<br>— chỉ kiểm tra<br>process còn<br>sống|Không|200: Healthy (luôn luôn, trừ khi process<br>crashed)|
|GET|/health/ready|Readiness probe<br>— kiểm tra DB<br>và Redis sẵn<br>sàng|Không|200: Healthy (DB + Redis up) 503:<br>Unhealthy (không nhận traffic)|


## **Phụ lục A – HTTP Status Codes** 

Bảng dưới đây liệt kê tất cả HTTP Status Codes được sử dụng trong API Culinary Blog, cùng ngữ cảnh sử dụng cụ thể. 

|**Code**|**Status**|**Ngữ cảnh sử dụng**|
|---|---|---|
|200|OK|GET request thành công; PATCH trả về resource đã cập nhật;<br>POST /auth/login thành công.|
|201|Created|POST tạo resource mới thành công (Recipe, Category, Step,<br>Ingredient, Image). Response body chứa resource vừa tạo.|
|204|No Content|DELETE thành công; POST /auth/logout thành công. Không<br>có response body.|
|400|Bad Request|Validation lỗi (FluentValidation), request body malformed hoặc file MIME không hợp lệ.|
|401|Unauthorized|Access Token thiếu hoặc invalid; Refresh Token hết hạn / bị<br>revoke.|
|403|Forbidden|Đã xác thực nhưng không có quyền: Author truy cập endpoint<br>Admin; Author cố xóa recipe của người khác.|
|404|Not Found|Resource khôngtồn tại hoặc đã soft-delete(IsDeleted=true).|
|409|Conflict|Trùng lặp unique field (email đã đăng ký, category slug đã tồn<br>tại); Xóa category đang có recipes.|
|422|Unprocessable<br>Entity|Dữ liệu hợp lệ về cú pháp nhưng không thể xử lý về ngữ<br>nghĩa (ví dụ: `Version`/`xmin` conflict — Optimistic Concurrency).|
|429|Too Many<br>Requests|Rate limit bị vượt. Response kèm header Retry-After (giây).|
|500|Internal Server<br>Error|Lỗi không xử lý được (unhandled exception). Trả RFC 7807,<br>log đầy đủ qua Serilog. Không lộ stack trace.|
|503|Service<br>Unavailable|Health check failed (DB/Redis down); hoặc server overloaded.|



## **Phụ lục B – Application Error Codes** 

Hệ thống sử dụng Application Error Codes (mã lỗi tùy chỉnh) trong trường RFC 7807 "type" để frontend có thể xử lý lỗi theo programmatic way mà không phụ thuộc vào chuỗi message (có thể thay đổi theo locale). 
|**Error Code**|**HTTP**<br>**Status**|**Mô tả**|**Module**|
|---|---|---|---|
|AUTH_EMAIL_EXISTS|409|Email đã được đăng<br>ký bởi tài khoản khác.|Auth|
|AUTH_INVALID_CREDENTIALS|401|Email hoặc mật khẩu<br>khôngđúng.|Auth|
|AUTH_TOKEN_EXPIRED|401|Access Token đã hết<br>hạn (15 phút).|Auth|
|AUTH_TOKEN_INVALID|401|Access Token sai định<br>dạng hoặc chữ ký<br>khônghợplệ.|Auth|
|AUTH_REFRESH_TOKEN_EXPIRED|401|Refresh Token đã hết<br>hạn (7 ngày).|Auth|
|AUTH_REFRESH_TOKEN_REVOKED|401|Refresh Token đã bị<br>thu hồi (reuse<br>detection).|Auth|
|AUTH_GOOGLE_TOKEN_INVALID|400|Google ID Token<br>không hợp lệ hoặc đã<br>hết hạn.|Auth|
|AUTH_ACCOUNT_DISABLED|403|Tài khoản bị vô hiệu<br>hóa (IsActive=false)<br>bởi Admin.|Auth|
|RECIPE_NOT_FOUND|404|Recipe với id/slug<br>không tồn tại hoặc đã<br>bị xóa.|Recipe|
|RECIPE_SLUG_EXISTS|409|Slug đã tồn tại — tự<br>động thêm suffix<br>(slug-1, slug-2...).|Recipe|
|RECIPE_PUBLISH_INCOMPLETE|422|Recipe thiếu điều kiện<br>publish: phải có ít nhất<br>1 step VÀ 1 ingredient (đồng nhất với FR-RCP-005).|Recipe|
|RECIPE_FORBIDDEN|403|User không phải<br>owner và không phải<br>Admin.|Recipe|
|RECIPE_CONCURRENCY_CONFLICT|422|`Version` (`xmin`) không<br>khớp — resource đã<br>được cập nhật bởi<br>request khác. Client<br>cần reload.|Recipe|
|CATEGORY_NOT_FOUND|404|Category không tồn<br>tại.|Category|
|CATEGORY_NAME_EXISTS|409|Tên category đã tồn<br>tại.|Category|


|**Error Code**|**HTTP**<br>**Status**|**Mô tả**|**Module**|
|---|---|---|---|
|CATEGORY_DELETE_HAS_RECIPES|409|Không thể xóa<br>category đang có<br>recipes thuộc về.|Category|
|FILE_SIZE_EXCEEDED|400|File upload vượt quá<br>giới hạn 5MB.|File|
|FILE_MIME_INVALID|400|Loại file không được<br>phép. Chỉ chấp nhận<br>JPEG, PNG, WebP,<br>AVIF.|File|
|VALIDATION_ERROR|400|Một hoặc nhiều field<br>không hợp lệ. Xem<br>"errors" object.|Common|
|RATE_LIMIT_EXCEEDED|429|Quá giới hạn request.<br>Xem Retry-After<br>header.|Common|



## **Phụ lục C – Từ điển Thuật ngữ** 

|**Thuật ngữ**|**Viết**<br>**tắt**|**Định nghĩa**|
|---|---|---|
|Access Token|AT|JSON Web Token (JWT) dùng để xác thực API<br>request. TTL = 15 phút. Ký bằng HS256.|
|Application Error Code|AEC|Mã lỗi tùy chỉnh dạng SCREAMING_SNAKE_CASE<br>trongtrường"type" của RFC 7807 Problem Details.|
|Archive|—|Trạng thái Recipe khi bị ẩn khỏi public listing nhưng<br>không bị xóa. RecipeStatus.Archived.|
|Author|—|Role người dùng mặc định sau khi đăng ký. Có thể<br>tạo/quản lýrecipe của mình.|
|Background Job|—|Tác vụ xử lý bất đồng bộ chạy ngoài HTTP request<br>cycle, quản lý bởi Hangfire.|
|Clean Architecture|CA|Kiến trúc phần mềm của Robert C. Martin tách biệt<br>concerns theo layers (Domain, Application,<br>Infrastructure, Presentation). Dependency chỉ đi vào<br>trong (hướngDomain).|
|Command Query<br>Responsibility<br>Segregation|CQRS|Pattern tách biệt write model (Commands) và read<br>model (Queries) để tối ưu từng luồng riêng.|
|Content Delivery<br>Network|CDN|Mạng phân phối nội dung tĩnh (ảnh, JS, CSS) từ<br>server gần người dùng nhất.|


|**Thuật ngữ**|**Viết**<br>**tắt**|**Định nghĩa**|
|---|---|---|
|Core Web Vitals|CWV|Chỉ số đo lường UX của Google: LCP (tải trang), CLS<br>(ổn định layout), INP (phản hồi tương tác).|
|CQRS|-|Xem Command QueryResponsibilitySegregation|
|Docker Compose|—|Công cụ định nghĩa và chạy multi-container Docker<br>application qua file YAML.|
|Draft|—|Trạng thái mặc định của Recipe khi mới tạo. Chỉ<br>Author/Admin thấy.|
|Full-Text Search|FTS|Tìm kiếm ngôn ngữ tự nhiên trong PostgreSQL qua<br>tsvector/tsquery + unaccent extension.|
|Hangfire|—|Thư viện .NET xử lý background jobs: fire-and-forget,<br>delayed, recurring.|
|HTTP Status Code|—|Mã phản hồi HTTP chuẩn (RFC 7231) cho biết kết<br>quả xử lý request (2xx: thành công, 4xx: client error,<br>5xx: server error).|
|Incremental Static<br>Regeneration|ISR|Tính năng Next.js tái sinh (regenerate) trang tĩnh theo<br>chu kỳ (revalidate interval)thayvì build lại toàn bộ.|
|JSON Web Token|JWT|Chuẩn mở (RFC 7519) định nghĩa cách truyền thông<br>tin an toàn giữa các bên dưới dạng JSON object<br>được ký.|
|MediatR|—|Thư viện .NET triển khai Mediator pattern. Dispatch<br>Commands/Queries qua Handler có pipeline<br>behaviors.|
|MinIO|—|Object storage server mã nguồn mở tương thích<br>Amazon S3 API. Dùng để lưu trữ ảnh.|
|Non-Functional<br>Requirement|NFR|Yêu cầu chất lượng hệ thống: hiệu năng, bảo mật, độ<br>tin cậy, khả năngbảo trì...|
|Nginx|—|Web server hiệu năng cao, dùng làm reverse proxy,<br>load balancer và SSL termination.|
|OpenTelemetry|OTEL|Framework quan sát hệ thống phân tán: distributed<br>tracing, metrics, logs.|
|Optimistic Concurrency|—|Kỹ thuật xử lý concurrent writes bằng `Version` ánh xạ tới PostgreSQL `xmin` — không lock DB, phát hiện conflict khi save.|
|Published|—|Trạng thái Recipe khi được công bố công khai.<br>RecipeStatus.Published.|
|Rate Limiting|—|Giới hạn số lượng request từ một IP trong khoảng<br>thời gian nhất định để ngăn brute force/DDoS.|
|Refresh Token|RT|Token dài hạn (7 ngày) dùng để lấy Access Token<br>mới mà khôngcần đăngnhậplại.|
|Refresh Token<br>Rotation|—|Mỗi lần dùng Refresh Token để refresh → token cũ bị<br>revoke, cấp token mới (bảo mật cao hơn).|


|**Thuật ngữ**|**Viết**<br>**tắt**|**Định nghĩa**|
|---|---|---|
|Reuse Detection|—|Cơ chế phát hiện khi Refresh Token đã bị revoke<br>được dùng lại → revoke toàn bộ token family của<br>user.|
|Slug|—|Chuỗi URL-friendly, dạng chữ-thường-gạch-nối, duy<br>nhất,dùngđể định danh Recipe/Categorytrên URL.|
|Soft Delete|—|Đánh dấu IsDeleted=true thay vì xóa vật lý khỏi<br>database. Dữ liệu có thể khôi phục.|
|Software<br>Requirements<br>Specification|SRS|Tài liệu đặc tả yêu cầu phần mềm theo IEEE 830 /<br>ISO/IEC/IEEE 29148.|
|TanStack Query|—|Thư viện React quản lý server state: caching,<br>background refetch,optimistic updates.|
|tsvector / tsquery|—|Kiểu dữ liệu PostgreSQL cho full-text search. tsvector<br>là chỉ mục đã xử lý, tsquery là biểu thức tìm kiếm.|
|Unit of Work|UoW|Pattern đảm bảo nhiều operations được thực hiện<br>trong một transaction duy nhất.|


