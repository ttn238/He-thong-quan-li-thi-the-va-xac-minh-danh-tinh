# Backend - DeathManagement.API

## 1. Tổng quan
Phân hệ Backend được xây dựng trên nền tảng **.NET Web API**, áp dụng mô hình kiến trúc phân tầng chuẩn (**Repository - Service Pattern**), đảm nhiệm việc xử lý logic nghiệp vụ, quản lý dữ liệu, phân quyền truy cập, xác thực và tích hợp với microservice AI.

## 2. Cấu trúc thư mục
```
backend/
├── DeathManagement.sln
└── src/
    └── DeathManagement.API/
        ├── Controllers/            # API Endpoints tiếp nhận request
        ├── Services/               # Xử lý business logic
        │   ├── Interfaces/
        ├── Repositories/           # Tương tác cơ sở dữ liệu qua EF Core
        │   ├── Interfaces/
        ├── Entities/               # Data models / Database entities
        ├── DTOs/                   # Data Transfer Objects cho Request / Response
        ├── Data/                   # DbContext và Seeding
        ├── Configurations/         # Fluent API Entity configurations
        ├── Security/               # JWT, Password Hasher, RBAC Policies
        ├── Middleware/             # Exception Handling, Request Logging
        └── Helpers/                # Validation, File, Image processing helpers
```

## 3. Các thực thể chính (Entities)
- `User`, `Role`: Tài khoản và phân quyền người dùng (Admin, Cán bộ xác minh, User).
- `MissingPerson`, `MissingPersonImage`: Hồ sơ người mất tích và hình ảnh đính kèm.
- `Body`, `BodyImage`: Hồ sơ thi thể chưa xác định và hình ảnh lưu trữ an toàn.
- `ObjectItem`: Đồ vật, tư trang, phương tiện thu thập tại hiện trường.
- `Identification`, `AIResult`: Quá trình đối chiếu và kết quả phân tích AI (Face, OCR, Matching score).
- `Verification`: Hồ sơ thẩm tra, phê duyệt xác nhận hoặc từ chối danh tính.
- `Handover`: Hồ sơ và biên bản bàn giao thi thể cho thân nhân.
- `Notification`: Quản lý thông báo người dùng và cán bộ.
- `AuditLog`: Nhật ký theo dõi hoạt động toàn hệ thống.

## 4. Nguyên tắc bảo mật & phân quyền
- **JWT Authentication:** Xác thực người dùng qua token, phân quyền chi tiết theo Role.
- **Dữ liệu nhạy cảm:** Hình ảnh thi thể (`BodyImage`) được bảo vệ nghiêm ngặt, chỉ cán bộ có thẩm quyền mới được truy cập tệp gốc.
- **Audit Logging:** Mọi hành vi cập nhật hồ sơ, kết luận xác minh và phê duyệt bàn giao đều được ghi vết tự động.
