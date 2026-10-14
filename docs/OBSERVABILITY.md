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

### 5. Bằng chứng kiểm thử

- `CategoryCacheTests` (Application.Tests): lần đọc thứ hai không gọi repository (hit); Update/Delete làm lần đọc kế tiếp truy vấn lại (miss) và trả dữ liệu mới; Create bị trùng tên (409) không xóa cache; metric `result=hit|miss` được phát.
- `ObservabilityMetricsTests`: counters created/updated/deleted và metric của MediatR.
- `SensitiveLoggingTests`: không rò rỉ token, cookie, mật khẩu vào log.

