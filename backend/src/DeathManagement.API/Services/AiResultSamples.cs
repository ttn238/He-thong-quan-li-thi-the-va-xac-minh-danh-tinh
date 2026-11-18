using DeathManagement.API.DTOs.Ai;

namespace DeathManagement.API.Services;

public enum AiMau { DangXuLy, CoKetQua, KhongCoKetQua, Loi504, KhuyetThieu, DangThuLai }

public static class AiResultSamples
{
    public static AiResultDto Tao(AiMau mau)
    {
        var d = new AiResultDto
        {
            RequestId = "AI-REQ-20261014-089",
            LinkedRecordId = "TB-20261014-0412",
            SubmittedAt = "2026-10-14T14:18:02+07:00",
            Engine = "Vision-DVI v2.8 (Full Object & OCR Hybrid)",
            ImageHash = "4c9a89d7b43a2ef86715f33e61c3b1297d0fa04812f84cb71fb4901baef4821a",
            Image = new AiImageDto { FileName = "HIENTRUONG_XUANLA_01.JPG", SizeBytes = 3842L * 1024, Format = "JPEG", Width = 1920, Height = 1080 }
        };

        switch (mau)
        {
            case AiMau.DangXuLy:
                d.Status = "processing"; d.Progress = 78;
                return d;

            case AiMau.DangThuLai:
                d.Status = "retrying"; d.Attempt = 2;
                return d;

            case AiMau.Loi504:
                d.Status = "failed";
                d.Error = new AiErrorDto
                {
                    Code = "ERR_AI_UPSTREAM_UNRESPONSIVE_504",
                    HttpStatus = 504,
                    Message = "Không thể nhận phản hồi từ cụm máy chủ AI phân tích hình ảnh (GPU Node DVI-AI-SERVER-04). Phiên kết nối quá thời gian quy định (Timeout 30.000ms).",
                    TraceId = "trc-883a992bc01f89aa0182",
                    OccurredAt = "2026-10-14T14:18:32.412+07:00",
                    Endpoint = "https://ai-gateway.dvi.bca.gov.vn/api/v2/vision/segmentation"
                };
                return d;

            case AiMau.KhongCoKetQua:
                d.Status = "completed"; d.CompletedAt = "2026-10-14T14:18:04+07:00"; d.DurationMs = 1240;
                return d;
        }

        // CoKetQua và KhuyetThieu dùng chung dữ liệu đầy đủ
        d.Status = "completed"; d.Progress = 100;
        d.CompletedAt = "2026-10-14T14:19:32+07:00"; d.DurationMs = 1240;
        d.Ocr = new()
        {
            new AiOcrItem { Text = "ĐƯỜNG XUÂN LA - QUẬN TÂY HỒ", Kind = "street_sign", Confidence = 0.984, Bbox = new[] { 180, 120, 420, 160 } },
            new AiOcrItem { Text = "29B1-884.21", Kind = "license_plate", Confidence = 0.962, Bbox = new[] { 640, 690, 190, 70 }, Note = "Khớp định dạng tiêu chuẩn Cục CSGT" },
            new AiOcrItem { Text = "QUÁN CÀ PHÊ... [KÝ TỰ BỊ CHE KHUẤT]", Kind = "other", Confidence = 0.721, Note = "Cần cán bộ thẩm tra thủ công" }
        };
        d.Objects = new()
        {
            new AiObjectItem { Label = "Phương tiện: Xe gắn máy", Confidence = 0.982, Bbox = new[] { 412, 120, 230, 65 }, Tag = "Khớp đặc điểm AM-04",
                Description = "Honda Wave Alpha màu xanh ngọc. Nứt vỡ yếm xe bên trái, tư thế đổ nghiêng sang phải tiếp giáp mặt đường nhựa." },
            new AiObjectItem { Label = "Biển báo hiệu giao thông", Confidence = 0.957, Bbox = new[] { 180, 120, 420, 160 }, Tag = "Tọa độ cố định",
                Description = "Biển chỉ dẫn phân luồng làn xe và biển tên phố Xuân La. Biển cắm cố định trên vỉa hè phía Đông." },
            new AiObjectItem { Label = "Dấu vết cày xước mặt đường", Confidence = 0.885, Tag = "Quan trọng",
                Description = "Vệt trượt kim loại kéo dài xấp xỉ 12 mét theo hướng Tây sang Đông, phương thẳng song song với vạch kẻ tim đường." },
            new AiObjectItem { Label = "Tư trang / Đồ vật cá nhân", Confidence = 0.91, Bbox = new[] { 650, 140, 150, 180 }, Tag = "Thu giữ mẫu vật",
                Description = "Mũ bảo hiểm nửa đầu màu đỏ sẫm, quai cài bị đứt, nằm cách cụm phương tiện 3.4 mét về hướng Tây Bắc." }
        };
        d.Locations = new()
        {
            new AiLocationItem { Rank = 1, Name = "Ngã tư đường Xuân La giao cắt Võ Chí Công", Confidence = 0.968,
                AdminArea = "P. Xuân La, Q. Tây Hồ, TP. Hà Nội", Lat = 21.054312, Lng = 105.801245, Basis = "OCR biển báo + Trạm BTS VNPT ID: HN-THO-048" },
            new AiLocationItem { Rank = 2, Name = "Đoạn dốc đê Bưởi nối dài Lạc Long Quân", Confidence = 0.821,
                AdminArea = "Phường Nghĩa Đô, Quận Cầu Giấy, Hà Nội", Lat = 21.045890, Lng = 105.800112 }
        };
        if (mau == AiMau.KhuyetThieu)
        {
            d.MissingFields = new()
            {
                new AiMissingField { Field = "Biển số phương tiện (OCR LP)", Status = "done", Value = "29B1-884.21 (96.2%)", Recommendation = "Đã đối khớp CSDL Cục CSGT" },
                new AiMissingField { Field = "Số khung / Số máy phương tiện", Status = "missing", Value = "[Chưa chụp cự ly gần lốc máy]", Recommendation = "Bắt buộc giám định viên kiểm tra trực tiếp" },
                new AiMissingField { Field = "Biển tên đường / Tọa độ WGS-84", Status = "done", Value = "21.054312, 105.801245 (Xuân La)", Recommendation = "Khớp trạm BTS di động" },
                new AiMissingField { Field = "Đặc điểm thương tích / Dấu vết sinh học", Status = "out_of_scope", Value = "[Không phát hiện người trong khung hình]", Recommendation = "Xem hồ sơ tử thi PM liên kết" }
            };
        }
        return d;
    }
}