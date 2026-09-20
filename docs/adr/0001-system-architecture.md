# ADR-0001: Kiến trúc hệ thống và ranh giới module

- Trạng thái: Accepted
- Ngày: 2026-09-20

## Bối cảnh

Culinary Blog cần cho phép bốn thành viên phát triển song song nhưng vẫn giữ dependency ổn định và giảm xung đột khi tích hợp.

## Quyết định

- Backend là modular monolith trên .NET 10, tổ chức theo Clean Architecture và vertical slice CQRS.
- Dependency đi theo hướng `API -> Infrastructure -> Application -> Domain`; API composition root được phép tham chiếu các project cần để đăng ký dependency injection.
- Domain chứa business model và invariant, không phụ thuộc package orchestration hoặc infrastructure.
- Application chứa use cases, ports, validation và behavior.
- Infrastructure hiện thực persistence và external services.
- API chỉ phụ trách HTTP transport, authentication middleware và composition.
- Frontend là ứng dụng Next.js 15 riêng, giao tiếp qua REST `/api/v1` và dùng contract ở biên HTTP, không tham chiếu domain backend.
- Module có schema và endpoint riêng; giao tiếp liên-module qua application contracts hoặc domain events, không truy cập repository của module khác.

## Hệ quả

Các nhánh có thể phát triển độc lập trên cùng baseline. Kiến trúc có thêm ceremony so với một project đơn, nhưng dependency rules có thể kiểm thử tự động và thích hợp với phạm vi môn học.

