# storage-seed

Thư mục chứa ảnh mẫu để seed/demo cho project Death Management.

## Cấu trúc

```text
storage-seed/
├── secure/
│   ├── guid_body_exam_01.jpg
│   └── guid_body_face_01.jpg
├── public/
│   └── guid_missing_portrait_01.jpg
└── README.md
```

## Cách sử dụng

1. Tạo thư mục lưu trữ thực tế theo cấu hình `Storage:RootPath` (ví dụ: `D:\\DeathManagementStorage`).
2. Copy nội dung thư mục `secure` vào `<RootPath>\\secure`.
3. Copy nội dung thư mục `public` vào `<RootPath>\\public`.
4. Đảm bảo `DuongDan` trong dữ liệu seed trỏ tới:
   - `/secure/guid_body_exam_01.jpg`
   - `/secure/guid_body_face_01.jpg`
   - `/public/guid_missing_portrait_01.jpg`

**Lưu ý:** Các file JPG trong gói này chỉ là ảnh placeholder minh họa, không phải ảnh người thật và không dùng làm dữ liệu AI đánh giá/nhận diện.
