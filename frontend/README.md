# Frontend - Ứng Dụng Quản Lý Thi Thể & Xác Minh Danh Tính

## 1. Tổng quan
Phân hệ Frontend được xây dựng bằng **React + TypeScript + Vite**, cung cấp giao diện trực quan, thân thiện cho cả 3 đối tượng người dùng:
1. **Admin Layout:** Dành cho quản trị viên theo dõi Dashboard, phân quyền, cấu hình hệ thống.
2. **Officer Layout:** Dành cho cán bộ chuyên trách tiếp nhận hồ sơ, đối chiếu kết quả AI, thẩm tra minh chứng và phê duyệt.
3. **User / Main Layout:** Dành cho người dân đăng ký tìm người mất tích, tra cứu, gửi yêu cầu xác nhận và theo dõi tiến trình.

## 2. Cấu trúc thư mục
```
frontend/
├── public/                 # Assets tĩnh (icon, hình ảnh công khai)
├── src/
│   ├── assets/             # Hình ảnh, icons nội bộ
│   ├── components/         # Components tái sử dụng (forms, tables, modals, upload, ai-result)
│   ├── layouts/            # Layout theo vai trò: AdminLayout, OfficerLayout, MainLayout
│   ├── pages/              # Trang theo từng màn hình nghiệp vụ
│   ├── features/           # Slices / Modules nghiệp vụ độc lập
│   ├── services/           # Gọi RESTful API tới Backend
│   ├── hooks/              # Custom React Hooks (useAuth, usePermission...)
│   ├── routes/             # Cấu hình định tuyến và Protected Routes
│   ├── types/              # Định nghĩa kiểu dữ liệu TypeScript
│   └── utils/              # Tiện ích format ngày tháng, trạng thái, validation
├── package.json
└── vite.config.ts
```

## 3. Các màn hình & Luồng giao diện chính
- **Xác thực:** Đăng nhập, Đăng ký (User), Đổi mật khẩu.
- **Khai báo & Quản lý:** Khai báo thân nhân, Lập hồ sơ người mất tích, Tiếp nhận hồ sơ thi thể (Officer).
- **Đối chiếu & Kết quả AI (`components/ai-result`):** Hiển thị trực quan điểm tương đồng (%), phân tích radar chart theo 5 tiêu chí (Cá nhân, Đặc điểm, Ảnh, Địa điểm, Thời gian).
- **Quy trình xác nhận:** Màn hình gửi bằng chứng quan hệ $\to$ Giao diện cán bộ thẩm tra $\to$ Phê duyệt / Từ chối $\to$ Theo dõi tiến trình thời gian thực.
