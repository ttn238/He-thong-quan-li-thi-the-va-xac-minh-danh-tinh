# Thư Mục Kiểm Thử (Testing Suite)

## 1. Cấu trúc
```
tests/
├── backend/            # Unit Tests cho phân hệ .NET Web API (xUnit / NUnit)
│   ├── AuthTests/              # Kiểm thử xác thực, JWT, phân quyền
│   ├── BodyTests/              # Kiểm thử tiếp nhận, cập nhật hồ sơ thi thể
│   ├── MissingPersonTests/     # Kiểm thử khai báo, quản lý người mất tích
│   └── IdentificationTests/    # Kiểm thử đối chiếu, thẩm tra danh tính
├── frontend/           # Kiểm thử UI và logic component (Vitest / React Testing Library)
└── integration/        # Kiểm thử tích hợp toàn diện hệ thống (End-to-End)
```

## 2. Tiêu chuẩn kiểm thử
- Kiểm thử đơn vị (Unit Tests) cho tất cả Service logic trọng yếu.
- Đảm bảo logic tính điểm đối chiếu AI và quy trình phê duyệt xác minh danh tính được kiểm thử chặt chẽ với các trường hợp biên.
