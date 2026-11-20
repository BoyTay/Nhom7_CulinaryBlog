# Nạp nội dung mẫu

1. Chép file JSON vào thư mục này, ví dụ `seed/content.json` (xem `content.sample.json`).
2. Thêm vào `.env`: `SEED_CONTENT_FILE=/seed/content.json`
3. Chạy `docker compose up -d` (chỉ cần restart `api`).

- Chạy lại nhiều lần được: danh mục và công thức trùng `slug` sẽ bị bỏ qua, không ghi đè.
- Công thức được nhập ở trạng thái Published, tác giả là tài khoản Admin (hoặc tài khoản tạo sớm nhất).
- Cần có ít nhất một tài khoản trong hệ thống.
- Khi có file nội dung, các danh mục giữ chỗ của môi trường Development ("Món chính", "Món khai vị",
  "Tráng miệng") không được tạo nữa.
- `imageUrl` (của công thức, danh mục và từng bước): URL đầy đủ (https://...) hoặc đường dẫn tương đối như
  `pho.jpg`, `steps/pho-1.jpg` (lấy từ `src/CulinaryBlog.Web/public/images/recipes/`, cần build lại web sau khi
  thêm ảnh). Ảnh bước là tùy chọn.

## Dữ liệu trong `content.json`

12 danh mục, 60 công thức (5 công thức mỗi danh mục). Mỗi công thức có mô tả, thời gian chuẩn bị/chế biến,
khẩu phần, độ khó, danh sách nguyên liệu kèm định lượng, các bước chế biến và một ảnh minh họa.

Nguồn: chuyên mục [Vào bếp của Điện máy XANH](https://www.dienmayxanh.com/vao-bep). Đường dẫn bài gốc của
từng công thức nằm ở trường `sourceUrl` (bộ nạp bỏ qua trường này). Ảnh minh họa lấy từ bài gốc (ảnh thành phẩm
800px) và lưu tại `src/CulinaryBlog.Web/public/images/recipes/<slug>.jpg`. 268/270 bước có ảnh riêng, lấy từ ảnh
của đúng bước đó trong bài gốc, lưu tại `src/CulinaryBlog.Web/public/images/recipes/steps/<slug>-<số bước>.jpg`.
