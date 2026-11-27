using DeathManagement.API.DTOs.Image;

namespace DeathManagement.API.Services.Interfaces;

public interface IImageService
{
    Task<List<ImageDto>> LayDanhSachHinhAnhAsync(ImageFilterRequest filter, bool coQuyenXemNhayCam);
    Task<ImageDto> TaiLenHinhAnhAsync(UploadImageRequest req, IProgress<int>? progress = null);

    /// <summary>Nạp ảnh từ luồng (HTTP multipart). Đường dẫn tệp luôn do backend tự sinh.</summary>
    Task<ImageDto> TaiLenHinhAnhTuStreamAsync(
        Stream nguon, string tenFileTaiLen, UploadImageRequest req, IProgress<int>? progress = null);

    Task<bool> XoaHinhAnhAsync(long maHinhAnh, long maTaiKhoan);
    string LayDuongDanVatLy(string duongDanLuuTru);
    Task<byte[]?> DocDuLieuHinhAnhAsync(long maHinhAnh, bool coQuyenXemNhayCam);

    /// <summary>Đọc dữ liệu ảnh với điều kiện ảnh phải thuộc đúng hồ sơ thi thể được yêu cầu.</summary>
    Task<(byte[] DuLieu, string DinhDang)?> DocAnhCuaHoSoThiTheAsync(long maHoSoThiThe, long maHinhAnh);
}
