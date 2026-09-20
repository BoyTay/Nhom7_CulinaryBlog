# ADR-0002: Dùng PostgreSQL `xmin` cho optimistic concurrency

- Trạng thái: Accepted
- Ngày: 2026-09-20

## Quyết định

Các aggregate cần chống lost update dùng PostgreSQL system column `xmin`, ánh xạ thành thuộc tính `uint Version` và concurrency token trong EF Core/Npgsql. API nhận version hiện tại khi cập nhật và trả HTTP 422 với mã lỗi nghiệp vụ khi version không còn khớp.

## Lý do

`byte[] RowVersion`/`[Timestamp]` là mô hình quen thuộc của SQL Server nhưng PostgreSQL không tự sinh giá trị tương đương. `xmin` được PostgreSQL cập nhật tự động và Npgsql hỗ trợ trực tiếp.

