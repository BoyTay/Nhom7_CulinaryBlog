# Kiểm tra accessibility Admin UI (`/admin/categories`)

Phạm vi: bàn phím, focus, contrast và responsive của trang quản trị Category (issue #34, tuần 7).

## Cách kiểm tra

- Chạy trang trên stack compose (`http://localhost/admin/categories`) và điều khiển Chrome thật bằng `puppeteer-core`.
- `axe-core` 4.10 với các bộ luật `wcag2a`, `wcag2aa`, `wcag21aa`, `wcag22aa`, `best-practice`, ở bốn chiều rộng: 1280, 768, 375, 320 px.
- Đo tràn ngang (`scrollWidth` so với `innerWidth`) ở cả bốn chiều rộng.
- Duyệt Tab 16 bước và chạy luồng chỉ bằng bàn phím: tạo, sửa/hủy, mở hộp thoại xóa, Escape, xác nhận xóa. Dữ liệu tạo ra được xóa ngay trong luồng.
- Contrast tính theo công thức WCAG 2.x cho các cặp màu trong token thiết kế (`app/design-tokens.css`) và CSS của trang.

## Kết quả trước khi sửa

| Mục | Kết quả |
| --- | --- |
| axe-core (4 chiều rộng) | 0 vi phạm |
| Tràn ngang | Không |
| Contrast chữ (mọi cặp được kiểm) | Đạt (thấp nhất 5,7:1) |
| Viền ô nhập `#b9b6a8` trên nền trắng | **2,0:1, không đạt 3:1 (WCAG 1.4.11)**. Axe không kiểm tiêu chí này |
| Sau khi bấm "Sửa" | Focus ở lại nút Sửa; form nằm sau cả bảng trong thứ tự Tab |
| Sau khi tạo/cập nhật thành công | Focus rơi về `body` vì nút gửi bị vô hiệu hóa trong lúc lưu |
| Nút "Xóa" khi danh mục còn công thức | Dùng `disabled`: không focus được, lý do chỉ có trong tooltip `title` |
| 320 px | Cột "Sửa/Xóa" nằm ngoài vùng nhìn thấy, phải cuộn ngang trong bảng; vùng cuộn không focus được bằng bàn phím |
| Bảng | Không có caption |

## Đã sửa

| Thay đổi | File |
| --- | --- |
| Bấm "Sửa" chuyển focus vào ô "Tên"; "Hủy" trả focus về nút "Sửa" của hàng đó; lưu thành công đưa focus về tiêu đề danh sách (cạnh thông báo) | `category-manager.tsx` |
| Nút "Xóa" khi còn công thức dùng `aria-disabled` (vẫn focus được), có mô tả ẩn "Không thể xóa vì danh mục còn N công thức" qua `aria-describedby`; bấm không làm gì | `category-manager.tsx`, `categories.css` |
| Viền ô nhập đổi thành `#827f70` (4,0:1 trên trắng, 3,7:1 trên nền trang); mặc định của `FormField` khi không có token là `#767676` (4,5:1) | `design-tokens.css`, `components/shared/shared.css` |
| Bảng có `caption` ẩn; vùng cuộn có `role="region"`, `aria-label`, `tabIndex=0` và focus ring | `category-manager.tsx`, `categories.css` |
| Dưới 40rem: ẩn cột Slug, hiện slug nhỏ dưới tên, giảm padding để Sửa/Xóa luôn nằm trong vùng nhìn | `categories.css` |
| `.visually-hidden` dùng kiểu clip chuẩn thay cho `left: -9999px` | `categories.css` |

## Kết quả sau khi sửa (chạy lại cùng kịch bản)

- axe-core: 0 vi phạm ở 1280, 768, 375, 320 px; không tràn ngang.
- Sau "Sửa": focus ở ô Tên. Sau "Hủy": focus về nút "Sửa A11y …". Sau tạo: focus ở `h1`. Hộp thoại xóa mở với focus ở "Giữ lại" (hành động an toàn); Escape đóng và trả focus về nút "Xóa"; xác nhận xóa đưa focus về `h1`.
- Ở 320 px, mọi nút "Sửa/Xóa" nằm trong vùng nhìn.

## Giới hạn

- Chưa kiểm tra bằng trình đọc màn hình thật (NVDA/VoiceOver); chỉ kiểm tra cây accessibility qua axe và thuộc tính ARIA.
- Viền thẻ và nút phụ (`#e2e0d6`, 1,2–1,3:1) không đạt 3:1 nhưng không mang thông tin riêng: nút có nhãn chữ và bảng có tiêu đề cột. Không đổi để giữ phong cách thiết kế.
- Trường hợp danh mục còn công thức chưa được kiểm tự động trong trình duyệt vì dữ liệu mẫu hiện không có công thức; đã rà theo mã nguồn.
- Chưa có kiểm thử React tự động cho tương tác của màn này (dự án chỉ có test `node --test` cho logic `.ts`).
- Trang `/search` và các trang khác dùng `FormField` sẽ nhận viền ô nhập mới nếu dùng token mặc định; cần báo Kiên, Bo, Trường.
