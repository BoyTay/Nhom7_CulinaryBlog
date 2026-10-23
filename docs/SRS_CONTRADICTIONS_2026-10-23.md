# Mâu thuẫn trong `SRS_Culinary_Blog_v1.0.0.md`

Ngày rà soát: 23/10/2026. Phiên bản SRS được rà: 1.0.1 (CR-001, ngày 20/09/2026). Số dòng dưới đây là số dòng trong file `docs/SRS_Culinary_Blog_v1.0.0.md`.

Mục đích: liệt kê những chỗ SRS **tự mâu thuẫn** (hai đoạn khác nhau của chính tài liệu nói ngược nhau) để nhóm chốt bằng một change request (đề xuất đặt tên CR-002), thay vì để mỗi thành viên tự đoán. Mục "Code hiện tại" cho biết bên nào đang được hiện thực để biết chọn phương án nào ít tốn công nhất. Những chỗ code chỉ lệch SRS mà SRS không mâu thuẫn không nằm trong tài liệu này.

**Lưu ý phân loại (cập nhật 23/10/2026 sau khi rà lại):** ba mục **C-03, C-13, C-16 không phải mâu thuẫn trong SRS**. Ở các mục đó SRS nhất quán hoặc chỉ thiếu, còn chỗ sai nằm ở code (hoặc là quyết định thiết kế cần chọn). Tôi giữ nguyên mã để các issue CR-002 vẫn tham chiếu được, nhưng chúng thuộc nhóm "code lệch SRS" hay "khoảng trống", không thuộc nhóm cần họp để thống nhất giữa hai đoạn SRS.

Mức độ: **Cao** = ảnh hưởng hợp đồng API hoặc schema đã khóa, cần chốt trước khi khóa cuối tuần 8; **Vừa** = ảnh hưởng hành vi hoặc kiến trúc nhưng sửa được sau; **Thấp** = diễn đạt, không đổi code.

## Bảng tóm tắt

| Mã | Chủ đề | Mức | Đề xuất chốt |
| --- | --- | --- | --- |
| C-01 | Lỗi validation: 400 hay 422 | Cao | 422 cho validation và nghiệp vụ, 400 chỉ cho body hỏng cú pháp |
| C-02 | Trường `type` của Problem Details | Vừa | Dùng mã lỗi ứng dụng, bỏ `about:blank` |
| C-03 † | Tài khoản khóa tạm, vô hiệu: code lệch SRS và thiếu ở phụ lục | Cao | Sửa code theo SRS (423, 403); bổ sung Phụ lục A/B |
| C-04 | Google token sai: 401 hay 400 | Vừa | 401 |
| C-05 | Cách trả refresh token (cookie hay body) | Cao | Cookie theo ADR-0003; sửa chương 8 |
| C-06 | Phản hồi đăng ký | Cao | Giống đăng nhập (token và user) |
| C-07 | Trường hồ sơ người dùng | Cao | `displayName`, `bio`; bỏ `fullName`, `userName`, `emailConfirmed` |
| C-08 | Luồng Google: Authorization Code + PKCE hay ID token | Vừa | ID token (CR-001) |
| C-09 | Thu hồi refresh token: `IsRevoked` hay `RevokedAt`; xoay vòng khi đăng nhập | Thấp | `RevokedAt`; chỉ xoay vòng ở `/refresh` |
| C-10 | Phát hiện tái dùng token: bắt buộc hay tùy chọn | Vừa | Bắt buộc (NFR-SEC-002) |
| C-11 | Kích hoạt email, OTP so với "không có xác nhận email" | Thấp | Bỏ khỏi MVP |
| C-12 | Đăng xuất khi access token hết hạn | Thấp | Cho phép nếu có refresh token hợp lệ |
| C-13 † | Recipe đọc theo `slug`: SRS nhất quán, code đọc theo `id` | Cao | Sửa code: đọc theo slug, ghi theo id |
| C-14 | Dạng phân trang và tham số sắp xếp | Cao | `PagedResult` và `sort=-field` |
| C-15 | Ai thấy recipe nào trong danh sách | Vừa | Theo FR-RCP-001 |
| C-16 † | Draft/Archived của người khác: SRS nói 403, code trả 404 | Vừa | Chọn 403 hay 404 rồi sửa phía còn lại |
| C-17 | Mức độ khó: 3 hay 4 giá trị, chuỗi hay số | Cao | Chốt enum trước khi khóa schema |
| C-18 | `Instructions` bắt buộc hay tùy chọn | Cao | Tùy chọn hoặc bỏ cột |
| C-19 | Bước thực hiện: `Title`, `stepNumber` | Cao | Bỏ `Title` hoặc cho tùy chọn; `stepNumber` tự sinh |
| C-20 | Nguyên liệu: `Quantity`/`Unit` nullable, `SortOrder` hay `OrderIndex` | Cao | Bắt buộc, `SortOrder` |
| C-21 | Dinh dưỡng: 4 hay 6 trường | Cao | Chốt danh sách trường trước khi khóa schema |
| C-22 | API ảnh: đặt ảnh chính và dạng phản hồi | Vừa | `PATCH .../primary`; trả `RecipeImageDto` |
| C-23 | Xóa danh mục: cứng hay mềm | Vừa | Mềm (CR-001) |
| C-24 | Danh mục: trường cập nhật, thứ tự sắp xếp | Vừa | Có `imageUrl`, `orderIndex`; sắp theo tên |
| C-25 | Kiểm tra magic bytes chỉ bằng 4 byte đầu | Vừa | Đọc đủ byte theo từng định dạng |
| C-26 | Cơ chế cache và TTL | Cao | Nêu một cơ chế cho mỗi đối tượng |
| C-27 | Redis lỗi: fallback hay readiness lỗi | Vừa | Redis không chặn readiness |
| C-28 | Rate limiting: Nginx, ASP.NET hay Redis | Vừa | ASP.NET Core theo IP thật qua forwarded headers |
| C-29 | `ApplicationUser` nằm trong Domain nhưng Domain chỉ dùng BCL | Cao | Chuyển `ApplicationUser` sang Infrastructure |
| C-30 | `LoggingBehavior` ghi tham số so với yêu cầu không log dữ liệu nhạy cảm | Vừa | Chỉ ghi tên request, thời gian, loại lỗi |
| C-31 | Trang recipe ISR so với recipe Draft cần xác thực | Vừa | Dùng render động cho recipe không công khai |
| C-32 | Phiên bản Node.js | Thấp | Node 22 |
| C-33 | Tailwind và "không dùng CSS framework override" | Thấp | Làm rõ câu chữ |

## 1. Lỗi và mã trạng thái

### C-01. Lỗi validation: 400 hay 422 (Cao)

- Dòng 43 (CR-001): "Lỗi cú pháp, binding và FluentValidation trả HTTP 400; lỗi quy tắc nghiệp vụ hoặc optimistic concurrency trả HTTP 422".
- Dòng 1570, 1610 (Phụ lục A và B): `VALIDATION_ERROR` là 400; "Validation lỗi (FluentValidation)" là 400. Chương 8 (dòng 1471, 1482, 1498): 400.
- Nhưng các FR ghi 422 cho cùng loại lỗi: FR-AUTH-001 A2/A3 (dòng 519), FR-AUTH-002 (543), FR-AUTH-007 (640, 643), FR-CAT-003 (708, 711), FR-RCP-001 (773), FR-RCP-003 (816), FR-RCP-009 A3, FR-SRCH-001 (987 và bảng mã trả về).
- Phụ lục A dòng 1576 còn nói 422 chỉ dành cho xung đột `xmin`.
- Code hiện tại: validation trả 422 `VALIDATION_ERROR`; body hỏng cú pháp trả 400 `MALFORMED_REQUEST`. Frontend và test đã dựa vào 422.
- Đề xuất: giữ như code. Sửa CR-001, Phụ lục A và B, chương 8 thành "FluentValidation: 422 `VALIDATION_ERROR`; JSON sai cú pháp hoặc sai kiểu: 400 `MALFORMED_REQUEST`". Cách ngược lại phải đổi mọi validator, frontend và test.

### C-02. Trường `type` của Problem Details (Vừa)

- Dòng 1463: ví dụ lỗi dùng `"type": "about:blank"`.
- Phụ lục B (dòng 1583): các mã `AUTH_...`, `RECIPE_...` nằm trong trường `type` để frontend xử lý theo chương trình. `about:blank` và mã ứng dụng không thể cùng là giá trị của `type`.
- Code hiện tại: `type` là mã ứng dụng, ví dụ `CATEGORY_NAME_EXISTS`; frontend đọc `problem.type`.
- Đề xuất: sửa ví dụ ở dòng 1463; dùng `type` cho mã lỗi, `title` và `detail` cho mô tả.

### C-03. Tài khoản khóa tạm hoặc vô hiệu (Cao) — không phải mâu thuẫn

- **SRS nhất quán về khóa tạm:** FR-AUTH-002 (dòng 535, 540, 543) nói lockout trả **423 Locked**, sai 5 lần thì khóa 15 phút. Không đoạn nào khác nói ngược lại.
- **Khoảng trống (SRS thiếu, không mâu thuẫn):**
  - Phụ lục A (dòng 1565–1579) không có hàng 423 và Phụ lục B không có mã lỗi cho lockout.
  - Luồng đăng nhập (dòng 538) không có bước kiểm tra `IsActive`, dù chương 7 (dòng 1431) định nghĩa `IsActive` và Phụ lục B (dòng 1595) có `AUTH_ACCOUNT_DISABLED` trả **403**.
- **Điểm mơ hồ nhỏ:** FR-AUTH-004 A4 (dòng 583) nói "user bị xóa hoặc bị khóa" khi refresh trả 401. "Bị khóa" không nói rõ là lockout tạm hay vô hiệu; nếu là vô hiệu thì khác 403 ở Phụ lục B, nhưng đó là hai endpoint khác nhau nên chưa chắc mâu thuẫn. Cần làm rõ một câu.
- **Code lệch SRS:** lockout trả 401 `ACCOUNT_LOCKED` (SRS: 423); `IsActive` không được kiểm tra khi đăng nhập (Phụ lục B: 403).
- Việc cần làm: sửa code theo SRS (Bo: B-1, B-2); bổ sung 423 vào Phụ lục A và mã lockout vào Phụ lục B; làm rõ FR-AUTH-004 A4.

### C-04. Google token sai: 401 hay 400 (Vừa)

- FR-AUTH-003 (dòng 561, 566): token Google không hợp lệ hoặc hết hạn trả **401**; thiếu thông tin profile trả 400.
- Phụ lục B (dòng 1594) và chương 8 (dòng 1473): `AUTH_GOOGLE_TOKEN_INVALID` trả **400**.
- FR-AUTH-003 còn dùng 502 khi Google API không khả dụng nhưng Phụ lục A không có 502.
- Code hiện tại: 401 `INVALID_GOOGLE_TOKEN`; có `GOOGLE_SERVICE_UNAVAILABLE`.
- Đề xuất: 401 cho token sai; thêm 502 vào Phụ lục A.

## 2. Hợp đồng xác thực

### C-05. Cách trả refresh token (Cao)

- FR-AUTH-001 (dòng 515) và FR-AUTH-003 (dòng 560): refresh token đặt trong **cookie bảo mật** theo ADR-0003.
- Cũng FR-AUTH-001 phần kết quả (dòng 521): "Client nhận được access token và refresh token".
- Chương 8: đăng nhập và Google trả `refreshToken` trong **body** (dòng 1472, 1473); refresh và logout nhận `{ refreshToken }` trong body (dòng 1474, 1480); FR-AUTH-004 (dòng 581) và FR-AUTH-005 (dòng 600) cũng dùng body.
- Code hiện tại: refresh token chỉ nằm trong cookie `rt` (`HttpOnly; Secure; SameSite=Strict; path=/api/v1/auth`); refresh đọc cookie.
- Đề xuất: cookie theo ADR-0003; sửa chương 8 và FR-AUTH-004, FR-AUTH-005 để bỏ `refreshToken` khỏi body.

### C-06. Phản hồi đăng ký (Cao)

- FR-AUTH-001 (dòng 515, 521): trả 201 với access token, thời hạn và user (auto-login).
- Chương 8 (dòng 1471): 201 chỉ trả `{ userId, email, displayName }`, không có token.
- Code hiện tại: 201 kèm `accessToken`, `expiresIn`, `user`, và cookie.
- Đề xuất: sửa chương 8 giống đăng nhập.

### C-07. Trường hồ sơ người dùng (Cao)

- FR-AUTH-006 (dòng 621): `{ id, fullName, email, userName, avatarUrl, roles, emailConfirmed, createdAt }`; FR-AUTH-007 (dòng 637, 639): cập nhật `fullName` và `avatarUrl`.
- Chương 8 (dòng 1481, 1482) và chương 7: `displayName`, `bio`, không có `userName`, `emailConfirmed`, `createdAt`. CR-001 (dòng 41) đã dùng `displayName` cho đăng ký.
- Code hiện tại: `{ id, email, displayName, avatarUrl, bio, roles }`. `PATCH` với `fullName` được bỏ qua mà không báo lỗi.
- Đề xuất: theo chương 8; sửa FR-AUTH-006 và FR-AUTH-007.

### C-08. Luồng Google (Vừa)

- FR-AUTH-003 phần mô tả (dòng 555) và mục 5.3 (dòng 1195): Authorization Code Flow với PKCE, cần `ClientSecret` và Redirect URI (dòng 558).
- Cùng FR đó, luồng chính (dòng 559) và CR-001 (dòng 42): frontend lấy ID token bằng Google Identity Services rồi gửi `{ idToken }`; backend chỉ xác minh chữ ký, issuer, audience, expiry.
- Code hiện tại: nhận `idToken`.
- Đề xuất: ID token; bỏ PKCE, `ClientSecret`, Redirect URI khỏi mô tả và mục 5.3.

### C-09. Thu hồi refresh token (Thấp)

- Dòng 606: "đánh dấu `IsRevoked = true`"; dòng 583, 590 và chương 7 (dòng 1446): trường là `RevokedAt` (NULL = còn hiệu lực).
- FR-AUTH-002 (dòng 534) mô tả cơ chế xoay vòng và thu hồi token cũ ngay trong đăng nhập, nhưng luồng 11 bước của đăng nhập không có bước thu hồi nào; việc xoay vòng chỉ có ở FR-AUTH-004.
- Đề xuất: dùng `RevokedAt` ở mọi nơi; đăng nhập chỉ cấp token mới, xoay vòng chỉ ở `/refresh`.

### C-10. Phát hiện tái dùng refresh token (Vừa)

- NFR-SEC-002 (dòng 1086): token đã thu hồi bị dùng lại thì **thu hồi toàn bộ family** (bắt buộc).
- FR-AUTH-004 A3 (dòng 583): "có thể" thu hồi toàn bộ ("paranoid mode") (tùy chọn).
- Code hiện tại: thu hồi toàn bộ family.
- Đề xuất: bắt buộc, sửa FR-AUTH-004.

### C-11. Xác nhận email (Thấp)

- CR-001 (dòng 47): không có luồng xác nhận email trong MVP.
- Nhưng FR-JOB-001 (dòng 1024) có "link kích hoạt email (nếu cần)", FR-AUTH-006 trả `emailConfirmed`, FR-AUTH-007 (dòng 637) nhắc "quy trình riêng có xác nhận OTP".
- Đề xuất: bỏ các tham chiếu trên khỏi MVP.

### C-12. Đăng xuất khi access token hết hạn (Thấp)

- FR-AUTH-005: điều kiện tiên quyết yêu cầu access token hợp lệ (dòng 598); A2 (dòng 604) cho phép logout khi access token hết hạn nếu refresh token còn hợp lệ; bảng mã (dòng 607): "401 – Access token không hợp lệ".
- Đề xuất: chốt một quy tắc. Đề xuất cho phép nếu có refresh token hợp lệ, vì cookie vẫn còn trong trình duyệt.

## 3. Recipe, Category, dữ liệu

### C-13. Recipe đọc theo slug (Cao) — không phải mâu thuẫn

- **SRS nhất quán:** đọc chi tiết theo slug (FR-RCP-002 dòng 793, 795; chương 8 dòng 1502; NFR-SEO-004 dòng 1141; mục 5.1 dòng 1154). Các thao tác ghi dùng `{id:guid}` (FR-RCP-004 đến 010 và chương 8). Đọc theo slug và ghi theo id là một thiết kế hợp lệ, không phải hai đoạn nói ngược nhau.
- **Code lệch SRS:** đọc chi tiết theo `{id}`; `GET /recipes/{slug}` trả 404; sitemap dùng `/recipes/{id}`; route web là `/recipes/[id]`.
- Việc cần làm: Trường thêm `GET /recipes/{slug}` và route web `[slug]` (T-4); Kiên đổi sitemap (K-2).

### C-14. Dạng phân trang và sắp xếp (Cao)

- Dòng 1462: tham số `page`, `pageSize=10`, `sortBy`, `sortOrder`; phản hồi `{ data[], meta{ page, pageSize, total, totalPages } }`.
- FR-RCP-001 (dòng 772) và FR-SRCH-003/004 (dòng 1003, 1004): `sort=-createdAt`, mặc định `pageSize=12`, tối đa 50; phản hồi `PagedResult` gồm `items`, `totalCount`, `page`, `pageSize`, `totalPages`, `hasNextPage`, `hasPreviousPage`.
- Chương 8 dòng 1491 và FR-SRCH-001 (dòng 987) dùng `pageSize=10` trong ví dụ, còn FR-CAT-002 (dòng 682) dùng 12.
- Code hiện tại: search và category dùng `PagedResult`; `GET /recipes` trả mảng thuần và không phân trang.
- Đề xuất: `PagedResult` và `sort=-field`, mặc định 12, tối đa 50; sửa dòng 1462.

### C-15. Ai thấy recipe nào trong danh sách (Vừa)

- FR-RCP-001 (dòng 769, 772): Guest thấy Published; người đăng nhập thấy thêm Draft của mình; Admin thấy tất cả.
- Chương 8 (dòng 1501): danh sách chỉ gồm Published.
- Đề xuất: theo FR-RCP-001 (đã gồm cả Draft và Archived của chính mình theo mô tả).

### C-16. Draft/Archived của người khác: 403 hay 404 (Vừa) — quyết định thiết kế, không phải mâu thuẫn

- FR-RCP-002 (dòng 795 và bảng mã) nói 403 khi không có quyền xem Draft hoặc Archived.
- Phụ lục B (`RECIPE_NOT_FOUND`) nói "không tồn tại hoặc đã bị xóa" và không nhắc đến Draft; chương 8 (dòng 1502) chỉ ghi "Draft: Author/Admin". Hai đoạn này không nói ngược 403.
- **Code lệch SRS:** trả 404 (không lộ sự tồn tại của recipe).
- Việc cần làm: chọn một. Trả 404 an toàn hơn nhưng khác FR-RCP-002 (cần sửa SRS); trả 403 đúng SRS nhưng lộ sự tồn tại recipe (cần sửa code). Kế hoạch hiện chọn 404.

### C-17. Mức độ khó (Cao)

- Chương 7 (dòng 1337): `smallint`, `1=Easy, 2=Medium, 3=Hard, 4=Expert`, mặc định 1.
- FR-SRCH-002 (dòng 1002): `difficulty={Easy|Medium|Hard}`; FR-RCP-001 (dòng 772): `difficulty=Easy` (chuỗi).
- Code hiện tại: enum 3 mức đánh số 0–2, lưu chuỗi `varchar(32)`.
- Đề xuất: chốt trước khi khóa schema: 3 hay 4 mức, lưu chuỗi hay số, tên giá trị.

### C-18. `Instructions` bắt buộc hay tùy chọn (Cao)

- Chương 7 (dòng 1333): `Instructions text NOT NULL` ("legacy field").
- FR-RCP-003 (dòng 811) và chương 8 (dòng 1505): `instructions?` tùy chọn.
- Code hiện tại: không có cột `Instructions`.
- Đề xuất: bỏ trường (chi tiết đã nằm ở `RecipeSteps`) hoặc để NULL được.

### C-19. Bước thực hiện (Cao)

- Chương 7 (dòng 1377): `Title varchar(200) NOT NULL`; chương 8 (dòng 1533): body `{ stepNumber, title, description, ... }`.
- FR-RCP-010 (dòng 965): body `{ description, TimerMinutes?, imageUrl? }`, `StepNumber` **tự sinh** bằng Max+1, xóa bước thì tự đánh số lại.
- Code hiện tại: không có `Title`; `stepNumber` tự sinh và tự đánh số lại.
- Đề xuất: bỏ `Title` hoặc cho tùy chọn; `stepNumber` luôn do server sinh.

### C-20. Nguyên liệu (Cao)

- Chương 7 (dòng 1391, 1392) và chương 8 (dòng 1543): `Quantity` và `Unit` được NULL; cột thứ tự là `OrderIndex`.
- FR-RCP-009 (mô tả và điều kiện tiên quyết, dòng 942–947): `Quantity > 0`, `Unit` không rỗng; cột thứ tự là `SortOrder` (cũng ở FR-RCP-002, dòng 788).
- Code hiện tại: `Quantity` và `Unit` bắt buộc; cột `SortOrder`.
- Đề xuất: bắt buộc và `SortOrder`. Nếu cần "vừa đủ" thì phải đổi schema, nên chốt trước khi khóa.

### C-21. Dinh dưỡng (Cao)

- Chương 7 (dòng 1360–1366): sáu trường `Calories`, `Protein`, `Carbohydrates`, `Fat`, `Fiber`, `Sodium` với tên cột `Nutrition_*`.
- FR-RCP-003 (dòng 812): `SetNutrition(calories, protein, carbs, fat)` (bốn trường).
- Code hiện tại: bốn trường (`Calories`, `ProteinGrams`, `CarbohydratesGrams`, `FatGrams`), không có tiền tố `Nutrition_`.
- Đề xuất: chốt số trường và tên cột trước khi khóa schema.

### C-22. API ảnh (Vừa)

- FR-RCP-008 (dòng 919, 927): `PATCH /recipes/{id}/images/{imageId}/primary`; upload trả 201 với `{ url, isPrimary }`.
- Chương 8 (dòng 1524, 1525): `PATCH /recipes/{id}/images/{imageId}` cập nhật `altText`, `isPrimary`, `orderIndex`; upload trả `{ imageId, originalUrl, altText, isPrimary }`.
- Code hiện tại: `PATCH .../primary`; upload trả `RecipeImageDto` đầy đủ (kèm `mediumUrl`, `thumbnailUrl`).
- Đề xuất: giữ `PATCH .../primary` và sửa chương 8; ghi rõ `RecipeImageDto` là phản hồi chuẩn.

### C-23. Xóa danh mục: cứng hay mềm (Vừa)

- CR-001 (dòng 44) và chương 8 (dòng 1499): soft delete.
- FR-CAT-005 (dòng 746, bước 5): "Xóa entity, lưu thay đổi".
- Code hiện tại: soft delete.
- Đề xuất: sửa bước 5 của FR-CAT-005.

### C-24. Danh mục: trường cập nhật và thứ tự (Vừa)

- FR-CAT-003 và FR-CAT-004 (dòng 701–727): chỉ `name` và `description`; `CategoryDto` gồm `{ id, name, slug, description, recipeCount }` (dòng 662); sắp theo tên.
- Chương 7 (dòng 1421) có `ImageUrl`, `OrderIndex` ("thứ tự hiển thị trên navigation"); chương 8 (dòng 1498) cho phép `imageUrl`, `orderIndex`.
- Code hiện tại: có `imageUrl` và `orderIndex`; danh sách sắp theo tên.
- Đề xuất: thêm `imageUrl`, `orderIndex` vào FR-CAT và nói rõ danh sách sắp theo tên hay theo `OrderIndex`.

### C-25. Kiểm tra magic bytes (Vừa)

- FR-RCP-008 (dòng 919): "đọc 4 bytes đầu" (JPEG `FF D8 FF`, PNG `89 50 4E 47`).
- Nhưng định dạng được phép có WebP (`RIFF....WEBP`, cần 12 byte) và AVIF (`ftyp` ở byte 4–11), nên 4 byte đầu không đủ để nhận biết.
- Đề xuất: đọc đủ phần đầu file (ít nhất 12 byte) và nêu chữ ký của cả bốn định dạng.

## 4. Cache, hạ tầng, kiến trúc

### C-26. Cơ chế cache và TTL (Cao)

Cùng một nhu cầu cache được mô tả bằng bốn cơ chế khác nhau:

| Nguồn | Nội dung |
| --- | --- |
| Dòng 649 và FR-CAT-001 (dòng 662) | `IMemoryCache`, TTL "1 giờ" ở phần mở đầu, "5 phút" trong cùng ô FR-CAT-001, rồi chú thích "TTL thống nhất là 60 phút" |
| FR-RCP-001, FR-RCP-002 (dòng 769, 788) | Output Cache của .NET: danh sách 15 phút, chi tiết 5 phút, xóa theo tag |
| Mục 6.3 (dòng 1262–1267) | `CachingBehavior` kiểm tra **Redis**, `CacheInvalidationBehavior` trong pipeline MediatR |
| NFR-PERF-003 (dòng 1069), NFR-SCALE-001 (dòng 1128), dòng 1241 | **Redis** phân tán; "không dùng `IMemoryCache`"; TTL chi tiết 5 phút, tìm kiếm 1 phút. FR-SRCH-001 (dòng 987) lại nói "không cache hoặc 5 phút" cho tìm kiếm |

- Code hiện tại: chỉ `IMemoryCache` cho danh sách danh mục (60 phút trượt); không có Output Cache hay cache Redis.
- Đề xuất: ghi một bảng "đối tượng, cơ chế, TTL, cách vô hiệu" duy nhất. Đề xuất thực tế cho đồ án: danh mục `IMemoryCache` 60 phút, recipe và tìm kiếm không cache hoặc Output Cache ngắn, Redis dành cho health check và mở rộng sau; sửa NFR-PERF-003 và NFR-SCALE-001 cho khớp.

### C-27. Redis lỗi: fallback hay readiness lỗi (Vừa)

- NFR-REL-002 (dòng 1111): Redis down thì "fallback database, không throw exception".
- FR-OBS-001 (dòng 1041) và chương 8 (dòng 1559): `/health/ready` trả 503 khi DB hoặc Redis lỗi để dừng nhận traffic.
- Hai quy tắc ngược nhau: nếu hệ thống chạy được không cần Redis thì readiness không nên lỗi vì Redis.
- Code hiện tại: readiness gồm cả database, Redis và MinIO nên 503 khi Redis hoặc MinIO lỗi (theo issue #22).
- Đề xuất: readiness chỉ phụ thuộc thành phần bắt buộc (database); Redis và MinIO báo trạng thái ở `/health` nhưng không làm readiness lỗi, hoặc ghi rõ ngược lại.

### C-28. Rate limiting (Vừa)

- NFR-SEC-003 (dòng 1087): middleware Rate Limiting của ASP.NET Core; mục 6.2 (dòng 1255) liệt kê `RateLimitingMiddleware`.
- Mục 6.1 (dòng 1239, 1241): Nginx làm "rate limiting basic"; Redis giữ "rate limiting counters".
- Đề xuất: chọn một nơi (ASP.NET Core). Khi chạy sau Nginx phải dùng IP thật từ `X-Forwarded-For` (đã làm ở W08).

### C-29. `ApplicationUser` trong Domain (Cao)

- Mục 6.2 (dòng 1251): lớp Domain chứa entity `ApplicationUser`.
- CONS-001 (dòng 447), CR-001 (dòng 48) và NFR-MAINT-004 (dòng 1122): Domain chỉ dùng BCL, không tham chiếu package khác.
- `ApplicationUser` kế thừa `IdentityUser` (gói ASP.NET Core Identity) nên không thể ở Domain.
- Code hiện tại: `ApplicationUser` nằm ở Infrastructure.
- Đề xuất: bỏ `ApplicationUser` khỏi danh sách entity Domain (và `EmailAddress` nếu dùng Identity), ghi là entity của Infrastructure.

### C-30. `LoggingBehavior` và dữ liệu nhạy cảm (Vừa)

- Mục 6.3 (dòng 1264): `LoggingBehavior` ghi "request type, **parameters**, elapsed time".
- NFR-SEC-004 (dòng 1088), NFR-SEC-007 (dòng 1094) và CONS-010 (dòng 460): không để lộ secret; request như đăng ký hoặc đăng nhập mang mật khẩu và token.
- Code hiện tại: chỉ ghi tên request, thời gian và loại lỗi; có test xác nhận không rò rỉ.
- Đề xuất: sửa mục 6.3 thành "không ghi tham số".

### C-31. Trang recipe ISR và recipe Draft (Vừa)

- Mục 5.1 (dòng 1154): `/recipes/[slug]` là ISR (`revalidate=300`), không yêu cầu đăng nhập.
- FR-RCP-002: recipe Draft và Archived chỉ chủ sở hữu hoặc Admin được xem, tức cần xác thực theo người dùng.
- Trang ISR được sinh tĩnh và dùng chung, không thể kiểm tra quyền theo người; nội dung Draft sẽ bị lộ hoặc không xem được.
- Đề xuất: ISR cho recipe Published công khai; recipe Draft/Archived đi qua trang render động theo token (hoặc từ dashboard).

### C-32. Phiên bản Node.js (Thấp)

- Dòng 40 (CR-001): Node.js 22 trong Docker và CI.
- Dòng 398, 419: yêu cầu tối thiểu Node.js 20 LTS.
- Đề xuất: nêu Node.js 22 là phiên bản chuẩn, 20 chỉ ở mức tương thích.

### C-33. Tailwind và "không dùng CSS framework override" (Thấp)

- Dòng 1100 (NFR-USE-001): "Framework: Tailwind CSS... Không sử dụng CSS framework override"; dòng 1238 liệt kê Tailwind trong ngăn xếp.
- Câu "không dùng CSS framework override" mâu thuẫn tự thân với việc dùng một CSS framework.
- Code hiện tại: không dùng Tailwind (CSS thuần với token thiết kế).
- Đề xuất: nếu giữ CSS thuần thì bỏ Tailwind khỏi dòng 1100 và 1238; nếu giữ Tailwind thì bỏ câu "không dùng CSS framework override".

## 5. Đề xuất cách xử lý

1. Họp nhóm xử lý các mục **Cao** trước (C-01, C-03, C-05, C-06, C-07, C-13, C-14, C-17 đến C-21, C-26, C-29), vì chúng ảnh hưởng schema và API sắp khóa.
2. Ghi quyết định thành CR-002 trong bảng lịch sử thay đổi của SRS (phiên bản 1.0.2), kèm danh sách dòng cần sửa ở trên.
3. Sau khi chốt, tách thành việc theo người phụ trách: Bo (C-03, C-05 đến C-12), Trường (C-13 đến C-22, C-25), Kiên (C-14 phần tìm kiếm), Hiếu (C-02, C-23, C-24, C-26 đến C-33).
4. Những mục mà code đã theo một bên (C-01, C-02, C-05, C-07, C-08, C-23, C-29, C-30) chỉ cần sửa tài liệu.
