# Culinary Blog – Design Handoff (cho Claude Code)

Nguồn thiết kế: các file `project/*.dc.html` (mỗi file = 1 artboard, HTML tĩnh + inline style). Đọc để lấy layout, spacing, màu, copy. Cài đặt lại bằng **Next.js 15 App Router + TypeScript + Tailwind** theo SRS v1.0.1. Không copy cú pháp `<x-dc>`, `<sc-for>`, `{{}}`, `DCLogic` – đó chỉ là định dạng của công cụ thiết kế.

## 1. Design tokens
Màu (đưa vào `tailwind.config`):
- bg `#F6F5F0` · surface `#FFFFFF` · ink `#1C2118` · muted `#5B6356` · line `#E2E0D6` · line-strong `#B9B6A8`
- primary `#2D5D43` · primary-tint `#E4EEE7` · primary-dark-text `#1F4630`
- danger `#A8321C` · danger-tint `#FBE9E5` · warn-tint `#F3E6BF` · success-tint `#D6EADB`
- Placeholder ảnh: `repeating-linear-gradient(135deg,#EDE6D8 0 12px,#F4EFE4 12px 24px)` (thay bằng ảnh MinIO thật)

Font (Google Fonts, dùng `next/font`): **Bricolage Grotesque** 600/700 cho tiêu đề, **Be Vietnam Pro** 400/500/600 cho nội dung (hỗ trợ tiếng Việt).
Cỡ tiêu đề: H1 desktop 44–64px, mobile 34–38px; H2 24–32px; body 15–17px.
Bo góc: card 16–20px, input/button 12px, chip/pill 999px. Container tối đa 1200px, padding ngang 24px. Nút/touch target ≥ 44px.
Breakpoints theo SRS: mobile 320–767 (1 cột), tablet 768–1199 (2 cột), desktop ≥1200. Lưới card: `grid-template-columns: repeat(auto-fit,minmax(260px,1fr))`, gap 24px.

## 2. Artboard ↔ route
| File | Route | Rendering (SRS 5.1) | Auth |
|---|---|---|---|
| Main | `/` | ISR 3600 | Không |
| Recipes | `/recipes` | SSR | Không |
| RecipeDetail | `/recipes/[slug]` | ISR 300 + JSON-LD | Không |
| CategoryList | `/categories` | ISR 3600 | Không |
| (chưa vẽ) | `/categories/[slug]` | ISR 600 | Không – dùng lại lưới card của Recipes |
| Search | `/search` | SSR | Không |
| Login | `/auth/login`, `/auth/register` | CSR | Redirect nếu đã login |
| Dashboard | `/dashboard/recipes` | CSR | Author/Admin |
| Wizard | `/dashboard/recipes/new`, `/dashboard/recipes/[id]/edit` | CSR | Owner/Admin |
| Categories | `/dashboard/categories` | CSR | Admin |
| Profile | `/profile` | CSR | Bắt buộc |
| MobileHome, MobileDetail | cùng route trên ở < 768px | | |

## 3. Component nên tách
`SiteHeader` (logo, nav, ô tìm, Đăng nhập/Đăng ký hoặc avatar), `RecipeCard`, `CategoryChip`, `LevelFilterChips` (aria-pressed), `Pagination`, `FactTile`, `IngredientList`, `StepList` (số bước + badge hẹn giờ), `NutritionGrid`, `StatusBadge` (Draft/Published/Archived), `DashboardShell` (sidebar 260px, thu xuống trên mobile), `FormField` (label + input + lỗi inline), `Stepper` (wizard 4 bước), `ImageUploader` (progress bar %), `Toast`.

## 4. Hành vi & API
- **Recipes**: query `GET /recipes?page&pageSize=12&categoryId&difficulty=Easy|Medium|Hard&maxCookTime&sort=-createdAt`. Nhãn UI: Dễ/Trung bình/Khó. Hiển thị `totalCount`, phân trang offset.
- **Search**: `GET /recipes/search?q=` (≥2 ký tự, prefix + bỏ dấu). Đánh dấu từ khớp bằng `<mark>`. Rỗng → thông báo gợi ý.
- **Detail**: `GET /recipes/{slug}` → ingredients (SortOrder), steps (StepNumber, TimerMinutes), images (IsPrimary), nutrition (calories, protein, carbs, fat). Thêm `<script type="application/ld+json">` Recipe và Open Graph (NFR-SEO).
- **Auth**: register `{displayName,email,password}`; login `{email,password}`; Google dùng Google Identity Services gửi `{idToken}` tới `/auth/google`. Access token giữ trong bộ nhớ; refresh token qua cookie bảo mật; tự gọi `/auth/refresh` khi 401. Login sai → 401 thông báo chung "Email hoặc mật khẩu không đúng"; 423 khi bị khóa; 409 email trùng.
- **Dashboard**: Xuất bản = `PATCH /recipes/{id}/publish` (422 `RECIPE_PUBLISH_INCOMPLETE` nếu thiếu nguyên liệu/bước → toast lỗi); Hủy xuất bản = `/unpublish`; Lưu trữ = `/archive`; Xóa = `DELETE` (soft delete) có hộp xác nhận.
- **Wizard**: 4 bước (Thông tin → Nguyên liệu → Các bước → Ảnh & dinh dưỡng). Tạo recipe luôn ở trạng thái Draft. Sửa recipe gửi `If-Match` = version; 422 concurrency → báo "Dữ liệu đã bị thay đổi bởi người dùng khác". Upload ảnh: JPEG/PNG/WebP/AVIF, ≤5MB, ảnh đầu là ảnh chính.
- **Categories (Admin)**: nút Xóa vô hiệu khi `recipeCount > 0` (BE trả 409). Slug tự sinh, không đổi khi đổi tên.
- **Profile**: `GET/PATCH /auth/me` chỉ sửa `fullName`, `avatarUrl`; email chỉ đọc.
- Mọi lỗi API theo RFC 7807; hiển thị lỗi field ngay cạnh input (React Hook Form + zod). Loading dùng skeleton, thành công/thất bại dùng toast (NFR-USE-004).

## 5. Lưu ý
- Tên món, số công thức theo danh mục, dinh dưỡng, ngày đăng trong thiết kế là **dữ liệu mẫu**, thay bằng dữ liệu API.
- Dùng thẻ ngữ nghĩa (`header`, `nav`, `main`, `article`, `aside`), `aria-current`, `aria-pressed`, `role="alert"/"status"`, focus ring 2px `#2D5D43`; tương phản chữ ≥ 4.5:1 (WCAG AA).
- Mỗi trang cần `<title>` "{Tên} | Culinary Blog" và `noindex` cho Draft/Archived.
- Đã có (tuần 8, nhánh `feat/ux-cohesion`): `/categories`, `/categories/[slug]`, `/dashboard` và `/dashboard/recipes` (empty state, hộp xác nhận xóa/lưu trữ), toast, header/footer dùng chung, `/recipes/[slug]`.
- Đường dẫn thực tế: đăng nhập/đăng ký ở `/login`, `/register` (không có tiền tố `/auth`); quản trị danh mục ở `/dashboard/categories` (`/admin/categories` chuyển hướng 308).
- Hợp đồng API: `status` và `difficulty` được trả dưới dạng tên (`"Published"`, `"Easy"`).
