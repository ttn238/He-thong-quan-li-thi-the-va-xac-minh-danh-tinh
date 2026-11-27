# Báo cáo kiểm thử phân quyền API - DeathManagement.API

Phạm vi: `backend/src/DeathManagement.API/Controllers/`, quy tắc tại `Security/AuthorizationPolicies.cs`, lớp thực thi `Security/AccessControl.cs`, `Security/AuthorizationSaveChangesInterceptor.cs`.

## 0. Giới hạn của lần kiểm thử này (đọc trước)

- Project hiện là ứng dụng WinForms (`OutputType=WinExe`, `Program.cs` mở `FrmLogin`). Thư mục `Controllers/**` và `Middleware/**` bị loại khỏi biên dịch trong `.csproj`, và không có `AddAuthentication` / `UseAuthorization` / `MapControllers`. Vì vậy **không có HTTP server nào để gửi request thật**.
- Môi trường kiểm thử không có .NET SDK và không có SQL Server, nên cũng không build/chạy được. Cột "Kết quả thực tế" bên dưới là kết quả **phân tích tĩnh mã nguồn** (đọc luồng xử lý từng API), không phải kết quả chạy request.
- Các test case ở mục 3 viết sẵn để chạy lại (Postman/curl) ngay khi bản Web API được dựng.
- 8/11 controller (`Body`, `Handover`, `Identification`, `MissingPerson`, `Notification`, `Object`, `Report`, `Verification`) là **file rỗng**, chưa có API nào để kiểm thử. Mục 4 ghi quyền kỳ vọng theo ma trận để làm đặc tả.

## 1. Quy tắc phân quyền (nguồn: AuthorizationPolicies.cs)

| Vai trò | Mã | Ghi chú |
|---|---|---|
| Quản trị hệ thống | ADMIN | ACCOUNT, PERMISSION, CATALOG, STORAGE, BODY(V,D), REPORT, AUDIT... |
| Công an / cán bộ xác minh | CONG_AN | Tin báo, hồ sơ, đối soát, xác minh, bàn giao |
| Công an duyệt | CONG_AN_DUYET | Toàn bộ quyền CONG_AN + VERIFICATION:APPROVE |
| Nhân viên nhà xác | NHA_XAC | BODY(PARTIAL), STORAGE, HANDOVER |
| Người dân | NGUOI_DAN | Chỉ INCIDENT, FILE, MISSING_PERSON phạm vi OWN |

Mặc định từ chối. Token JWT mang claim `role` và `perm = RESOURCE:ACTION:SCOPE`.

## 2. Danh sách API đã triển khai và quyền yêu cầu (15 API)

Ký hiệu: A = ADMIN, Auth = mọi người dùng đã đăng nhập. Quyền trong ngoặc là quyền tương ứng trong ma trận (do `[Authorize(Roles=...)]` trong controller chỉ kiểm tra tên vai trò).

| ID | Method + Đường dẫn | Quyền yêu cầu | Mã mong đợi: không có token | Token sai / hết hạn | Vai trò thấp (CONG_AN, CONG_AN_DUYET, NHA_XAC, NGUOI_DAN) | ADMIN |
|---|---|---|---|---|---|---|
| API-01 | POST /api/v1/auth/login | Công khai | 400 nếu thiếu trường, 401 sai thông tin, 423 khóa | n/a | n/a | n/a |
| API-02 | GET /api/v1/users | A (ACCOUNT:VIEW) | 401 | 401 | 403 | 200 |
| API-03 | GET /api/v1/users/me | Auth | 401 | 401 | 200 (chỉ dữ liệu của chính mình) | 200 |
| API-04 | PUT /api/v1/users/me | Auth | 401 | 401 | 200 (chỉ HoTen, Email, SoDienThoai) | 200 |
| API-05 | GET /api/v1/users/{id} | A (ACCOUNT:VIEW) | 401 | 401 | 403 | 200 / 404 |
| API-06 | POST /api/v1/users | A (ACCOUNT:CREATE) | 401 | 401 | 403 | 201 |
| API-07 | PUT /api/v1/users/{id} | A (ACCOUNT:UPDATE; đổi vai trò thêm PERMISSION:UPDATE) | 401 | 401 | 403 | 200 |
| API-08 | PUT /api/v1/users/{id}/status | A (ACCOUNT:UPDATE) | 401 | 401 | 403 | 200 |
| API-09 | PUT /api/v1/users/{id}/reset-password | A (ACCOUNT:UPDATE) | 401 | 401 | 403 | 200 |
| API-10 | DELETE /api/v1/users/{id} | A (ACCOUNT:UPDATE, xóa mềm) | 401 | 401 | 403 | 200 |
| API-11 | GET /api/v1/roles | A (PERMISSION:VIEW) | 401 | 401 | 403 | 200 |
| API-12 | GET /api/v1/roles/{id} | A (PERMISSION:VIEW) | 401 | 401 | 403 | 200 / 404 |
| API-13 | GET /api/v1/roles/permissions | A (PERMISSION:VIEW) | 401 | 401 | 403 | 200 |
| API-14 | PUT /api/v1/roles/assign | A (PERMISSION:UPDATE) | 401 | 401 | 403 | 200 |
| API-15 | PUT /api/v1/roles/{id}/permissions | A (PERMISSION:UPDATE) | 401 | 401 | 403 | 200 |

## 3. Bộ test case (chạy lại khi có Web API)

Với mỗi API-02 đến API-15 chạy các case sau (Base URL ví dụ `https://localhost:5001`):

| TC | Tình huống | Cách gửi | Mong đợi |
|---|---|---|---|
| TC-01 | Không xác thực | Không có header `Authorization` | 401, body không chứa dữ liệu nghiệp vụ |
| TC-02 | Token sai định dạng | `Authorization: Bearer abc.def.ghi` | 401 |
| TC-03 | Token hết hạn | Token có `exp` trong quá khứ (hơn 30 giây do ClockSkew) | 401 |
| TC-04 | Token ký sai khóa | Token ký bằng khóa khác | 401 |
| TC-05 | Token bị sửa claim | Đổi `role` thành `ADMIN`, giữ nguyên chữ ký | 401 |
| TC-06 | Token `alg=none` | Bỏ chữ ký | 401 |
| TC-07 | Leo thang dọc | Token NGUOI_DAN / NHA_XAC / CONG_AN / CONG_AN_DUYET gọi API chỉ dành cho ADMIN | 403, không lộ dữ liệu |
| TC-08 | Truy cập ngang | Token NGUOI_DAN A gọi `GET /users/{id}` của B | 403 |
| TC-09 | Nhồi trường | `PUT /users/me` kèm `MaVaiTro=1`, `TrangThai`, `MatKhauHash` | 200 nhưng bỏ qua các trường thừa; vai trò không đổi |
| TC-10 | Tự leo thang | ADMIN `PUT /users/{idCuaMinh}` với `MaVaiTro` khác | Bị chặn (400/403) |
| TC-11 | Tự khóa / tự xóa | ADMIN gọi `/status` (Khoa) hoặc `DELETE` trên chính mình | 400 |
| TC-12 | Hạ quyền admin duy nhất | `PUT /roles/assign` đổi admin hoạt động duy nhất sang vai trò khác | 400 |
| TC-13 | Token cũ sau khi khóa | Khóa tài khoản rồi dùng lại token đã cấp | 401 (xem BUG-08) |
| TC-14 | Đăng nhập | Sai mật khẩu 5 lần, tài khoản khóa, tài khoản không tồn tại | Thông báo chung, không phân biệt tài khoản tồn tại hay không |
| TC-15 | Lỗi không lộ nội bộ | Gửi body sai để gây exception | Không có stack trace, tên bảng, chuỗi kết nối |

### Kết quả thực tế (phân tích tĩnh) cho từng API

"Trước sửa" là mã gốc trong file zip. "Sau sửa" là mã đã vá kèm báo cáo này. Ghi nhận: chưa chạy request thật.

| API | Trước sửa | Sau sửa |
|---|---|---|
| API-01 login | Không compile (BUG-10). Logic: thông báo sai chung, tài khoản khóa trả 423 | Compile được ở mức khai báo; hành vi giữ nguyên |
| API-02, 05, 06 | Chặn ở attribute (nếu pipeline cấu hình); service không kiểm tra; không có exception middleware nên 401/403 từ lớp dưới thành 500 | Service kiểm tra `ACCOUNT:*`; middleware trả 401/403 |
| API-03 | Hoạt động, chỉ đọc theo claim `sub` | Thêm `RequireSelfOr` |
| API-04 | Có thể bị 403 nhầm do interceptor (BUG-06); chống nhồi trường tốt vì DTO không có `MaVaiTro` | Đã vá BUG-06 (cần xác nhận khi chạy) |
| API-07 | Kiểm tra đổi vai trò nằm sau `return` nên không chạy (BUG-05); phụ thuộc interceptor | Kiểm tra chạy trước khi ghi |
| API-08, 09, 10 | Service không kiểm tra; interceptor cho phép ghi `TrangThai`, `MatKhauHash` khi chưa đăng nhập (BUG-02) | Service bắt buộc `ACCOUNT:UPDATE` |
| API-11, 12, 13, 14, 15 | Chỉ có attribute; service và repository không kiểm tra; bảng `VaiTro_Quyen` không có interceptor (BUG-03) | Service bắt buộc `PERMISSION:VIEW/UPDATE` |

## 4. Quyền kỳ vọng cho controller chưa triển khai (đặc tả theo ma trận)

| Controller | Vai trò được phép (hành động) | Vai trò phải nhận 403 |
|---|---|---|
| Body | ADMIN (V, D); CONG_AN, CONG_AN_DUYET (V, C, U); NHA_XAC (V, U, phạm vi PARTIAL) | NGUOI_DAN |
| MissingPerson | CONG_AN, CONG_AN_DUYET (V, C, U, CONFIRM); NGUOI_DAN (V, C, U, chỉ OWN) | ADMIN, NHA_XAC |
| Object | CONG_AN, CONG_AN_DUYET, NHA_XAC (V, C, U) | ADMIN, NGUOI_DAN |
| Identification (MATCHING, AI_RESULT) | ADMIN (V, U); CONG_AN, CONG_AN_DUYET (V, C, CONFIRM) | NHA_XAC, NGUOI_DAN |
| Verification | ADMIN (V); CONG_AN, CONG_AN_DUYET (V, C, U, CONFIRM); APPROVE chỉ CONG_AN_DUYET | NHA_XAC, NGUOI_DAN; CONG_AN gọi APPROVE |
| Handover | CONG_AN, CONG_AN_DUYET (V, C, U, CONFIRM); NHA_XAC (V, U, CONFIRM, HANDOVER) | ADMIN, NGUOI_DAN |
| Report | ADMIN, CONG_AN, CONG_AN_DUYET (V, EXPORT); NHA_XAC (PARTIAL) | NGUOI_DAN |
| Notification | Chưa có tài nguyên trong ma trận. Cần quyết định: mọi người dùng đăng nhập, chỉ thấy thông báo của mình | Ẩn danh |

## 5. Danh sách lỗi

Mức độ: Critical > High > Medium > Low. Trạng thái: Đã xử lý (có trong file bàn giao) / Mở (cần quyết định hoặc cấu hình).

| Bug | Mức | Mô tả và tình huống | Trạng thái |
|---|---|---|---|
| BUG-01 | Critical | Không có pipeline Web API: `Program.cs` là WinForms, Controllers/Middleware bị `Compile Remove`, không cấu hình JWT Bearer / `UseAuthorization`. `[Authorize]` không có hiệu lực. Ngoài ra `AccessControl.PrincipalProvider` mặc định đọc `Session` tĩnh dùng chung, không an toàn khi nhiều request đồng thời; bản Web API phải gán `PrincipalProvider` theo `HttpContext.User`. | Mở (quyết định kiến trúc: tách project ASP.NET Core) |
| BUG-02 | High | `UserService.ChangeStatusAsync` / `ResetPasswordAsync` không kiểm tra quyền. Interceptor cho phép khi **chưa đăng nhập** ghi `MatKhauHash`, `TrangThai`, `KhoaDenThoiDiem`, `SoLanDangNhapSai` (phục vụ đăng nhập). Nếu lớp controller không chặn (BUG-01), người ẩn danh đặt lại mật khẩu hoặc mở khóa tài khoản bất kỳ. | Đã xử lý: `AccessControl.Require` trong mọi hàm của UserService |
| BUG-03 | High | `RoleService` (đọc, gán vai trò, sửa ma trận quyền) không kiểm tra quyền; interceptor không bao phủ bảng `VaiTro_Quyen`. Chỉ có attribute controller bảo vệ. | Đã xử lý: `PERMISSION:VIEW/UPDATE` trong RoleService |
| BUG-04 | Medium | `ExceptionMiddleware.cs` rỗng: `UnauthenticatedException` / `ForbiddenException` bị trả 500 thay vì 401 / 403; không có xử lý lỗi chung nên có nguy cơ lộ chi tiết lỗi. | Đã xử lý: middleware mới (cần đăng ký khi có Web API) |
| BUG-05 | Medium | `UserService.UpdateAsync`: khối kiểm tra đổi vai trò / tự đổi vai trò nằm sau `return` (mã chết). `MapToDto` khai báo trùng biến `tenVaiTro` (lỗi biên dịch CS0128). | Đã xử lý |
| BUG-06 | Medium | `UserRepository.UpdateAsync` gọi `_db.Users.Update()` trên thực thể đã theo dõi, EF đánh dấu mọi trường là đã sửa; interceptor thấy `MaVaiTro`, `MatKhauHash`... bị đổi nên: người dùng thường nhận 403 khi `PUT /users/me`, ADMIN bị chặn khi tự sửa hồ sơ. Nhận định từ đọc mã, cần xác nhận lúc chạy. | Đã xử lý (chỉ gọi `Update` khi thực thể detached) |
| BUG-07 | Medium | `POST/PUT /users` không kiểm tra `MaVaiTro` hợp lệ (lỗi khóa ngoại thành 500); `PUT /roles/{id}/permissions` không kiểm tra `MaQuyen` tồn tại / trùng. | Một phần: User đã xử lý; `MaQuyen` còn mở |
| BUG-08 | Medium | JWT không thu hồi được: tài khoản bị khóa/xóa hoặc đổi vai trò vẫn dùng token cũ tới 480 phút, và quyền được nhúng sẵn trong token. | Mở (rút ngắn hạn token, hoặc kiểm tra `TrangThai` / phiên bản quyền mỗi request) |
| BUG-09 | Medium | `appsettings.json` không có mục `Jwt` nên khóa ký là ngẫu nhiên mỗi lần khởi động: token mất hiệu lực sau restart, không chạy được nhiều instance. Chuỗi kết nối chứa mật khẩu `sa` dạng văn bản thường. | Mở (dùng User Secrets / biến môi trường, đổi mật khẩu `sa`) |
| BUG-10 | Low | `AuthController` giải cấu trúc 2 giá trị trong khi `IAuthService` trả 3 giá trị (lỗi biên dịch). | Đã xử lý. Có chủ đích giữ `TamKhoa` trả 401 chung để không lộ tài khoản tồn tại hoặc đang bị khóa tạm |
| BUG-11 | Low | `ResolveRoleCode` khớp chuỗi con ("quan tri", "cong an", "duyet"): vai trò tùy biến đặt tên chứa cụm này sẽ nhận quyền tương ứng. Controller dùng chuỗi vai trò thay vì policy `RequireAdmin`, và `AdminRolesCsv` chứa tên tiếng Việt không bao giờ xuất hiện trong claim. | Mở |
| BUG-12 | Low (thiết kế) | Sửa ma trận ở `PUT /roles/{id}/permissions` ghi vào CSDL nhưng quyền thực thi lấy từ ma trận cố định trong mã (`AuthorizationPolicies.Matrix`), nên chỉnh sửa trên giao diện không có tác dụng thực tế. | Mở (cần quyết định nguồn quyền duy nhất) |
| BUG-13 | Low | Đăng nhập sai 5 lần khóa tài khoản 15 phút, không giới hạn theo IP: kẻ tấn công có thể khóa tài khoản bất kỳ (từ chối dịch vụ). | Mở |

## 6. Gợi ý đăng ký khi dựng Web API

```csharp
builder.Services.AddAuthentication("Bearer").AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = /* cùng tham số như JwtTokenService.Validate, Jwt:Key lấy từ User Secrets */;
});
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Sau đó gán `AccessControl.PrincipalProvider` lấy từ `IHttpContextAccessor.HttpContext?.User` và chạy lại mục 3.
