using DeathManagement.API.DTOs.MissingPerson;

namespace DeathManagement.API.Repositories.Interfaces;

public interface IMissingPersonRepository
{
    /// <summary>
    /// Tổng hợp chi tiết đầy đủ một hồ sơ người mất tích (Task 8.4):
    /// HoSoNguoiMatTich + NguoiThan + sp_LayHinhAnhTheoHoSo + v_DoiSanhChiTiet.
    /// </summary>
    Task<MissingPersonDetailDto?> GetDetailByIdAsync(
        long maHoSoMatTich,
        long maTaiKhoan,
        bool coQuyenXemHinhAnh,
        bool coQuyenXemDoiSanh);
}
