# Observability: tracing, metrics và kiểm soát log nhạy cảm

## Tín hiệu đang phát

| Loại | Nguồn | Ghi chú |
| --- | --- | --- |
| Trace | ASP.NET Core, HttpClient, EF Core | Bỏ qua `/health/live`; exception được ghi vào span. SQL không kèm giá trị tham số. |
| Metric HTTP | ASP.NET Core instrumentation | `http.server.request.duration` (request, duration, lỗi qua `http.response.status_code`). |
| Metric runtime | .NET runtime instrumentation | GC, thread pool, bộ nhớ. |
| Metric MediatR | Meter `CulinaryBlog.Application` | `culinaryblog.request.duration` (ms, tag `request`, `outcome`), `culinaryblog.request.errors` (tag `request`, `exception`). |
| Metric Category | Meter `CulinaryBlog.Categories` | `culinaryblog.category.created`, `.updated`, `.deleted`. Chỉ ghi sau khi `SaveChanges` thành công. |

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
