# Tài Liệu Thiết Kế Hệ Thống (System Documentation)

## 1. Mục lục tài liệu
```
docs/
├── requirements/               # Đặc tả yêu cầu hệ thống
│   ├── functional-requirements.md
│   └── non-functional-requirements.md
├── use-case/                   # Đặc tả chi tiết từng ca sử dụng (UC01 -> UC19)
│   ├── UC01-login.md
│   ├── UC02-manage-user.md
│   ├── UC03-report-missing-person.md
│   ├── UC04-manage-body.md
│   ├── UC05-identification.md
│   ├── UC06-verification.md
│   └── UC07-handover.md
├── activity/                   # Sơ đồ hoạt động (Activity Diagrams)
├── sequence/                   # Sơ đồ tuần tự (Sequence Diagrams)
├── class-diagram/              # Sơ đồ lớp (Class Diagrams)
├── database/                   # Sơ đồ quan hệ thực thể (ERD) & Từ điển dữ liệu
├── architecture/               # Kiến trúc hệ thống, kiến trúc phân tầng, kiến trúc bảo mật & AI
├── api/                        # Tài liệu API Endpoints
│   └── api-documentation.md
└── reports/                    # Báo cáo và slide thuyết trình đề tài
```

## 2. Tiêu chuẩn tuân thủ
- Mọi mô hình thiết kế và luồng xử lý phải tuân thủ nghiêm ngặt nguyên tắc: **AI hỗ trợ đề xuất, cán bộ có thẩm quyền ra quyết định chính thức**.
- Tuyệt đối bảo mật dữ liệu nhạy cảm (hình ảnh thi thể, thông tin khám nghiệm).
