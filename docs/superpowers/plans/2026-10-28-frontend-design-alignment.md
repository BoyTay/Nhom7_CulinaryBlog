# Đưa giao diện frontend về đúng thiết kế (Design Alignment) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Làm cho toàn bộ giao diện web khớp 12 artboard trong `docs/design/project/*.dc.html` (desktop 1440px và mobile 390px): một bảng màu, một bộ font, một bộ thành phần dùng chung, đủ các khối nội dung mà thiết kế yêu cầu.

**Architecture:** Nguyên nhân gốc của cảm giác "sơ sài, rời rạc" là `app/globals.css` vẫn giữ bảng màu tím và font Atkinson của bản scaffold; token đúng thiết kế (`design-tokens.css`) chỉ có hiệu lực khi một trang tự thêm class `ds-scope`. Kế hoạch đảo lại: token thiết kế thành mặc định toàn cục, font nạp bằng `next/font`, rồi dựng một lớp thành phần dùng chung (nút, chip, huy hiệu, thẻ công thức) và làm từng trang theo artboard. Backend chỉ bổ sung vài trường mà thiết kế cần hiển thị (tên danh mục, tên tác giả, ảnh thu nhỏ, ngày cập nhật).

**Tech Stack:** Next.js 15 App Router, React 19, TypeScript, CSS thuần với token, `next/font/google`, `node --test`, Playwright (đã có), .NET 10 / xUnit cho phần DTO.

**Spec:** `docs/design/DESIGN_HANDOFF.md` và 12 artboard `docs/design/project/*.dc.html` (`canvas.json` cho kích thước); `docs/SRS_Culinary_Blog_v1.0.0.md` mục 5.1, NFR-USE-001..004; kế hoạch trước `docs/superpowers/plans/2026-10-28-ux-cohesion.md` (đã hoàn thành: route, dashboard, toast).

## Global Constraints

- Màu (đúng giá trị handoff): bg `#F6F5F0`, surface `#FFFFFF`, ink `#1C2118`, muted `#5B6356`, line `#E2E0D6`, line-strong `#B9B6A8`, primary `#2D5D43`, primary-tint `#E4EEE7`, primary-dark-text `#1F4630`, danger `#A8321C`, danger-tint `#FBE9E5`, warn-tint `#F3E6BF`, success-tint `#D6EADB`. Không còn màu tím `#7C3AED`/`#FAF5FF`/`#35136D` ở bất kỳ file CSS nào.
- Font: Bricolage Grotesque 600/700 cho tiêu đề, Be Vietnam Pro 400/500/600 cho nội dung, nạp bằng `next/font/google` (subset `vietnamese` và `latin`), không dùng `@import` Google Fonts.
- Bo góc: thẻ 16–20px, input và nút ô vuông 12px, chip và nút pill 999px. Container tối đa 1200px, padding ngang 24px (16px trên mobile).
- Touch target ≥ 44px, tương phản chữ ≥ 4.5:1, focus ring 2px `#2D5D43`, thẻ ngữ nghĩa (`header`, `nav`, `main`, `article`, `aside`, `footer`), `aria-current="page"`, `aria-pressed` cho chip lọc.
- Breakpoint: mobile 320–767 (1 cột), tablet 768–1199, desktop ≥ 1200. Lưới thẻ `repeat(auto-fit, minmax(min(260px, 100%), 1fr))`, gap 24px.
- Chữ hiển thị bằng tiếng Việt; không đưa tên món, số liệu hay ngày trong artboard vào code (chỉ là dữ liệu mẫu).
- Không thêm thư viện mới (npm 11 đã làm hỏng `npm ci` trong Docker một lần, xem ledger kế hoạch trước). Không dùng Tailwind (chưa cài).
- Frontend: `npm run typecheck`, `npm run lint`, `npm test` sạch; backend: `dotnet build CulinaryBlog.sln -c Release --warnaserror` 0 warning; E2E hiện có (6 luồng) vẫn đạt.
- Test viết trước và phải thấy đỏ. Mỗi task một commit theo Conventional Commits, không có dòng attribution Claude trong commit hoặc PR (quy tắc của người dùng).
- Quyền sở hữu: Task 2 chạm DTO của module Recipe (Trường); mở commit riêng và nhờ Trường xem. File cá nhân (`docs/BAO_CAO_LAB_*`, `docs/W0*_HIEU_*`, `tmp/`) giữ nguyên chưa theo dõi.

## Review Focus

- Mọi trang (kể cả trang chủ, footer, 404) cùng một nền `#F6F5F0`; không còn vùng nền tím lộ ra hai bên nội dung.
- Recipe chưa có ảnh: thẻ hiện ô giữ chỗ đúng kiểu thiết kế, không vỡ bố cục; có ảnh: dùng ảnh thu nhỏ, không méo (`object-fit: cover`) và có `alt`.
- Tiêu đề rất dài (200 ký tự) và tên danh mục rất dài không làm tràn thẻ hoặc header; có xuống dòng hoặc cắt bằng dấu chấm lửng.
- Danh sách rỗng, danh mục rỗng, tìm không ra kết quả, API lỗi: mỗi trang có trạng thái riêng theo thiết kế (khung nét đứt `#CFCCBF`), không trang trắng.
- Trang `/categories` khi có hàng trăm danh mục (DB dev hiện có 169 danh mục do test tích hợp để lại) vẫn dùng được: lưới đúng cột, không tràn chữ.
- Mobile 390px: header có nút tìm kiếm và menu; menu mở bằng bàn phím (Enter, Escape đóng) và trả focus; không có cuộn ngang trên toàn trang.
- Dashboard: người dùng chưa đăng nhập đi tới `/login?returnUrl=…`; tab trạng thái giữ đúng khi tải lại; Admin thấy huy hiệu "Admin" và mục Danh mục, Author thì không.

---

## File Structure

- `src/CulinaryBlog.Web/app/globals.css` (viết lại gọn: reset + token toàn cục), `app/design-tokens.css` (xóa sau khi gộp), `app/layout.tsx` (next/font).
- `components/ui/ui.css` (mới: `.btn`, `.chip`, `.badge`, `.card`, `.field`, `.empty-state`), `components/ui/*.tsx` nhỏ khi cần.
- `components/shared/site-header.tsx`, `site-header.css`, `mobile-menu.tsx`, `site-footer.tsx` (viết lại, bỏ style inline).
- `components/recipes/recipe-card.tsx` (thẻ thống nhất), `components/recipes/related-recipes.tsx`, `components/recipes/recipe-detail*.tsx`.
- `components/dashboard/*` (shell, tab trạng thái, bảng, thẻ người dùng).
- `lib/ui/format.ts` (nhãn độ khó, thời gian, ngày), `lib/recipes/list-view.ts` (lọc, sắp xếp, phân trang phía server component).
- Backend: `RecipeListItemDto`, `RecipeDetailDto`, `RecipeReadRepository`, `CategoryRecipeSummary`.
- `e2e/visual.spec.ts` (mới): chụp ảnh đối chiếu thiết kế.

---

### Task 1: Token và font toàn cục, bỏ bảng màu tím

**Files:**
- Modify: `app/globals.css`, `app/layout.tsx`, `components/shared/shared.css`
- Delete: `app/design-tokens.css` (gộp vào `globals.css`)
- Test: `tests/ui/design-tokens.test.ts`

**Interfaces:**
- Produces: biến CSS toàn cục trên `:root` (`--color-*` cũ vẫn tồn tại làm bí danh để các file CSS cũ không vỡ): `--ds-bg`, `--ds-surface`, `--ds-ink`, `--ds-muted`, `--ds-line`, `--ds-line-strong`, `--ds-primary`, `--ds-primary-tint`, `--ds-primary-dark-text`, `--ds-danger`, `--ds-danger-tint`, `--ds-warn-tint`, `--ds-success-tint`, `--font-heading`, `--font-body`.

- [ ] **Step 1: Viết test đỏ `design-tokens.test.ts`:** đọc mọi file `.css` dưới `app/` và `components/`; (a) không file nào chứa `#7c3aed`, `#faf5ff`, `#35136d` hoặc `Atkinson` (không phân biệt hoa thường); (b) `globals.css` khai báo `:root` với đúng 13 giá trị màu ở Global Constraints; (c) không file nào còn `@import url(` tới `fonts.googleapis.com`.
- [ ] **Step 2: Chạy `npm test -- tests/ui`**, mong đợi FAIL ở cả ba khẳng định.
- [ ] **Step 3: Viết lại `globals.css`:** `:root` chứa token thiết kế (tách khối `--ds-*` và khối bí danh `--color-primary: var(--ds-primary)` …), `body` dùng `var(--font-body)` và nền `--ds-bg`, tiêu đề `h1..h3` dùng `var(--font-heading)`. Chuyển các rule `.site-header`, `.hero`, `.features`, `.feature-card`, `.brand` cũ ra khỏi file (trang chủ sẽ viết lại ở Task 5). Gộp phần còn dùng của `design-tokens.css` rồi xóa file và import của nó.
- [ ] **Step 4: Nạp font trong `app/layout.tsx`** bằng `Bricolage_Grotesque` (weight 600, 700) và `Be_Vietnam_Pro` (400, 500, 600) từ `next/font/google`, `subsets: ["latin", "vietnamese"]`, `variable: "--font-heading"` và `"--font-body"`, `display: "swap"`; gắn hai class biến vào `<html>`.
- [ ] **Step 5: Quét class `ds-scope`:** giữ nguyên trong JSX (không còn tác dụng, an toàn) nhưng xóa mọi khai báo CSS `.ds-scope { --… }` còn sót.
- [ ] **Step 6: Chạy `npm test`, `npm run typecheck`, `npm run lint`**, mong đợi sạch; mở `/` và `/recipes` bằng Chrome, nền đều `#F6F5F0`.
- [ ] **Step 7: Commit** `refactor(web): make the design tokens and fonts the global default`.

### Task 2: Backend bổ sung trường cho thẻ và trang chi tiết

> Chạm module Recipe (Trường): commit riêng, nhờ Trường xem.

**Files:**
- Modify: `RecipeListItemDto.cs`, `RecipeDetailDto.cs`, `RecipeReadRepository.cs`, `CategoryRecipeSummary` (trong `ICategoryRepository.cs`) và repository tương ứng; `lib/recipes/contracts.ts`, `lib/categories/category-view.ts`
- Test: `tests/CulinaryBlog.Integration.Tests/RecipeReadRepositoryTests.cs`, `RecipeEndpointTests.cs`

**Interfaces:**
- Produces (JSON, camelCase): `RecipeListItemDto` thêm `categoryName: string`, `categorySlug: string`, `authorName: string`, `thumbnailUrl: string | null`, `updatedAt: string | null`; `RecipeDetailDto` thêm `categoryName`, `categorySlug`, `authorName`, `publishedAt` (đã có); `CategoryRecipeSummary` thêm `thumbnailUrl`, `cookTimeMinutes`, `difficulty`.
- `thumbnailUrl` lấy từ ảnh chính (`IsPrimary`), ưu tiên `ThumbnailUrl`, rồi `MediumUrl`, rồi `Url`; `null` khi chưa có ảnh. `authorName` là `DisplayName` của người dùng, `"Ẩn danh"` khi không tìm thấy.

- [ ] **Step 1: Test tích hợp đỏ:** `RecipeReadRepositoryTests` tạo recipe thật (qua `RecipeIntegrationTestData`) với danh mục tên "Món nước", người dùng có `DisplayName` "Phan Hiếu" và một ảnh chính có `ThumbnailUrl`; khẳng định `ListAsync` và `GetDetailBySlugAsync` trả `categoryName == "Món nước"`, `categorySlug`, `authorName == "Phan Hiếu"`, `thumbnailUrl` đúng URL; recipe không ảnh trả `thumbnailUrl == null`.
- [ ] **Step 2: Chạy `dotnet test tests/CulinaryBlog.Integration.Tests --filter RecipeReadRepository`**, mong đợi FAIL (biên dịch hoặc giá trị).
- [ ] **Step 3: Mở rộng DTO và `RecipeReadRepository`** bằng join `Categories` và `Users` trong cùng truy vấn (không N+1); cập nhật bản fake `TestRecipeRepository` trong `RecipeEndpointTests` và các lời gọi tạo DTO trong test.
- [ ] **Step 4: Cập nhật `contracts.ts`** và mọi fixture frontend dùng `RecipeListItemDto` (`recipe-list.test.ts`, `home-data.test.ts`).
- [ ] **Step 5: Chạy `dotnet build --warnaserror`, toàn bộ test backend (dừng container api trước), `npm test`**, mong đợi sạch.
- [ ] **Step 6: Commit** `feat(api): include category, author and thumbnail in recipe read models`.

### Task 3: Bộ thành phần giao diện dùng chung

**Files:**
- Create: `components/ui/ui.css`, `components/ui/status-badge.tsx` (thay `recipe-status-badge.tsx`), `lib/ui/format.ts`
- Modify: `components/shared/index.ts`, nơi đang dùng `.button`, `.button-primary`
- Test: `tests/ui/format.test.ts`

**Interfaces:**
- Produces trong `lib/ui/format.ts`: `difficultyLabel(d: RecipeDifficulty): "Dễ" | "Trung bình" | "Khó"`, `cookTimeLabel(minutes: number): string` (`"90 phút"`, `"2 giờ"` khi chia hết 60 và ≥ 120, `"1 giờ 30 phút"` các trường hợp còn lại từ 60 trở lên), `formatDateVi(iso: string): string` (`"14/10/2026"`, múi giờ `Asia/Ho_Chi_Minh`), `initials(name: string): string` (hai chữ cái đầu của từ đầu và từ cuối, hoa).
- Lớp CSS: `.btn` (pill, `min-height: 44px`), `.btn-primary`, `.btn-ghost`, `.btn-outline`, `.btn-danger`; `.chip` và `.chip[aria-pressed="true"]`; `.badge-draft|published|archived`; `.card` (viền line, bo 16px, hover đổ bóng `0 8px 24px rgba(28,33,24,.10)`); `.empty-state` (viền nét đứt `#CFCCBF`, nền trắng); `.field` (nhãn 14px đậm, input cao 48px, bo 12px, focus 2px primary).

- [ ] **Step 1: Test đỏ `format.test.ts`:** `cookTimeLabel(30) === "30 phút"`, `(90) === "1 giờ 30 phút"`, `(120) === "2 giờ"`, `(0) === "0 phút"`; `difficultyLabel("Medium") === "Trung bình"`; `formatDateVi("2026-10-14T03:00:00Z") === "14/10/2026"` và `"2026-12-31T20:00:00Z" === "01/01/2027"` (qua múi giờ Việt Nam); `initials("Phan Trung Hiếu") === "PH"`, `initials("  ") === "?"`, `initials("an") === "A"`.
- [ ] **Step 2: Chạy**, mong đợi FAIL (module chưa có).
- [ ] **Step 3: Cài `format.ts`** (di chuyển `difficultyLabel` từ `lib/recipes/list.ts`, để `list.ts` import lại) và viết `ui.css` theo artboard; thay `.button-*` cũ ở các nơi dùng bằng lớp mới.
- [ ] **Step 4: Chạy `npm test`, typecheck, lint**, mong đợi sạch.
- [ ] **Step 5: Commit** `feat(web): add shared button, chip, badge and card styles with formatters`.

### Task 4: Header, menu mobile và footer theo thiết kế

**Files:**
- Modify: `components/shared/site-header.tsx`, `components/shared/app-chrome.tsx`, `components/shared/shared.css`
- Create: `components/shared/site-header.css`, `components/shared/mobile-menu.tsx`, `lib/navigation/search-href.ts`
- Test: `tests/navigation/search-href.test.ts`, `tests/navigation/nav-active.test.ts`, mở rộng `e2e/journey.spec.ts`

**Interfaces:**
- Produces: `buildSearchHref(query: string): string` (cắt khoảng trắng; rỗng hoặc dưới 2 ký tự trả `/search`; còn lại `/search?q=<encodeURIComponent>`); `isActiveNav(pathname: string, href: string): boolean` (`/recipes/pho-bo` đang ở mục `/recipes`; `/` chỉ khớp chính xác; `/dashboard/recipes` khớp `/dashboard`).

- [ ] **Step 1: Test đỏ:** `buildSearchHref("pho bo") === "/search?q=pho%20bo"`, `("a") === "/search"`, `("  ") === "/search"`, `("phở & bò") === "/search?q=ph%E1%BB%9F%20%26%20b%C3%B2"`; `isActiveNav` đủ bốn trường hợp trên và `isActiveNav("/categories/mon-chay", "/categories") === true`, `isActiveNav("/recipes", "/") === false`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Viết lại `SiteHeader`:** nền `#F6F5F0`, viền dưới line, brand Bricolage 22px primary, `nav aria-label="Chính"` với `aria-current="page"` và màu primary cho mục đang mở, ô tìm kiếm pill (`role="search"`, `<form action="/search">`, ô `name="q"` có `<label>` ẩn) chỉ hiện từ 768px; khi chưa đăng nhập: `Đăng nhập` (ghost) và `Đăng ký` (primary pill); khi đã đăng nhập: avatar tròn `initials` dẫn tới `/profile`. Không còn style inline.
- [ ] **Step 4: `MobileMenu` (dưới 768px):** nút tìm kiếm và nút menu 44×44 (`aria-label="Mở menu"`, `aria-expanded`, `aria-controls`); bảng điều hướng gồm các link, ô tìm kiếm và nút đăng nhập/đăng ký; Escape đóng và trả focus về nút mở; khóa cuộn nền khi mở.
- [ ] **Step 5: Footer:** viền trên line, một hàng `© Culinary Blog` và `Nhóm 07 · Phát triển ứng dụng web nâng cao`, full-width (hết hộp trắng lệch trong nền tím).
- [ ] **Step 6: E2E:** thêm test mobile (viewport 390×844) mở menu bằng bàn phím, kiểm tra Escape đóng và focus quay về; test desktop gõ vào ô tìm của header rồi Enter đến `/search?q=…`.
- [ ] **Step 7: Chạy `npm test`, `npm run e2e` (nới `AUTH_RATE_LIMIT_PERMITS=1000`)**, mong đợi đạt.
- [ ] **Step 8: Commit** `feat(web): rebuild header, mobile menu and footer to match the design`.

### Task 5: Thẻ công thức thống nhất và trang chủ

**Files:**
- Modify: `components/recipes/recipe-card.tsx`, `components/recipes/recipe-image.tsx`, `lib/recipes/list.ts`, `lib/home/home-data.ts`, `app/page.tsx`, `app/home.css`
- Test: `tests/recipes/recipe-card-model.test.ts`, `tests/home/home-data.test.ts` (cập nhật)

**Interfaces:**
- Consumes: DTO mới (Task 2), `cookTimeLabel`, `difficultyLabel` (Task 3).
- Produces: `RecipeCardModel = { id; href; title; categoryName; categoryHref; cookTimeLabel; difficultyLabel; thumbnailUrl: string | null; thumbnailAlt: string }` và `toRecipeCardModel(recipe: RecipeListItemDto): RecipeCardModel`; `thumbnailAlt` là `"Ảnh món <title>"`. `HomeViewModel` thêm `hero: RecipeCardModel | null` (công thức mới nhất có ảnh, nếu không thì `null`).

- [ ] **Step 1: Test đỏ:** `toRecipeCardModel` trả đúng `categoryHref` (`/categories/<slug>`), nhãn thời gian và độ khó, `thumbnailUrl` giữ nguyên hoặc `null`; `buildHomeViewModel` đặt `hero` là recipe mới nhất có `thumbnailUrl`, và `null` khi không recipe nào có ảnh; tối đa 8 thẻ, không trùng với `hero`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: `RecipeCard`:** ảnh 160–170px (hoặc ô giữ chỗ kiểu thiết kế), nhãn danh mục chữ hoa 12px primary, tiêu đề Bricolage 18–19px tối đa hai dòng (`-webkit-line-clamp`), hàng meta có biểu tượng đồng hồ, thời gian và độ khó; cả thẻ là một link; hover đổ bóng.
- [ ] **Step 4: Trang chủ theo `Main.dc.html`:** hero hai cột (tiêu đề 64px "Món ngon từ bếp nhà, chia sẻ cho mọi người.", đoạn mô tả, hai nút "Khám phá công thức" và "Đăng bài của bạn"; cột phải là ảnh `hero` hoặc khung giữ chỗ bo 24px cao 420px kèm nhãn "Ảnh công thức nổi bật"); mục "Công thức mới nhất" kèm link "Xem tất cả"; dải nền `#E4EEE7` "Duyệt theo danh mục" với chip pill hiện tên và số lượng; khối CTA nền `#1C2118` "Có món tủ muốn chia sẻ?" nút "Tạo tài khoản Tác giả" (khi đã đăng nhập đổi thành "Viết công thức mới"). Bỏ bảng danh mục trong hero của bản hiện tại.
- [ ] **Step 5: Trạng thái:** API lỗi (khối công thức thay bằng `.empty-state` có link tới `/recipes`), chưa có công thức (`.empty-state` mời viết bài đầu tiên).
- [ ] **Step 6: Chạy `npm test`, typecheck, lint**, mở `/` ở 1440 và 390 đối chiếu `Main.dc.html` và `MobileHome.dc.html`.
- [ ] **Step 7: Commit** `feat(web): unify the recipe card and rebuild the home page`.

### Task 6: Danh sách công thức (`/recipes`)

**Files:**
- Modify: `app/recipes/(list)/page.tsx`, `components/recipes/recipe-list-filters.tsx`, `recipe-list-pagination.tsx`, `recipe-list-results.tsx`, `lib/recipes/list.ts`
- Create: `lib/recipes/list-view.ts`
- Test: `tests/recipes/list-view.test.ts`, `tests/recipes/recipe-list.test.ts` (cập nhật)

**Interfaces:**
- Produces: `parseListParams(raw: Record<string, string | string[] | undefined>): { q: string; difficulty?: RecipeDifficulty; maxCook?: 30 | 60 | 90; sort: "newest" | "name" | "cook"; categoryId?: string; page: number }` (giá trị lạ bị bỏ, `page` mặc định 1) và `applyListView(items: RecipeListItemDto[], params): { items: RecipeListItemDto[]; totalCount: number; totalPages: number; page: number }` (tìm theo tiêu đề không dấu, lọc, sắp xếp, cắt trang 12 mục, kẹp `page` vào `[1, totalPages]`).

- [ ] **Step 1: Test đỏ:** với 30 recipe mẫu — `q: "pho bo"` khớp "Phở bò" (bỏ dấu, không phân biệt hoa thường); `maxCook: 60` loại recipe nấu 90 phút; `sort: "name"` theo A–Z có dấu tiếng Việt (`localeCompare("vi")`); `sort: "cook"` tăng dần; `page: 99` kẹp về trang cuối; tham số `maxCook=45` hoặc `sort=abc` bị bỏ; kết quả rỗng trả `totalPages: 1`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Cài hai hàm** (tìm kiếm khớp theo tiêu đề và mô tả đã bỏ dấu; việc lọc, sắp xếp và phân trang chạy trong server component vì API hiện trả cả danh sách; ghi chú trong mã rằng T-5 của `CR-002_PLAN` sẽ chuyển việc này lên API).
- [ ] **Step 4: Giao diện theo `Recipes.dc.html`:** tiêu đề 44px kèm `aria-live` đếm công thức; ô tìm kiếm cao 56px bo 14px ("Thử gõ “pho bo” – tìm được cả “phở bò”"); hàng lọc gồm nhóm chip "Độ khó" (`aria-pressed`), chọn "Nấu tối đa" (Bất kỳ, 30, 60, 90 phút) và chọn "Sắp xếp" (Mới nhất, Tên A–Z, Thời gian nấu); giữ chọn danh mục hiện có; mọi thay đổi cập nhật URL (`router.push`) và có thể chia sẻ; phân trang theo `Pagination` dùng chung; trạng thái rỗng "Không có công thức phù hợp / Thử bỏ bớt bộ lọc hoặc đổi từ khóa".
- [ ] **Step 5: Chạy test, typecheck, lint**; E2E luồng guest vẫn đạt.
- [ ] **Step 6: Commit** `feat(web): add search, cook-time filter, sorting and paging to the recipe list`.

### Task 7: Trang chi tiết công thức

**Files:**
- Modify: `components/recipes/recipe-detail.tsx`, `recipe-detail-gallery.tsx`, `lib/recipes/detail.ts`, `app/recipes/[slug]/recipe-detail.css`, `app/recipes/[slug]/page.tsx`
- Create: `components/recipes/related-recipes.tsx`, `lib/recipes/server-related.ts`
- Test: `tests/recipes/recipe-detail.test.ts` (mở rộng), `tests/recipes/related.test.ts`

**Interfaces:**
- Produces: `pickRelated(all: RecipeListItemDto[], current: { id: string; categoryId: string }, limit = 3): RecipeListItemDto[]` (cùng danh mục, đã xuất bản, bỏ chính nó, mới nhất trước); `RecipeDetailViewModel` thêm `categoryName`, `categoryHref`, `authorName`, `authorInitials`, `publishedLabel` (`"Đăng ngày 14/10/2026"` hoặc `null`).

- [ ] **Step 1: Test đỏ:** `pickRelated` với 5 recipe (2 khác danh mục, 1 Draft, chính nó) trả đúng 2 phần tử theo thứ tự mới nhất; `createRecipeDetailViewModel` trả breadcrumb và `authorInitials === "PH"`, `publishedLabel === "Đăng ngày 14/10/2026"`, `null` khi chưa xuất bản; bộ dinh dưỡng bốn ô `Năng lượng`, `Đạm`, `Carb`, `Béo` kèm đơn vị khi có dữ liệu, `null` khi không.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Giao diện theo `RecipeDetail.dc.html`:** breadcrumb `Công thức / <Danh mục> / <Tên>` (link tới `/categories/<slug>`); đầu bài hai cột (ảnh chính 440px bo 20px cùng dải ảnh thu nhỏ 88×64 chọn được bằng chuột và bàn phím; cột phải gồm nhãn danh mục, `h1` 52px, mô tả, dòng tác giả có avatar `initials` và ngày đăng, bốn ô số liệu Chuẩn bị/Nấu/Khẩu phần/Độ khó); thân bài hai cột (khung "Nguyên liệu" sticky rộng ≤ 400px; "Cách làm" với vòng số 40px và huy hiệu hẹn giờ; "Dinh dưỡng mỗi khẩu phần" khi có dữ liệu); mục "Cùng danh mục <Tên>" với 3 thẻ nhỏ (ảnh 120px). Giữ JSON-LD và Open Graph hiện có.
- [ ] **Step 4: Mobile theo `MobileDetail.dc.html`:** header có nút quay lại, ảnh 260px, hai cột cho ô số liệu và dinh dưỡng, nguyên liệu và các bước xếp dọc.
- [ ] **Step 5: Chạy test, typecheck, lint, E2E**; đối chiếu bằng ảnh ở 1440 và 390.
- [ ] **Step 6: Commit** `feat(web): complete the recipe detail page with author, gallery, nutrition and related recipes`.

### Task 8: Danh mục (`/categories`, `/categories/[slug]`)

**Files:**
- Modify: `app/categories/page.tsx`, `app/categories/[slug]/page.tsx`, `lib/categories/category-view.ts`, `app/recipes/recipes.css` (tách phần danh mục)
- Create: `components/categories/category-card.tsx`, `app/categories/categories.css`
- Test: `tests/categories/category-view.test.ts` (cập nhật)

**Interfaces:**
- Produces: `CategoryCard({ name; href; description; recipeCount })`; `buildCategoryDetailView` dùng `RecipeCard` chung (Task 5) thay vì thẻ chữ.

- [ ] **Step 1: Test đỏ:** trang chi tiết danh mục tạo `cards` có `cookTimeLabel`, `difficultyLabel`, `thumbnailUrl` từ `CategoryRecipeSummary` mới; danh mục không mô tả dùng câu `"<n> công thức"`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Giao diện theo `CategoryList.dc.html`:** tiêu đề 48px, mô tả "Chọn một danh mục để xem các công thức đã xuất bản.", lưới thẻ 20px (dải ảnh 72px, tên 22px, mô tả 14px cắt hai dòng, "<n> công thức" màu primary) rộng tối thiểu `min(260px,100%)`; sửa lỗi tràn chữ hiện tại của lưới danh mục (tên dài phải xuống dòng bằng `overflow-wrap: anywhere`).
- [ ] **Step 4: Trang chi tiết danh mục:** tiêu đề, mô tả, lưới `RecipeCard`, phân trang, trạng thái rỗng "Danh mục này chưa có công thức".
- [ ] **Step 5: Chạy test, typecheck, lint**; mở `/categories` với 169 danh mục của DB dev, đảm bảo lưới đều và không tràn.
- [ ] **Step 6: Commit** `feat(web): restyle the category pages with the shared card`.

### Task 9: Trang tìm kiếm (`/search`)

**Files:**
- Modify: `app/search/search-page.tsx`, `app/search/search.css`
- Test: `tests/search/search-view.test.ts` (mới, cho hàm thuần), E2E

**Interfaces:**
- Produces: `searchSummary(q: string, total: number): { title: string; subtitle: string }` (`title === "Kết quả cho “pho bo”"`, `subtitle === "3 công thức, sắp xếp theo độ liên quan."`; `total === 0` ra `"Không có kết quả. Thử từ khóa ngắn hơn."`).

- [ ] **Step 1: Test đỏ cho `searchSummary`** (ba trường hợp trên và `q` có dấu ngoặc kép được thoát).
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Giao diện theo `Search.dc.html`:** cột nội dung 900px; ô tìm viền primary cao 60px kèm nút "Tìm"; hàng kết quả (ảnh 160px, nhãn danh mục, tiêu đề 22px có `<mark>` màu `#F6E3A1`, mô tả, "<thời gian> · <độ khó>"); phân trang; trạng thái rỗng nét đứt. Bộ lọc hiện có (danh mục, độ khó, thời gian nấu, khẩu phần, sắp xếp) chuyển vào một nút "Bộ lọc" mở/đóng ở trên danh sách (`aria-expanded`), không còn cột bên trái chiếm chỗ.
- [ ] **Step 4: Chạy test, typecheck, lint, E2E.**
- [ ] **Step 5: Commit** `feat(web): align the search page with the design`.

### Task 10: Dashboard theo thiết kế

**Files:**
- Modify: `components/dashboard/dashboard-shell.tsx`, `my-recipes-table.tsx`, `dashboard.css`, `lib/navigation/routes.ts` (`showsSiteChrome`), `lib/dashboard/my-recipes.ts`
- Create: `components/dashboard/status-tabs.tsx`, `components/dashboard/user-card.tsx`
- Test: `tests/dashboard/my-recipes.test.ts` (mở rộng), `tests/navigation/nav-items.test.ts` (cập nhật)

**Interfaces:**
- Produces: `filterByStatus(items: RecipeListItemDto[], tab: "all" | "draft" | "published" | "archived"): RecipeListItemDto[]`; `parseStatusTab(raw: string | undefined): StatusTab` (mặc định `all`); `availableActions("Archived")` đổi thành `["unpublish-restore", "delete"]` hiển thị nhãn "Khôi phục" (gọi `unpublish`, đưa về Bản nháp) — `actionLabels` thêm khóa tương ứng; `showsSiteChrome("/dashboard")` trả `false` cho mọi đường dẫn `/dashboard/**`.

- [ ] **Step 1: Test đỏ:** `filterByStatus` đủ bốn tab; `parseStatusTab("xyz") === "all"`; `availableActions("Archived")` gồm "Khôi phục"; `showsSiteChrome` false cho `/dashboard`, `/dashboard/recipes`, `/dashboard/categories` và vẫn true cho `/profile`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Shell theo `Dashboard.dc.html`:** thanh bên 260px nền trắng (brand liên kết về `/`, `nav aria-label="Dashboard"` gồm Tổng quan, Công thức của tôi, Danh mục kèm huy hiệu "Admin" chỉ cho Admin, Hồ sơ; thẻ người dùng ở đáy với avatar `initials`, tên và vai trò "Tác giả"/"Quản trị viên"); khu nội dung padding 40px. Dưới 768px thanh bên thu thành thanh ngang có thể cuộn.
- [ ] **Step 4: Trang "Công thức của tôi":** tiêu đề 36px và nút "Tạo công thức mới"; hàng tab trạng thái (`role="tablist"`, đồng bộ `?status=` trên URL); bảng bốn cột (Công thức, Danh mục, Trạng thái, Cập nhật) cùng cột thao tác (Sửa, hành động theo trạng thái, Xóa có `aria-label="Xóa <tên>"`); dòng chú thích "Chỉ có thể xuất bản khi công thức có ít nhất 1 nguyên liệu và 1 bước thực hiện."; nút Xóa/Lưu trữ vẫn qua `ConfirmDialog`.
- [ ] **Step 5: Cập nhật E2E:** dashboard giờ không có header chung; kiểm tra tab "Bản nháp" lọc đúng, "Khôi phục" chuyển Lưu trữ về Bản nháp.
- [ ] **Step 6: Chạy test, typecheck, lint, E2E.**
- [ ] **Step 7: Commit** `feat(web): rebuild the dashboard with sidebar, status tabs and richer table`.

### Task 11: Đối chiếu Wizard, Hồ sơ, Đăng nhập và Quản trị danh mục

**Files:**
- Create: `e2e/visual.spec.ts`
- Modify (sau khi đối chiếu): `app/dashboard/recipes/recipe-editor.css`, `components/recipes/editor/recipe-editor.tsx`, `app/profile/profile.css`, `components/auth/profile-form.tsx`, `app/dashboard/(shell)/categories/categories.css`

- [ ] **Step 1: Viết `visual.spec.ts`:** đăng ký tác giả tạm, tạo một recipe đầy đủ rồi lưu ảnh chụp toàn trang ở 1440×900 và 390×844 cho `/`, `/recipes`, recipe chi tiết, `/categories`, `/search?q=pho`, `/login`, `/register`, `/dashboard`, `/dashboard/recipes`, `/dashboard/recipes/new`, `/profile` vào `e2e/__screens__/` (thư mục bị git bỏ qua). Test chỉ khẳng định mỗi trang có đúng một `h1`, đủ landmark `main`, và không có cuộn ngang (`document.documentElement.scrollWidth <= innerWidth`).
- [ ] **Step 2: Chạy `npm run e2e` với `AUTH_RATE_LIMIT_PERMITS=1000`**; lỗi cuộn ngang nào lộ ra được sửa ở task chứa trang đó, không vá trong test.
- [ ] **Step 3: Đối chiếu bằng mắt với artboard và ghi lệch vào commit:** Wizard (thanh đầu "← Công thức của tôi | Tạo công thức mới | Lưu ở trạng thái bản nháp", bốn thẻ bước 44px, thẻ nội dung bo 20px padding 36px, nút Quay lại/Tiếp tục), Hồ sơ (thẻ 720px, avatar 88px, huy hiệu vai trò, email chỉ đọc, nút "Lưu thay đổi" và "Đăng xuất"), Đăng nhập (giữ bố cục hai cột hiện có vì phong phú hơn thiết kế), Quản trị danh mục (bảng ba cột Tên/Slug/Số công thức, nút Xóa vô hiệu khi còn công thức kèm `title`, khối "Thêm danh mục" rộng ≤ 380px). Chỉ sửa những chỗ lệch rõ rệt về bố cục, màu và khoảng cách.
- [ ] **Step 4: Chạy test, typecheck, lint, E2E.**
- [ ] **Step 5: Commit** `style(web): align the editor, profile and category admin with the design`.

### Task 12: Rà mobile, khả năng truy cập và hoàn thiện

**Files:**
- Modify: các file CSS có breakpoint; `e2e/visual.spec.ts`
- Test: mở rộng `e2e/visual.spec.ts`

- [ ] **Step 1: Mở rộng test mobile:** ở 390px, thẻ công thức trong danh sách chuyển sang dạng ngang (ảnh 96px bên trái, như `MobileHome.dc.html`), dải chip danh mục cuộn ngang trong chính nó chứ không làm cuộn cả trang, mọi nút và link có vùng chạm ≥ 44px (kiểm tra `boundingBox` cho header, chip, nút hành động).
- [ ] **Step 2: Chạy**, mong đợi FAIL ở phần chưa chuyển.
- [ ] **Step 3: Sửa CSS** theo kết quả (card ngang dưới 768px, chip cuộn ngang, ảnh chi tiết 260px, lưới hai cột cho ô số liệu).
- [ ] **Step 4: Kiểm tra tương phản và focus** thủ công cho văn bản muted `#5B6356` trên `#F6F5F0` (≥ 4.5:1) và chip chưa chọn; sửa token nếu thiếu.
- [ ] **Step 5: Cập nhật `docs/design/DESIGN_HANDOFF.md`** (bỏ ghi chú đã làm, thêm bảng route ↔ artboard ↔ trạng thái) và `README.md` (lệnh chụp ảnh đối chiếu).
- [ ] **Step 6: Chạy toàn bộ:** `dotnet build --warnaserror`, test backend (dừng api trước), `npm run typecheck && npm run lint && npm test`, build lại Docker, `npm run e2e`, `node scripts/smoke-test.mjs`.
- [ ] **Step 7: Commit** `style(web): finish the mobile pass and sync the design handoff`.

---

## Thứ tự và phụ thuộc

`1 → 3 → 2 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12`. Task 1 phải đứng đầu vì mọi trang phụ thuộc token; Task 2 (backend) chặn Task 5, 7, 8; Task 3 (thành phần chung) chặn Task 4 đến 10. Nếu Trường chưa kịp xem Task 2, làm Task 1, 3, 4 trước.

## Self-review

- **Phủ thiết kế:** 12 artboard — Main (T5), Recipes (T6), RecipeDetail (T7), CategoryList (T8), Search (T9), Login (T11, giữ), Dashboard (T10), Wizard (T11), Categories admin (T11), Profile (T11), MobileHome (T5, T12), MobileDetail (T7, T12). Header/footer chung (T4), token và font (T1).
- **Nhất quán tên:** `RecipeCardModel`/`toRecipeCardModel` (T5) được T8 và T7 dùng; `cookTimeLabel`/`difficultyLabel` (T3) được T5, T8, T9 dùng; `StatusTab`, `filterByStatus` chỉ ở T10; DTO mới ở T2 được T5, T7, T8, T10 dùng.
- **Chưa kiểm chứng khi lập kế hoạch:** ảnh dashboard, wizard và hồ sơ hiện tại chưa chụp được (phiên bị giới hạn 10 request/phút nên bị đưa về trang đăng nhập), vì vậy Task 10 và 11 dựa vào mã và E2E đã có; Task 11 Step 1 tạo công cụ chụp để đối chiếu trước khi sửa. Kích thước thật của thẻ dashboard và wizard cần xác nhận bằng ảnh.
- **Ngoài phạm vi (cố ý):** chuyển lọc, sắp xếp, phân trang lên API (T-5 của `CR-002_PLAN`), ảnh đại diện upload, trang `/dashboard/categories` thêm tính năng mới, dọn 169 danh mục test trong DB dev (nên sửa test tích hợp để tự dọn, việc riêng), Lighthouse CI.
