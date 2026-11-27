using DeathManagement.API.DTOs.MissingPerson;

namespace DeathManagement.API.Services.Interfaces;

public interface IMissingPersonService
{
    /// <summary>
    /// Xem chi tiết đầy đủ hồ sơ người mất tích theo mã (Task 8.4 — Yêu cầu quyền MISSING_PERSON:VIEW).
    /// </summary>
    Task<MissingPersonDetailDto?> GetByIdAsync(long maHoSoMatTich);
}
