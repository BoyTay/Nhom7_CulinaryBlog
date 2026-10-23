# Observability: tracing, metrics và kiểm soát log nhạy cảm

## Tín hiệu đang phát

| Loại | Nguồn | Ghi chú |
| --- | --- | --- |
| Trace | ASP.NET Core, HttpClient, EF Core | Bỏ qua `/health/live`; exception được ghi vào span. SQL không kèm giá trị tham số. |
| Metric HTTP | ASP.NET Core instrumentation | `http.server.request.duration` (request, duration, lỗi qua `http.response.status_code`). |
| Metric runtime | .NET runtime instrumentation | GC, thread pool, bộ nhớ. |
| Metric MediatR | Meter `CulinaryBlog.Application` | `culinaryblog.request.duration` (ms, tag `request`, `outcome`), `culinaryblog.request.errors` (tag `request`, `exception`). |
| Metric Category | Meter `CulinaryBlog.Categories` | `culinaryblog.category.created`, `.updated`, `.deleted`. Chỉ ghi sau khi `SaveChanges` thành công. |
| Metric cache Category | Meter `CulinaryBlog.Categories` | `culinaryblog.category.cache.requests` (tag `result` = `hit` hoặc `miss`) cho mỗi lần đọc `GET /categories`. |

## Tương quan trace và log

- Serilog gắn `@tr` (TraceId) và `@sp` (SpanId) vào mọi log event nằm trong một request, cả ở Console lẫn Seq.
- `CorrelationIdMiddleware` trả header `X-Correlation-ID` và gắn tag `correlation.id` lên span hiện tại.
- Log hoàn tất mỗi request (Serilog) có `RequestMethod`, `RequestPath`, `StatusCode`, `Elapsed`, `CorrelationId` và, khi đã xác thực, `UserId` (chỉ id, không có e-mail hay token).
- Từ một trace trong Seq chọn "Find logs", hoặc lọc `@tr = '<trace id>'`.

## Cấu hình OTLP

```json
"OpenTelemetry": {
  "Protocol": "HttpProtobuf",
  "TracesEndpoint": "http://localhost:5341/ingest/otlp/v1/traces",
  "MetricsEndpoint": "",
  "OtlpEndpoint": ""
}
```

- `OtlpEndpoint` là endpoint chung (gRPC mặc định); `TracesEndpoint` / `MetricsEndpoint` ghi đè theo từng tín hiệu.
- Để trống endpoint thì exporter không được bật (mặc định production/test).
- Seq chỉ nhận **trace và log**, không nhận metrics qua OTLP, nên `MetricsEndpoint` mặc định để trống. Xem metrics cục bộ bằng `dotnet-counters monitor -n CulinaryBlog.API --counters CulinaryBlog.Categories,CulinaryBlog.Application`. `Protocol` áp dụng chung cho cả hai tín hiệu.

## Xem trace và log bằng Seq

1. `docker compose up -d seq` (đăng nhập `admin` với mật khẩu `SEQ_FIRSTRUN_ADMINPASSWORD` trong `.env`).
2. Chạy API ở môi trường Development; `appsettings.Development.json` đã trỏ log và trace về `http://localhost:5341`.
3. Mở http://localhost:5341, tab **Traces** để xem span (ASP.NET Core → EF Core), tab **Events** để xem log có `@tr` khớp.
4. Trong docker compose, service `api` đã được cấu hình gửi trace tới `http://seq/ingest/otlp/v1/traces`.

## Kiểm soát dữ liệu nhạy cảm

- Không bật `EnableSensitiveDataLogging` của EF Core; instrumentation EF Core không xuất giá trị tham số.
- Serilog request logging chỉ ghi method, path, status, thời gian; không ghi header hay body.
- `RequestTimingBehavior` chỉ log tên request, thời gian, loại exception; không log payload.
- Thư viện IdentityModel che PII (token) trong thông báo lỗi mặc định.
- Test `SensitiveLoggingTests` gửi bearer token, cookie, mật khẩu trong body và xác nhận không chuỗi nào xuất hiện trong log event (kể cả log lỗi kết nối DB không chứa mật khẩu DB).
- Quy ước cho code mới: không đưa token, cookie, mật khẩu, connection string hoặc payload vào message/property của log; log theo ID hoặc mã lỗi.

## Hướng dẫn truy vấn vận hành (runbook)

Mọi lệnh dưới đây chạy cục bộ; không cần dashboard riêng. Metrics xem bằng `dotnet-counters`, trace và log xem bằng Seq.

### 1. Xem nhanh sức khỏe API

```powershell
dotnet-counters monitor -n CulinaryBlog.API --counters Microsoft.AspNetCore.Hosting,CulinaryBlog.Application,CulinaryBlog.Categories
```

| Câu hỏi | Nhìn vào | Dấu hiệu bất thường |
| --- | --- | --- |
| API có chậm không? | `http.server.request.duration`, `culinaryblog.request.duration` (tag `request`) | Request cụ thể tăng đột biến so với các request khác |
| Có lỗi không? | `culinaryblog.request.errors` (tag `request`, `exception`), `http.response.status_code` 5xx | Số lỗi tăng liên tục, cùng một `exception` |
| Cache Category có hiệu quả không? | `culinaryblog.category.cache.requests` theo `result` | Gần như toàn `miss` dù không có thay đổi Category (cache bị xóa liên tục hoặc hết hạn quá sớm) |
| Admin có đang sửa Category không? | `culinaryblog.category.created/updated/deleted` | Mỗi lần tăng sẽ kèm một `miss` ở lần đọc kế tiếp (đúng thiết kế: ghi xong thì xóa cache) |

### 2. Hành vi cache Category (tham chiếu để đọc metric)

- Key `categories:all`, sliding expiration 60 phút, lưu trong `IMemoryCache` của từng instance.
- Lần đọc đầu hoặc sau khi cache bị xóa là `miss`; các lần sau là `hit`.
- `Create`, `Update`, `Delete` xóa key sau khi `SaveChanges` thành công. Lệnh lỗi (ví dụ trùng tên, 409) không xóa cache.
- Chạy nhiều instance API thì mỗi instance có cache riêng; sau thay đổi Category, instance khác có thể trả dữ liệu cũ tối đa 60 phút (giới hạn đã biết).

### 3. Điều tra một request lỗi

1. Lấy `traceId` trong body lỗi RFC 7807 (hoặc header `X-Correlation-ID`) mà client nhận được.
2. Trong Seq, tab **Events**, lọc `@tr = '<traceId>'` để xem log của request.
3. Tab **Traces**, mở trace đó để xem span ASP.NET Core và EF Core; span lỗi có exception.
4. Đối chiếu mã lỗi (`type` trong problem details, ví dụ `CATEGORY_NAME_EXISTS`) với log `Request failed with {ErrorCode}`.

### 4. Truy vấn Seq hữu ích

| Mục đích | Truy vấn |
| --- | --- |
| Mọi lỗi server | `@Level = 'Error'` |
| Lỗi theo mã | `ErrorCode = 'CATEGORY_DELETE_HAS_RECIPES'` |
| Request chậm | `Elapsed > 500` (log request của Serilog) |
| Log của một trace | `@tr = '<traceId>'` |

### 5. Health endpoints và hành vi khi dịch vụ phụ thuộc lỗi

| Endpoint | Kiểm tra | Mã trả về |
| --- | --- | --- |
| `GET /health/live` | Chỉ process còn sống (`self`) | Luôn 200 khi API chạy, kể cả khi PostgreSQL/Redis/MinIO đều tắt |
| `GET /health/ready` | `database`, `redis`, `minio` (và `hangfire` khi bật) | 200 nếu tất cả khỏe, 503 nếu có thành phần lỗi |
| `GET /health` | Tất cả check | Như `/health/ready` cộng `self` |

Body là JSON theo SRS 8.7: `status`, `totalDurationMs` và `entries`, một object khóa theo tên check (`database`, `redis`, `minio`, `hangfire`, `self`), mỗi mục có `status`, `description`, `durationMs`. Body không có exception hay chuỗi kết nối. Khi nhận 503, đọc `entries` để biết thành phần nào lỗi:

```powershell
curl.exe -s http://localhost/health/ready
```

Kết quả diễn tập trên stack compose đầy đủ (dừng từng dịch vụ bằng `docker compose stop <service>`):

| Dịch vụ tắt | `/health/live` | `/health/ready` | Ảnh hưởng quan sát được | Tự phục hồi sau `start` |
| --- | --- | --- | --- | --- |
| Redis | 200 | 503, chỉ `redis` lỗi, mất khoảng 5 giây | `GET /api/v1/categories` vẫn 200 | Ngay lập tức |
| MinIO | 200 | 503, chỉ `minio` lỗi, mất khoảng 3 giây | `GET /api/v1/categories` vẫn 200 | Dưới 2 giây |
| PostgreSQL | 200 | 503, chỉ `database` lỗi, phản hồi tức thì | `GET /api/v1/categories` vẫn 200 nhờ cache 60 phút; `GET /api/v1/categories/{slug}`, `/recipes`, `/recipes/search` trả 500 `INTERNAL_SERVER_ERROR` kèm `traceId`; `/recipes` và `/recipes/search` mất khoảng 5 giây mới báo lỗi | Khoảng 3 giây |

Giới hạn đã biết:
- Khi PostgreSQL tắt, lỗi kết nối trả 500 chứ không phải 503; dùng `traceId` trong body để tìm log.
- Check Redis có thể mất khoảng 5 giây khi container Redis bị dừng (phân giải tên Docker chờ lâu hơn timeout 3 giây của check). Probe readiness của orchestrator nên đặt timeout từ 6 giây trở lên.
- Đọc danh mục từ cache vẫn thành công khi database tắt; ghi (Admin Create/Update/Delete) sẽ lỗi.

### 6. Sự cố môi trường local thường gặp

| Triệu chứng | Nguyên nhân | Cách xử lý |
| --- | --- | --- |
| `docker compose up` báo `failed to bind host port ... address already in use` | Cổng 5432 (PostgreSQL), 6379 (Redis) đã có ứng dụng khác dùng | Xem ai giữ cổng: `netstat -ano \| findstr :5432`. Tạo `compose.override.yaml` (file local, đã nằm trong `.git/info/exclude`) với `ports: !override` để đổi cổng máy, ví dụ `"55432:5432"`. Nếu `netstat` không thấy tiến trình nào mà Docker vẫn báo trùng, chọn cổng khác |
| `failed to connect to the docker API ... dockerDesktopLinuxEngine` | Docker Desktop chưa chạy | Mở Docker Desktop, đợi engine sẵn sàng rồi `docker compose up -d` |
| `/health/ready` 503 chỉ `redis` lỗi nhưng container Redis `healthy` | Redis không gắn vào network compose sau một lần start lỗi (API không phân giải được tên `redis`) | `docker network inspect culinary-blog_default` thấy thiếu Redis thì `docker compose up -d --force-recreate redis` |
| Sau khi pull, API trả 500 hoặc integration test lỗi hàng loạt (bảng/cột không tồn tại) | Database còn ở migration cũ | `dotnet ef database update --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.API --connection "<chuỗi kết nối>"` |
| Integration test lỗi `password authentication failed` hoặc không kết nối được | Test dùng `localhost:5432` trong `appsettings.Development.json`, trong khi database compose ở cổng khác | Đặt biến `ConnectionStrings__Postgres` trỏ tới database compose trước khi chạy `dotnet test` |
| `docker compose build web` lỗi ở bước `npm ci` | `package-lock.json` bị `npm install` cục bộ sửa lệch với `package.json` | `git checkout src/CulinaryBlog.Web/package-lock.json`, build lại. Chỉ commit lock khi chủ ý thêm/sửa dependency |
| `POST /api/v1/auth/login` trả 429 | Rate limit đăng nhập | Đợi khoảng 1 phút rồi thử lại, không lặp vòng gọi |
| Trang Admin báo "Phiên quản trị không hợp lệ" | Token Admin thiếu hoặc hết hạn (15 phút) | Đăng nhập lại để lấy access token mới rồi dán vào ô token |

Thứ tự điều tra chung: `docker compose ps` → `curl.exe -s http://localhost/health/ready` (xem `entries`) → log API (`docker compose logs api`, hoặc Seq theo `@tr` của `traceId`) → mục 3.

### 7. Bằng chứng kiểm thử

- `CategoryCacheTests` (Application.Tests): lần đọc thứ hai không gọi repository (hit); Update/Delete làm lần đọc kế tiếp truy vấn lại (miss) và trả dữ liệu mới; Create bị trùng tên (409) không xóa cache; metric `result=hit|miss` được phát.
- `ObservabilityMetricsTests`: counters created/updated/deleted và metric của MediatR.
- `SensitiveLoggingTests`: không rò rỉ token, cookie, mật khẩu vào log.
- `CategoryRepositoryTests` (PostgreSQL thật, schema riêng): quyền xem Draft theo FR-CAT-002 (Guest, tác giả, tác giả khác), phân trang, đếm công thức Published so với tất cả, soft delete và tên/slug đã xóa vẫn bị giữ chỗ.
- `CategoryEndpointErrorTests`: 422, 404, 401, 403, 409 của các endpoint Category và `type` trong problem details.
- `DegradedHealthTests`: `/health/live` vẫn 200 khi mọi phụ thuộc lỗi, `/health/ready` chỉ ra đúng thành phần lỗi, body không rò rỉ mật khẩu.
- `StructuredLoggingTests`: log request có method/path/status/elapsed, `CorrelationId` (giữ nguyên hoặc sinh mới) và `UserId` khi đã xác thực.
