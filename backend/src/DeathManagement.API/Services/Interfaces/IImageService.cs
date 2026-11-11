using DeathManagement.API.DTOs.Image;

namespace DeathManagement.API.Services.Interfaces;

public interface IImageService
{
    Task<List<ImageDto>> LayDanhSachHinhAnhAsync(ImageFilterRequest filter, bool coQuyenXemNhayCam);
    Task<ImageDto> TaiLenHinhAnhAsync(UploadImageRequest req, IProgress<int>? progress = null);
    Task<bool> XoaHinhAnhAsync(long maHinhAnh, long maTaiKhoan);
    string LayDuongDanVatLy(string duongDanLuuTru);
    Task<byte[]?> DocDuLieuHinhAnhAsync(long maHinhAnh, bool coQuyenXemNhayCam);
}
