using DeathManagement.API.Data;
using DeathManagement.API.Security;
using Microsoft.EntityFrameworkCore;
using static DeathManagement.API.Security.AuthorizationPolicies;

namespace DeathManagement.API.Services;

public class TinBaoService(ApplicationDbContext db)
{
    /// <summary>Cán bộ xử lý tin báo (DaTiepNhan / TuChoi / YeuCauBoSung / DangXacMinh / DaXuLy)</summary>
    public async Task XuLyTinBaoAsync(int maTinBao, string quyetDinh)
    {
        AccessControl.Require(ResourceIncident, ActionConfirm);
        var uid = AccessControl.UserId!.Value;           // lấy từ token, không nhận từ UI
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_XuLyTinBao {maTinBao}, {uid}, {quyetDinh}");
    }
    // Khi viết GuiTinBaoAsync: AccessControl.Require(ResourceIncident, ActionCreate);
    // NguonTin = vai trò NGUOI_DAN ? "NguoiDan" : "CanBo"; MaTaiKhoanTiepNhan = AccessControl.UserId
    // Xem tin báo: ScopeOf(ResourceIncident, ActionView) == ScopeOwn thì lọc theo người tạo.
}

public class HoSoThiTheService(ApplicationDbContext db)
{
    public async Task XepViTriAsync(long maHoSoThiThe, int maViTri)
    {
        AccessControl.Require(ResourceStorage, ActionUpdate);
        var uid = AccessControl.UserId!.Value;
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_XepViTri {maHoSoThiThe}, {maViTri}, {uid}");
    }

    public async Task DongHoSoAsync(long maHoSoThiThe)
    {
        AccessControl.Require(ResourceBody, ActionUpdate);
        var uid = AccessControl.UserId!.Value;
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_DongHoSoThiThe {maHoSoThiThe}, {uid}");
    }

    public async Task MoLaiHoSoAsync(long maHoSoThiThe, string lyDo)
    {
        AccessControl.Require(ResourceBody, ActionUpdate);
        var uid = AccessControl.UserId!.Value;
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_MoLaiHoSoThiThe {maHoSoThiThe}, {uid}, {lyDo}");
    }
    // Khi viết LapHoSoAsync / XemHoSoAsync: AccessControl.Require(ResourceBody, ActionCreate / ActionView)
    // Quyền BODY:VIEW dạng PARTIAL (NHA_XAC): cần lọc thêm theo hồ sơ được phân công khi có dữ liệu phân công.
}