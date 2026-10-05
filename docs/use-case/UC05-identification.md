# UC05 – Đối Chiếu & Nhận Dạng AI (Identification & Matching)

## 1. Thông tin chung
- **Mã Use Case:** UC05
- **Tên Use Case:** Đối chiếu & Nhận dạng AI (kết hợp UC17, UC18)
- **Actor chính:** Hệ thống AI (tự động)
- **Actor thụ hưởng:** Cán bộ xác minh, Người dùng

## 2. Quy trình xử lý đối chiếu
1. **Kích hoạt:** Kích hoạt tự động khi có hồ sơ người mất tích mới hoặc hồ sơ thi thể mới được tạo, hoặc chạy quét định kỳ.
2. **Trích xuất đặc trưng (Feature Extraction):**
   - Phân tích ảnh khuôn mặt $\to$ trích xuất vector embedding 512 chiều.
   - Nhận diện OCR biển số xe hoặc tài liệu vật phẩm.
3. **Tính toán điểm matching (5 tiêu chí):**
   $$\text{Điểm tổng} = 0.30 \times S_{\text{cá\_nhân}} + 0.25 \times S_{\text{hình\_ảnh}} + 0.20 \times S_{\text{đặc\_điểm}} + 0.15 \times S_{\text{địa\_điểm}} + 0.10 \times S_{\text{thời\_gian}}$$
4. **Phân loại ngưỡng kết quả:**
   - **> 50%:** Xếp loại *Có khả năng*.
   - **> 75%:** Xếp loại *Cần xác minh*.
   - **> 90%:** Xếp loại *Ưu tiên xác minh khẩn*.
5. **Lưu trữ kết quả:** Tạo bản ghi vào bảng `KetQuaAI` và `AI_Matching`.
6. **Kích hoạt thông báo:** Nếu điểm $\ge 50\%$, hệ thống kích hoạt thông báo gửi cán bộ và thông báo mã hồ sơ gợi ý tới người dùng (không kèm ảnh nhạy cảm).
