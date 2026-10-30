# Kết nối hành trình người dùng (UX Cohesion) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Biến các module đã chạy riêng lẻ (Auth, Category, Recipe, Search) thành một sản phẩm liền mạch: một khung điều hướng duy nhất, không còn link chết, trang chủ có dữ liệu thật, có dashboard cho tác giả, và một bài kiểm tra E2E đi hết hành trình khách → đăng ký → viết bài → publish → thấy bài của mình.

**Architecture:** Thêm lớp dùng chung ở frontend (route registry, app shell, toast, dashboard shell) và lấp các trang SRS mục 5.1 còn thiếu. Backend chỉ thêm hai khả năng frontend đang cần: đọc recipe theo slug và lọc "recipe của tôi". Một test kiểm tra tính toàn vẹn link và một test Playwright giữ cho các phần không lệch nhau trở lại.

**Tech Stack:** Next.js 15 App Router, React 19, TypeScript, CSS thuần với design tokens (`app/design-tokens.css`), TanStack Query, `node --test`, Playwright; .NET 10 Minimal API, MediatR, xUnit.

**Spec:** `docs/SRS_Culinary_Blog_v1.0.0.md` mục 5.1 (bảng route), FR-RCP-001/002, NFR-USE-002/004, NFR-SEO-001..004; `docs/design/DESIGN_HANDOFF.md`; `docs/SRS_CONTRADICTIONS_2026-10-23.md` C-13, C-16, C-31; `docs/CR-002_PLAN.md` (T-4, T-5, H-3 chồng lên kế hoạch này, xem Task 6, 8).

## Global Constraints

- Frontend dùng CSS thuần với token trong `design-tokens.css` (Tailwind không được cài); không thêm CSS framework. Màu: primary `#2D5D43`, bg `#F6F5F0`, ink `#1C2118`. Font Bricolage Grotesque (tiêu đề), Be Vietnam Pro (nội dung).
- Mọi chữ hiển thị bằng tiếng Việt; mã lỗi API dùng `problem.type`, không hardcode chuỗi lỗi.
- Touch target ≥ 44px, tương phản chữ ≥ 4.5:1, focus ring 2px `#2D5D43`, thẻ ngữ nghĩa (`header`, `nav`, `main`, `footer`).
- Mỗi trang có `<title>` dạng `{Tên} | Culinary Blog`; recipe Draft/Archived là `noindex`.
- Lỗi validation 422 `VALIDATION_ERROR`, JSON hỏng 400 `MALFORMED_REQUEST`; Draft/Archived của người khác trả 404 (quyết định C-16 đang áp dụng).
- Frontend: `npm run typecheck`, `npm run lint`, `npm test` sạch; backend: `dotnet build CulinaryBlog.sln -c Release --warnaserror` 0 warning.
- Test viết trước, thấy đỏ rồi mới viết code. Mỗi task một commit theo Conventional Commits.
- Không ghi dòng attribution Claude vào commit hoặc PR (quy tắc của người dùng, ưu tiên hơn nhắc nhở mặc định).
- Không đưa mật khẩu hay token vào code hoặc test; lấy từ biến môi trường hoặc sinh ngẫu nhiên.
- Quyền sở hữu: backend Recipe thuộc Trường. Task 6 và 8 chạm module đó, nên mở PR riêng, gắn Trường duyệt, không gộp vào PR frontend.
- Các file cá nhân (`docs/BAO_CAO_LAB_*`, `docs/W0*_HIEU_*`, `tmp/`) giữ nguyên chưa theo dõi.

## Review Focus

- Người chưa đăng nhập vào `/dashboard/*` hoặc `/dashboard/categories` phải bị chuyển tới `/login?returnUrl=...`, đăng nhập xong quay lại đúng trang; Author vào trang Admin thấy thông báo không đủ quyền chứ không phải trang trắng.
- Dashboard chưa có recipe nào: phải có trạng thái rỗng kèm nút "Viết công thức đầu tiên", không phải bảng trống.
- Xóa/lưu trữ recipe: phải có hộp xác nhận; publish thiếu nguyên liệu/bước (422 `RECIPE_PUBLISH_INCOMPLETE`) phải hiện toast lỗi nêu lý do.
- Mở `/recipes/{slug}` của Draft người khác (hoặc slug không tồn tại) phải ra trang 404 thân thiện, không lộ sự tồn tại của recipe.
- Danh mục không còn recipe nào, hoặc API lỗi: trang chủ vẫn dựng được (khối đó ẩn hoặc báo lỗi nhẹ), không làm sập cả trang.
- Link cũ `/recipes/{id}` và `/admin/categories` (đã được lưu hoặc nằm trong sitemap cũ) phải chuyển hướng tới URL mới, không 404.

---

## File Structure

Frontend (`src/CulinaryBlog.Web`):

- `lib/navigation/routes.ts` (mới): hằng số route, hàm dựng URL, hàm `buildNavItems`.
- `components/shared/site-header.tsx` (sửa), `components/shared/site-footer.tsx` (mới), `components/shared/toast.tsx` (mới).
- `components/dashboard/dashboard-shell.tsx`, `components/dashboard/recipe-status-badge.tsx`, `components/dashboard/confirm-dialog.tsx` (mới).
- `lib/home/home-data.ts` (mới): lấy và dựng view model trang chủ.
- `app/page.tsx` (viết lại), `app/categories/page.tsx`, `app/categories/[slug]/page.tsx`, `app/dashboard/layout.tsx`, `app/dashboard/page.tsx`, `app/dashboard/recipes/page.tsx`, `app/dashboard/categories/page.tsx` (mới), `app/robots.ts` (mới).
- `app/recipes/[id]` đổi thành `app/recipes/[slug]`; `app/admin/categories` thành trang chuyển hướng.
- `e2e/` (mới): Playwright.

Backend: `src/CulinaryBlog.Application/Recipes/Queries/GetRecipeBySlug/`, `GetRecipeListQuery.cs` (sửa), `src/CulinaryBlog.API/Endpoints/EndpointRouteBuilderExtensions.cs` (sửa).

---

### Task 1: Route registry và test toàn vẹn link

**Files:**
- Create: `src/CulinaryBlog.Web/lib/navigation/routes.ts`
- Test: `src/CulinaryBlog.Web/tests/navigation/route-integrity.test.ts`

**Interfaces:**
- Produces: `routes` (object hằng số), `recipeHref(slug: string): string`, `categoryHref(slug: string): string`, `isKnownRoute(path: string): boolean`. Các task sau chỉ dùng những hàm này để dựng link.

- [ ] **Step 1: Viết test đỏ `route-integrity.test.ts`** gồm hai test:
  - `every internal href in source resolves to an existing page`: quét `app/` và `components/` lấy mọi chuỗi `href="/..."` và `router.push|replace("/...")` dạng literal, bỏ phần query và hash, đối chiếu với tập route sinh từ các `page.tsx` trong `app/` (coi `[x]` là khớp bất kỳ một đoạn). Assert danh sách link không khớp rỗng.
  - `route registry only lists existing pages`: mọi giá trị trong `routes` (trừ động) khớp một `page.tsx`.
- [ ] **Step 2: Chạy `npm test -- tests/navigation`**. Mong đợi FAIL, liệt kê tối thiểu `/categories` và `/dashboard/recipes`.
- [ ] **Step 3: Tạo `routes.ts`** với các key: `home`, `recipes`, `search`, `categories`, `login`, `register`, `profile`, `dashboard`, `dashboardRecipes`, `dashboardRecipeNew`, `dashboardCategories`; `recipeHref` trả `/recipes/${slug}`, `categoryHref` trả `/categories/${slug}`. Test ở Step 1 vẫn đỏ cho đến Task 3–7 thêm các trang; đánh dấu hai test bằng `{ todo: true }` cho tới Task 7 rồi gỡ ở bước cuối của Task 7.
- [ ] **Step 4: Chạy lại test**. Mong đợi: các test khác PASS, test toàn vẹn ở trạng thái todo.
- [ ] **Step 5: Commit** `test(web): add route registry and internal link integrity test`.

### Task 2: Khung ứng dụng dùng chung (header, footer) và điều hướng theo vai trò

**Files:**
- Modify: `app/layout.tsx`, `components/shared/site-header.tsx`, `components/shared/index.ts`, các trang đang tự render `<SiteHeader />` (`app/recipes/page.tsx`, `app/search/**`, `app/profile/page.tsx`, trang auth, `app/admin/categories/**`)
- Create: `components/shared/site-footer.tsx`
- Test: `tests/navigation/nav-items.test.ts`

**Interfaces:**
- Consumes: `routes` (Task 1), kiểu `User` trong `lib/auth`.
- Produces: `buildNavItems(user: { roles: string[] } | null): NavItem[]` trong `lib/navigation/routes.ts`, với `NavItem = { label: string; href: string; requiresRole?: "Author" | "Admin" }`.

- [ ] **Step 1: Test đỏ `nav-items.test.ts`**: khách thấy `Công thức`, `Danh mục`, `Tìm kiếm`; Author thấy thêm `Bảng điều khiển` (`/dashboard`); Admin thấy thêm `Quản lý danh mục` (`/dashboard/categories`); vai trò không phân biệt hoa thường.
- [ ] **Step 2: Chạy test**, mong đợi FAIL vì `buildNavItems` chưa có.
- [ ] **Step 3: Implement `buildNavItems`** và dùng nó trong `SiteHeader`, thay danh sách vai trò hardcode hiện tại (`admin`, `chef`, `creator`...) bằng đúng hai vai trò `Author` và `Admin` theo SRS 2.3.
- [ ] **Step 4: Đưa `SiteHeader` và `SiteFooter` vào `app/layout.tsx`**, thêm liên kết "Bỏ qua tới nội dung" trước header; xóa các lần render `SiteHeader` riêng trong từng trang. Footer có link `Công thức`, `Danh mục`, `Tìm kiếm` và dòng bản quyền nhóm.
- [ ] **Step 5: Chạy `npm run typecheck && npm run lint && npm test`**, mong đợi sạch; mở thủ công `/login`, `/recipes`, `/search`, `/profile` thấy cùng một header.
- [ ] **Step 6: Commit** `feat(web): share one site header and footer across all routes`.

### Task 3: Trang chủ kết nối dữ liệu thật

**Files:**
- Modify: `app/page.tsx`, `app/globals.css`
- Create: `lib/home/home-data.ts`
- Test: `tests/home/home-data.test.ts`

**Interfaces:**
- Consumes: `recipeServerApiBaseUrl()`, `routes`, `recipeHref`, `categoryHref`.
- Produces: `getHomeData(): Promise<HomeViewModel>` với `HomeViewModel = { featured: RecipeCardModel[]; categories: { name: string; slug: string; recipeCount: number }[]; state: "ok" | "partial" | "error" }`; và hàm thuần `buildHomeViewModel(recipes, categories, failures)` để test.

- [ ] **Step 1: Test đỏ** cho `buildHomeViewModel`: 
  - chỉ lấy recipe `Published`, tối đa 6, mới nhất trước, `href` dùng slug; 
  - danh mục có `recipeCount === 0` bị ẩn; 
  - nếu recipe lỗi mà danh mục ổn thì `state === "partial"` và `featured` rỗng; 
  - cả hai lỗi thì `state === "error"`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Implement `home-data.ts`**; gọi `GET /recipes?status=Published` và `GET /categories` song song bằng `Promise.allSettled`, đặt `export const revalidate = 3600` cho trang (ISR theo SRS 5.1).
- [ ] **Step 4: Viết lại `app/page.tsx`**: hero có ô tìm kiếm (form GET tới `/search`), lưới 6 recipe nổi bật, dải danh mục dẫn tới `/categories/{slug}`, lời kêu gọi đăng ký khi chưa đăng nhập hoặc "Viết công thức" khi đã đăng nhập. Xóa khung "Kiến trúc sẵn sàng" và nút "Xem API".
- [ ] **Step 5: Chạy toàn bộ test frontend**, mong đợi sạch. Kiểm tra thủ công khi tắt API: trang chủ vẫn dựng và hiện thông báo nhẹ.
- [ ] **Step 6: Commit** `feat(web): home page shows featured recipes and categories`.

### Task 4: Hệ thống toast và hộp xác nhận

**Files:**
- Create: `components/shared/toast.tsx`, `components/dashboard/confirm-dialog.tsx`
- Modify: `app/layout.tsx`, `components/shared/index.ts`
- Test: `tests/shared/toast-store.test.ts`

**Interfaces:**
- Produces: `ToastProvider`, `useToast(): { success(msg: string): void; error(msg: string): void }` (tự đóng sau 5 giây, `role="status"` cho thành công và `role="alert"` cho lỗi); `ConfirmDialog({ open, title, description, confirmLabel, destructive, onConfirm, onCancel })` (đóng bằng Escape, trả focus về nút gọi).

- [ ] **Step 1: Test đỏ cho store toast thuần** (`createToastStore`): thêm toast, tự hết hạn khi tới `durationMs`, tối đa 3 toast hiển thị, toast lỗi dùng `role: "alert"`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Implement store và `ToastProvider`**, gắn vào `app/layout.tsx` cạnh `QueryProvider`; implement `ConfirmDialog` bằng phần tử `<dialog>` hoặc `role="dialog" aria-modal`.
- [ ] **Step 4: Chạy typecheck, lint, test**, mong đợi sạch.
- [ ] **Step 5: Commit** `feat(web): add toast notifications and confirm dialog`.

### Task 5: Trang `/categories` và `/categories/[slug]`

**Files:**
- Create: `app/categories/page.tsx`, `app/categories/[slug]/page.tsx`, `app/categories/[slug]/not-found.tsx`, `lib/categories/server-categories.ts`
- Test: `tests/categories/category-view.test.ts`

**Interfaces:**
- Consumes: API có sẵn `GET /categories` (`CategoryDto[]`) và `GET /categories/{slug}?page&pageSize` (`CategoryDetailDto`, 404 `CATEGORY_NOT_FOUND`, 422 `VALIDATION_ERROR`); `categoryHref`, `recipeHref`, `RecipeCard`.
- Produces: `getCategoryList()`, `getCategoryDetail(slug: string, page: number)`, và `parseCategoryPage(raw: string | undefined): number` (số nguyên dương, mặc định 1).

- [ ] **Step 1: Test đỏ** cho `parseCategoryPage` (`undefined`, `"0"`, `"abc"`, `"-2"`, `"3"`) và cho hàm dựng view model chi tiết danh mục (trạng thái rỗng khi không có recipe, tổng số trang).
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Implement** hai hàm lấy dữ liệu; `/categories` là ISR `revalidate = 3600`, `/categories/[slug]` là ISR `revalidate = 600` với `generateMetadata` (title `{Tên danh mục} | Culinary Blog`, mô tả); slug không tồn tại gọi `notFound()`.
- [ ] **Step 4: Gỡ `todo` ở test toàn vẹn link của Task 1 cho `/categories`**, chạy lại, mong đợi PASS phần này.
- [ ] **Step 5: Commit** `feat(web): add category list and category detail pages`.

### Task 6: Backend — đọc recipe theo slug và lọc recipe của tôi

> Chạm module của Trường: mở PR riêng, Trường duyệt. Trùng T-4 và T-5 của `CR-002_PLAN`; nếu Trường đã làm thì bỏ task này và dùng kết quả của họ.

**Files:**
- Create: `src/CulinaryBlog.Application/Recipes/Queries/GetRecipeBySlug/GetRecipeBySlugQuery.cs` và handler cùng thư mục
- Modify: `GetRecipeListQuery.cs`, handler tương ứng, `EndpointRouteBuilderExtensions.cs`
- Test: `tests/CulinaryBlog.Integration.Tests/RecipeBySlugEndpointTests.cs`, `RecipeListScopeTests.cs`

**Interfaces:**
- Produces: `GET /api/v1/recipes/{slug}` (route ràng buộc không phải guid) trả `RecipeDetailDto` kèm ETag như `GetRecipeDetail`; `GET /api/v1/recipes?scope=mine` yêu cầu đăng nhập, trả mọi trạng thái của người dùng hiện tại, thiếu token trả 401. `GetRecipeListQuery` thêm tham số `bool Mine = false`.

- [ ] **Step 1: Test tích hợp đỏ**:
  - slug của recipe Published trả 200 cho khách;
  - slug không tồn tại trả 404 `RECIPE_NOT_FOUND`;
  - slug của Draft: chủ và Admin 200, người khác và khách 404;
  - `scope=mine` không token trả 401;
  - `scope=mine` của Author A không chứa recipe của B và chứa cả Draft/Archived của A.
- [ ] **Step 2: Chạy** `dotnet test tests/CulinaryBlog.Integration.Tests --filter "RecipeBySlug|RecipeListScope"`, mong đợi FAIL.
- [ ] **Step 3: Implement query và đăng ký route**; tái dùng logic quyền xem của `GetRecipeDetailQuery`, không sao chép. `{id:guid}` và `{slug}` cùng tồn tại, ràng buộc `:guid` đảm bảo không nhầm.
- [ ] **Step 4: Chạy lại bộ test trên và toàn bộ `RecipeEndpointTests`**, mong đợi PASS, không test cũ nào đỏ.
- [ ] **Step 5: Commit** `feat(api): read recipe by slug and list own recipes`.

### Task 7: Trang chi tiết recipe theo slug và link nhất quán

**Files:**
- Rename: `app/recipes/[id]` → `app/recipes/[slug]` (gồm `layout.tsx`, `page.tsx`, `error.tsx`, `not-found.tsx`)
- Modify: `lib/recipes/client.ts` (thêm `getBySlug`), `lib/recipes/server-detail.ts`, `lib/recipes/list.ts` (đổi `href` sang `recipeHref(slug)`), `components/recipes/recipe-card.tsx`, `components/recipes/recipe-detail.tsx`
- Create: `app/recipes/[id]/page.tsx` dạng chuyển hướng chỉ khi tham số là UUID; dùng route `app/recipes/by-id/[id]/route.ts` nếu Next không cho hai segment động cùng cấp
- Test: `tests/recipes/recipe-links.test.ts`

**Interfaces:**
- Consumes: `GET /recipes/{slug}` (Task 6), `recipeHref`, `categoryHref`.
- Produces: `RecipeClient.getBySlug(slug: string): Promise<RecipeDetailDto>`; `createRecipeListViewModel` trả `href` dạng `/recipes/{slug}`.

- [ ] **Step 1: Test đỏ** `recipe-links.test.ts`: `createRecipeListViewModel` tạo `href` theo slug; trang chi tiết có breadcrumb `Công thức › {Danh mục} › {Tên}` với link `categoryHref`.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Implement**: trang chi tiết tìm theo slug, 404 cho Draft/Archived của người khác (khớp C-16), `generateMetadata` có Open Graph và `robots: { index: false }` khi không phải Published. Request tới `/recipes/{uuid}` chuyển hướng 308 sang URL slug của recipe đó.
- [ ] **Step 4: Cập nhật dashboard editor và các link nội bộ còn dùng id để hiển thị trang công khai**; editor vẫn dùng id cho ghi.
- [ ] **Step 5: Chạy toàn bộ test frontend**, mong đợi PASS; gỡ `todo` còn lại của Task 1, test toàn vẹn link phải xanh cho `/recipes/[slug]`.
- [ ] **Step 6: Commit** `feat(web): serve recipe detail by slug and redirect legacy id URLs`.

### Task 8: Dashboard tác giả (`/dashboard`, `/dashboard/recipes`)

**Files:**
- Create: `app/dashboard/layout.tsx`, `app/dashboard/page.tsx`, `app/dashboard/recipes/page.tsx`, `components/dashboard/dashboard-shell.tsx`, `components/dashboard/recipe-status-badge.tsx`, `components/dashboard/my-recipes-table.tsx`, `lib/recipes/my-recipes.ts`
- Modify: `app/dashboard/recipes/layout.tsx` (gộp vào layout dashboard), `lib/recipes/client.ts` (`listMine()`)
- Test: `tests/dashboard/my-recipes.test.ts`

**Interfaces:**
- Consumes: `GET /recipes?scope=mine` (Task 6), `publish/unpublish/archive/delete` đã có trong `RecipeClient`, `useToast`, `ConfirmDialog`, `ProtectedRoute`.
- Produces: `summarizeMyRecipes(items: RecipeListItemDto[]): { draft: number; published: number; archived: number }`; `availableActions(status: RecipeStatus): ("publish" | "unpublish" | "archive" | "delete")[]` (Draft: publish, delete; Published: unpublish, archive; Archived: delete); `explainLifecycleError(problemType: string): string` (ánh xạ `RECIPE_PUBLISH_INCOMPLETE` sang câu tiếng Việt nêu cần thêm nguyên liệu và bước).

- [ ] **Step 1: Test đỏ** cho ba hàm thuần ở trên, gồm trường hợp danh sách rỗng và mã lỗi lạ (trả thông báo chung).
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Implement dashboard**: `DashboardShell` có sidebar 260px (thu xuống trên mobile) với `Tổng quan`, `Công thức của tôi`, và `Danh mục` chỉ cho Admin; `/dashboard` hiện ba thẻ số lượng và nút "Viết công thức mới"; `/dashboard/recipes` hiện bảng kèm huy hiệu trạng thái, nút sửa (tới `/dashboard/recipes/{id}/edit`), xem (tới trang slug, chỉ khi Published) và các hành động từ `availableActions`. Xóa và lưu trữ qua `ConfirmDialog`; thành công/thất bại qua toast; làm mới danh sách bằng TanStack Query invalidate. Trạng thái rỗng có nút "Viết công thức đầu tiên".
- [ ] **Step 4: Bọc toàn bộ `/dashboard/**` bằng `ProtectedRoute` yêu cầu Author hoặc Admin**, chuyển về `/login?returnUrl=...` khi chưa đăng nhập.
- [ ] **Step 5: Chạy typecheck, lint, test**, mong đợi sạch; kiểm tra thủ công vòng publish → thấy ở `/recipes` → unpublish → biến mất.
- [ ] **Step 6: Commit** `feat(web): add author dashboard with recipe lifecycle actions`.

### Task 9: Quản trị danh mục chuyển về `/dashboard/categories`

**Files:**
- Move: `app/admin/categories/category-manager.tsx` → `app/dashboard/categories/category-manager.tsx`
- Create: `app/dashboard/categories/page.tsx`
- Modify: `app/admin/categories/page.tsx` (chuyển hướng), `lib/navigation/routes.ts`, nginx/Next config nếu cần redirect cấp cấu hình
- Test: `tests/dashboard/admin-guard.test.ts`

**Interfaces:**
- Produces: `canAccessAdmin(roles: string[] | undefined): boolean` (đúng `Admin`, không phân biệt hoa thường).

- [ ] **Step 1: Test đỏ** `canAccessAdmin`: Admin true, Author false, rỗng và `undefined` false.
- [ ] **Step 2: Chạy**, mong đợi FAIL.
- [ ] **Step 3: Implement**: trang mới nằm trong `DashboardShell`, dùng `canAccessAdmin` để hiện thông báo "Bạn không có quyền truy cập" cho Author; `/admin/categories` chuyển hướng 308 sang `/dashboard/categories`. Nút Xóa vô hiệu khi `recipeCount > 0` (đúng `DESIGN_HANDOFF`).
- [ ] **Step 4: Chạy test frontend**, mong đợi PASS.
- [ ] **Step 5: Commit** `refactor(web): move category admin under the dashboard`.

### Task 10: SEO tối thiểu thống nhất

**Files:**
- Create: `app/robots.ts`
- Modify: `deploy/nginx/nginx.conf` (proxy `/sitemap.xml` tới API nếu sitemap do API phục vụ), `app/categories/**`, `app/search/page.tsx` (metadata)
- Test: `tests/seo/robots.test.ts`

**Interfaces:**
- Produces: `robots()` của Next trả `allow: "/"`, `disallow: ["/dashboard", "/profile", "/api"]` và `sitemap` trỏ `${origin}/sitemap.xml`.

- [ ] **Step 1: Xác định nơi sitemap đang được phục vụ** bằng cách gọi `GET /sitemap.xml` qua Nginx và qua API; ghi kết quả vào mô tả commit.
- [ ] **Step 2: Test đỏ** `robots.test.ts` kiểm tra đúng cấu hình trên.
- [ ] **Step 3: Implement `robots.ts`**, thêm proxy Nginx nếu sitemap chỉ có ở API; đảm bảo URL trong sitemap khớp `/recipes/{slug}` (phụ thuộc K-2 của `CR-002_PLAN`; ghi lại nếu chưa xong).
- [ ] **Step 4: Chạy test và `curl http://localhost/robots.txt`**, mong đợi 200 và có dòng `Sitemap:`.
- [ ] **Step 5: Commit** `feat(web): add robots.txt and expose the sitemap through the proxy`.

### Task 11: E2E Playwright cho hành trình chính

**Files:**
- Create: `src/CulinaryBlog.Web/e2e/journey.spec.ts`, `e2e/helpers.ts`, `playwright.config.ts`
- Modify: `package.json` (script `e2e`, devDependency `@playwright/test` ghim phiên bản cụ thể), `.github/workflows/ci.yml` (job tùy chọn chạy trên Compose)

**Interfaces:**
- Consumes: toàn bộ các task trên; `BASE_URL` (mặc định `http://localhost`); `ADMIN_EMAIL` và `ADMIN_PASSWORD` từ môi trường, bỏ qua test admin nếu thiếu.
- Produces: `registerAuthor(page): Promise<{ email: string; password: string }>` với email ngẫu nhiên theo timestamp và mật khẩu sinh ngẫu nhiên.

- [ ] **Step 1: Viết spec đỏ** gồm các luồng:
  - `guest can go from home to a recipe through a category`: trang chủ → danh mục → recipe → thấy nguyên liệu và các bước;
  - `new author can write and publish a recipe and see it listed`: đăng ký → `/dashboard/recipes` rỗng có nút tạo → tạo recipe qua wizard (có nguyên liệu và bước) → publish → thấy trong `/recipes` và tìm được ở `/search`;
  - `publishing an incomplete recipe shows a clear error`: publish thiếu bước → toast nêu lý do;
  - `author cannot open admin pages`: `/dashboard/categories` hiện thông báo không đủ quyền;
  - `legacy urls redirect`: `/admin/categories` (khi là Admin) và `/recipes/{uuid}` chuyển hướng.
- [ ] **Step 2: Chạy** `npm run e2e` trên stack Compose đang chạy, mong đợi FAIL ở bước nào chưa nối. Sửa lỗi lộ ra ở task tương ứng, không vá trong test.
- [ ] **Step 3: Chạy lại cho tới khi cả năm luồng PASS.**
- [ ] **Step 4: Cập nhật `scripts/smoke-test.mjs`** để dùng `GET /recipes/{slug}`; thêm hướng dẫn chạy E2E vào README.
- [ ] **Step 5: Commit** `test(web): add end-to-end user journey tests`.

### Task 12: Chốt tài liệu và kiểm tra cuối

**Files:**
- Modify: `docs/SRS_Culinary_Blog_v1.0.0.md` mục 5.1 (đường dẫn thực tế `/login`, `/register`, `/dashboard/categories`), `docs/design/DESIGN_HANDOFF.md` (bỏ mục "Chưa có" đã làm), `README.md`

- [ ] **Step 1: Đối chiếu bảng route SRS 5.1 với `routes.ts`**, sửa các dòng lệch; ghi vào bảng lịch sử thay đổi SRS dưới nhãn CR-002 (không đổi số phiên bản nếu nhóm chưa họp chốt).
- [ ] **Step 2: Chạy toàn bộ kiểm tra**: `dotnet build CulinaryBlog.sln -c Release --warnaserror`, `dotnet test CulinaryBlog.sln`, `npm run typecheck && npm run lint && npm test`, `docker compose -p culinary-w08 up --build -d`, `node scripts/smoke-test.mjs`, `npm run e2e`. Mong đợi tất cả xanh.
- [ ] **Step 3: Commit** `docs: sync routes and design handoff with the connected user journey`.

---

## Thứ tự và phụ thuộc

`1 → 2 → (3, 4 song song) → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12`. Task 6 là điểm chặn của 7 và 8; nếu Trường chưa kịp, làm 3, 4, 5, 9 trước.

## Self-review

- **Phủ yêu cầu:** link chết (Task 1, 5, 8), hai header (Task 2), trang chủ rời rạc (Task 3), thiếu dashboard và `/dashboard` (Task 8), đường dẫn lệch SRS (Task 7, 9, 12), toast và xác nhận (Task 4), SEO tối thiểu (Task 10), hành trình end-to-end (Task 11).
- **Nhất quán tên:** `routes`, `recipeHref`, `categoryHref` (Task 1) được dùng ở Task 3, 5, 7; `useToast` và `ConfirmDialog` (Task 4) được dùng ở Task 8; `scope=mine` (Task 6) được dùng ở Task 8.
- **Chưa kiểm chứng khi lập plan:** nơi `sitemap.xml` được phục vụ (Task 10 Step 1 xác minh); Next có cho `[id]` và `[slug]` cùng cấp hay không (Task 7 đã nêu phương án thay thế); các chi tiết chưa đọc kỹ của SRS chương 7–8.
- **Ngoài phạm vi (cố ý):** cache Redis, HTTPS/HSTS/CSP, Lighthouse CI và k6, sửa 33 mâu thuẫn SRS, khóa ngoại Recipe. Những việc này đã nằm trong `CR-002_PLAN` hoặc cần quyết định riêng của nhóm.
