# Nạp nội dung mẫu

1. Chép file JSON vào thư mục này, ví dụ `seed/content.json` (xem `content.sample.json`).
2. Thêm vào `.env`: `SEED_CONTENT_FILE=/seed/content.json`
3. Chạy `docker compose up -d` (chỉ cần restart `api`).

- Chạy lại nhiều lần được: danh mục và công thức trùng `slug` sẽ bị bỏ qua, không ghi đè.
- Công thức được nhập ở trạng thái Published, tác giả là tài khoản Admin (hoặc tài khoản tạo sớm nhất).
- Cần có ít nhất một tài khoản trong hệ thống.
- `imageUrl`: URL đầy đủ (https://...) hoặc tên file như `pho.jpg`
  (lấy từ `src/CulinaryBlog.Web/public/images/recipes/`, cần build lại web sau khi thêm ảnh).
