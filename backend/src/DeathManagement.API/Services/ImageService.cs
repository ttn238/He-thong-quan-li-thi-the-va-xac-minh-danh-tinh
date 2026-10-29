using System.Security.Cryptography;
using DeathManagement.API.Helpers;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Entities;
using DeathManagement.API.Helpers;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;
using DeathManagement.API.Helpers;
using Microsoft.EntityFrameworkCore;
using Image = DeathManagement.API.Entities.Image;

namespace DeathManagement.API.Services;

public class ImageService(ApplicationDbContext db) : IImageService
{
    private static readonly string StorageBaseDir = Program.Config["Storage:RootPath"] is { Length: > 0 } root
    ? root : Path.Combine(AppContext.BaseDirectory, "App_Data", "storage");

    public string LayDuongDanVatLy(string duongDanLuuTru)
    {
        if (string.IsNullOrWhiteSpace(duongDanLuuTru))
            return string.Empty;

        // Nếu là đường dẫn Windows tuyệt đối với ổ đĩa (vd: C:\path\file.jpg) và file tồn tại
        if (duongDanLuuTru.Length >= 2 && duongDanLuuTru[1] == ':' && File.Exists(duongDanLuuTru))
            return duongDanLuuTru;

        // Chuẩn hóa đường dẫn tương đối (loại bỏ '/' hoặc '\' ở đầu)
        var clean = duongDanLuuTru.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);

        // Map các alias đường dẫn từ seed data hoặc cấu trúc cũ
        clean = clean.Replace($"secure-storage{Path.DirectorySeparatorChar}bodies", "secure")
                     .Replace($"public-storage{Path.DirectorySeparatorChar}missing", "public")
                     .Replace("secure-storage", "secure")
                     .Replace("public-storage", "public");

        var candidate = Path.Combine(StorageBaseDir, clean);
        if (File.Exists(candidate))
            return candidate;

        // Tìm thêm trong thư mục secure hoặc public theo tên file
        var fileName = Path.GetFileName(clean);
        var inSecure = Path.Combine(StorageBaseDir, "secure", fileName);
        if (File.Exists(inSecure))
            return inSecure;

        var inPublic = Path.Combine(StorageBaseDir, "public", fileName);
        if (File.Exists(inPublic))
            return inPublic;

        // Thử tìm trong App_Data ở thư mục gốc của project (nếu chạy từ VS/IDE)
        var projectStorage = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "App_Data", "storage");
        if (Directory.Exists(projectStorage))
        {
            var inProj = Path.Combine(projectStorage, clean);
            if (File.Exists(inProj)) return inProj;

            var inProjSec = Path.Combine(projectStorage, "secure", fileName);
            if (File.Exists(inProjSec)) return inProjSec;

            var inProjPub = Path.Combine(projectStorage, "public", fileName);
            if (File.Exists(inProjPub)) return inProjPub;
        }

        return candidate;
    }

    public async Task<List<ImageDto>> LayDanhSachHinhAnhAsync(ImageFilterRequest filter, bool coQuyenXemNhayCam)
    {
        // [PHÂN QUYỀN] Backend tự quyết định, KHÔNG tin tham số coQuyenXemNhayCam do UI truyền vào
        AccessControl.Require(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionView);
        coQuyenXemNhayCam = AccessControl.CanPolicy(AuthorizationPolicies.RequireSensitiveDataViewer);
        var phamViXem = AccessControl.ScopeOf(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionView);
        var maNguoiDung = AccessControl.UserId ?? 0;

        var q = db.Images
            .AsNoTracking()
            .Include(x => x.TaiKhoanTaiLen)
            .Where(x => x.TrangThai == "HoatDong");

        // NGUOI_DAN (phạm vi OWN): chỉ thấy tệp do chính mình tải lên
        if (phamViXem == AuthorizationPolicies.ScopeOwn)
            q = q.Where(x => x.MaTaiKhoanTaiLen == maNguoiDung);

        // NFR1.2: Nếu không có quyền Cán bộ/Admin, tuyệt đối ẩn ảnh nhạy cảm
        if (!coQuyenXemNhayCam)
        {
            q = q.Where(x => !x.LaNhayCam);
        }
        else if (filter.ChiHienNhayCam == true)
        {
            q = q.Where(x => x.LaNhayCam);
        }

        // Lọc theo đối tượng cụ thể
        if (filter.MaHoSoThiThe.HasValue)
            q = q.Where(x => x.MaHoSoThiThe == filter.MaHoSoThiThe.Value);

        if (filter.MaHoSoMatTich.HasValue)
            q = q.Where(x => x.MaHoSoMatTich == filter.MaHoSoMatTich.Value);

        if (filter.ChiHoSoThiThe == true)
            q = q.Where(x => x.MaHoSoThiThe != null);

        if (filter.ChiHoSoMatTich == true)
            q = q.Where(x => x.MaHoSoMatTich != null);

        if (filter.MaTinBao.HasValue)
            q = q.Where(x => x.MaTinBao == filter.MaTinBao.Value);

        if (filter.MaXacMinh.HasValue)
            q = q.Where(x => x.MaXacMinh == filter.MaXacMinh.Value);

        if (!string.IsNullOrWhiteSpace(filter.LoaiHinhAnh) && !filter.LoaiHinhAnh.StartsWith("Tất cả"))
            q = q.Where(x => x.LoaiHinhAnh == filter.LoaiHinhAnh);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            q = q.Where(x => x.TenGoc.ToLower().Contains(kw) ||
                             (x.MoTa != null && x.MoTa.ToLower().Contains(kw)) ||
                             (x.TaiKhoanTaiLen != null && x.TaiKhoanTaiLen.HoTen.ToLower().Contains(kw)));
        }

        var list = await q.OrderByDescending(x => x.ThoiGianTaiLen).ToListAsync();

        return list.Select(x => new ImageDto(
            x.MaHinhAnh,
            x.TenFile,
            x.TenGoc,
            x.DuongDan,
            x.LoaiHinhAnh,
            x.DinhDang,
            x.KichThuoc,
            x.ChieuRong,
            x.ChieuCao,
            x.HashMD5,
            x.LaNhayCam,
            x.MoTa,
            x.MaHoSoThiThe,
            x.MaHoSoMatTich,
            x.MaTinBao,
            x.MaXacMinh,
            x.MaTaiKhoanTaiLen,
            x.TaiKhoanTaiLen?.HoTen ?? "Hệ thống",
            x.ThoiGianTaiLen,
            x.TrangThai
        )).ToList();
    }

    public async Task<ImageDto> TaiLenHinhAnhAsync(UploadImageRequest req, IProgress<int>? progress = null)
    {
        // [PHÂN QUYỀN] kiểm tra TRƯỚC khi sao chép tệp để không sinh tệp rác
        AccessControl.Require(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionCreate);
        req.MaTaiKhoanTaiLen = AccessControl.UserId!.Value;   // luôn lấy từ token, bỏ qua giá trị UI gửi lên

        if (!req.MaHoSoThiThe.HasValue && !req.MaHoSoMatTich.HasValue
            && !req.MaTinBao.HasValue && !req.MaXacMinh.HasValue)
            throw new InvalidOperationException("Ảnh phải gắn với ít nhất một tin báo, hồ sơ thi thể, hồ sơ mất tích hoặc xác minh.");

        if (!ImageHelper.LoaiHopLe.Contains(req.LoaiHinhAnh))
            throw new InvalidOperationException("Loại hình ảnh không hợp lệ.");

        // Người dân (phạm vi OWN) chỉ được thêm ảnh vào tin báo do chính mình tạo
        if (req.MaTinBao.HasValue && AccessControl.RoleCode == AuthorizationPolicies.RoleCitizen)
        {
            var laCuaToi = await db.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM TinBao WHERE MaTinBao = {req.MaTinBao} AND MaTaiKhoanTiepNhan = {req.MaTaiKhoanTaiLen}")
                .SingleAsync();
            if (laCuaToi == 0)
                throw new InvalidOperationException("Bạn không có quyền thêm ảnh vào tin báo này.");
        }

        if (req.MaTinBao.HasValue
            && await db.Images.CountAsync(x => x.MaTinBao == req.MaTinBao && x.TrangThai == "HoatDong") >= ImageHelper.MaxAnhMoiTinBao)
            throw new InvalidOperationException($"Tin báo đã đủ {ImageHelper.MaxAnhMoiTinBao} ảnh.");

        if (req.MaHoSoMatTich.HasValue
            && await db.Images.CountAsync(x => x.MaHoSoMatTich == req.MaHoSoMatTich && x.TrangThai == "HoatDong") >= ImageHelper.MaxAnhMoiHoSoMatTich)
            throw new InvalidOperationException($"Hồ sơ mất tích đã đủ {ImageHelper.MaxAnhMoiHoSoMatTich} ảnh.");

        if (!File.Exists(req.FilePath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy tệp ảnh được chọn.");
        }

        var fi = new FileInfo(req.FilePath);
        if (!FileHelper.IsValidImageExtension(fi.Name))
            throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ.");
        if (!FileHelper.IsValidFileSize(fi.Length))
            throw new InvalidOperationException("Ảnh phải lớn hơn 0 và không quá 10 MB.");
        if (!FileHelper.IsValidImageContent(req.FilePath))
            throw new InvalidOperationException("Tệp không phải ảnh hợp lệ hoặc đã bị hỏng.");
        if (!ImageHelper.LoaiHopLe.Contains(req.LoaiHinhAnh))
            throw new InvalidOperationException("Loại hình ảnh không hợp lệ.");
        if (req.MaHoSoThiThe == null && req.MaHoSoMatTich == null && req.MaTinBao == null && req.MaXacMinh == null)
            throw new InvalidOperationException("Ảnh phải gắn với ít nhất một hồ sơ.");

        if (req.MaTinBao.HasValue)
        {
            // Người dân chỉ được thêm ảnh vào tin báo do chính mình tạo
            if (AccessControl.RoleCode == AuthorizationPolicies.RoleCitizen)
            {
                int cuaToi = await db.Database.SqlQuery<int>(
                    $"SELECT COUNT(*) AS [Value] FROM TinBao WHERE MaTinBao = {req.MaTinBao.Value} AND MaTaiKhoanTiepNhan = {req.MaTaiKhoanTaiLen}").SingleAsync();
                if (cuaToi == 0)
                    throw AccessControl.NoPermission(AccessControl.Principal, AuthorizationPolicies.ResourceFile,
                        AuthorizationPolicies.ActionCreate, "Chỉ được thêm ảnh vào tin báo do chính mình tạo.");
            }
            int daCo = await db.Images.CountAsync(x => x.MaTinBao == req.MaTinBao && x.TrangThai == "HoatDong");
            if (daCo >= ImageHelper.MaxAnhMoiTinBao)
                throw new InvalidOperationException($"Mỗi tin báo tối đa {ImageHelper.MaxAnhMoiTinBao} ảnh.");
        }
        if (req.MaHoSoMatTich.HasValue)
        {
            int daCo = await db.Images.CountAsync(x => x.MaHoSoMatTich == req.MaHoSoMatTich && x.TrangThai == "HoatDong");
            if (daCo >= ImageHelper.MaxAnhMoiHoSoMatTich)
                throw new InvalidOperationException($"Mỗi hồ sơ mất tích tối đa {ImageHelper.MaxAnhMoiHoSoMatTich} ảnh.");
        }

        var ext = fi.Extension.ToLowerInvariant();
        var mime = ImageHelper.GetMimeType(fi.Name);

        // Quy tắc NFR1.2: Ảnh tử thi hoặc loại ThiThe_* bắt buộc gán cờ LaNhayCam = true
        var laNhayCam = req.LaNhayCam || req.MaHoSoThiThe.HasValue || req.LoaiHinhAnh.StartsWith("ThiThe_");
        if (laNhayCam) AccessControl.RequirePolicy(AuthorizationPolicies.RequireSensitiveDataViewer);
        var subFolder = laNhayCam ? "secure" : "public";
        var destFolder = Path.Combine(StorageBaseDir, subFolder);
        Directory.CreateDirectory(destFolder);

        var fileGuid = $"{Guid.NewGuid():N}{ext}";
        var destFilePath = Path.Combine(destFolder, fileGuid);
        var relativePath = $"/{subFolder}/{fileGuid}";

        // Async I/O copy file có báo cáo tiến trình (Chương 5 Đồ án)
        await using (var sourceStream = new FileStream(duongDanNguon, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
        await using (var destStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
            {
                await destStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;
                if (fi.Length > 0 && progress != null)
                {
                    var percent = (int)(totalRead * 100 / fi.Length);
                    progress.Report(percent);
                }
            }
        }

        // Tính mã băm MD5 và đọc độ phân giải (GDI+)
        string hashMd5;
        await using (var fs = File.OpenRead(destFilePath))
        {
            var hashBytes = await MD5.HashDataAsync(fs);
            hashMd5 = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        bool trung = await db.Images.AnyAsync(x => x.HashMD5 == hashMd5 && x.TrangThai == "HoatDong"
            && x.MaTinBao == req.MaTinBao && x.MaHoSoThiThe == req.MaHoSoThiThe
            && x.MaHoSoMatTich == req.MaHoSoMatTich && x.MaXacMinh == req.MaXacMinh);
        if (trung)
        {
            try { File.Delete(destFilePath); } catch { }
            throw new InvalidOperationException("Ảnh này đã được tải lên cho hồ sơ này.");
        }

        int? width = null;
        int? height = null;
        try
        {
            using var img = System.Drawing.Image.FromFile(destFilePath);
            width = img.Width;
            height = img.Height;
        }
        catch
        {
            // Trường hợp không đọc được dimensions bằng GDI+
        }

        var entity = new Image
        {
            TenFile = fileGuid,
            TenGoc = fi.Name,
            DuongDan = relativePath,
            LoaiHinhAnh = req.LoaiHinhAnh,
            DinhDang = mime,
            KichThuoc = fi.Length,
            ChieuRong = width,
            ChieuCao = height,
            HashMD5 = hashMd5,
            LaNhayCam = laNhayCam,
            MoTa = req.MoTa,
            MaHoSoThiThe = req.MaHoSoThiThe,
            MaHoSoMatTich = req.MaHoSoMatTich,
            MaTinBao = req.MaTinBao,
            MaXacMinh = req.MaXacMinh,
            MaTaiKhoanTaiLen = req.MaTaiKhoanTaiLen,
            ThoiGianTaiLen = DateTime.UtcNow,
            TrangThai = "HoatDong"
        };

        db.Images.Add(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Lấy lỗi chi tiết từ SQL Server / Entity Framework
            string loiChiTiet = ex.ToString();

            try
            {
                if (File.Exists(destFilePath))
                    File.Delete(destFilePath);
            }
            catch
            {
                // Không che mất lỗi gốc.
            }

            System.Diagnostics.Debug.WriteLine(loiChiTiet);

            throw new InvalidOperationException(
                "Lỗi lưu thông tin ảnh vào database:\n" + loiChiTiet,
                ex);
        }


        // Ghi Audit Log qua thủ tục sp_GhiNhatKy
        try
        {
            var act = $"TAI_ANH#{entity.MaHinhAnh} ({entity.LoaiHinhAnh})";
            var target = $"HinhAnh#{entity.MaHinhAnh}";
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy {act}, {target}, {req.MaTaiKhoanTaiLen}");
        }
        catch { }

        var uploader = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.MaTaiKhoan == req.MaTaiKhoanTaiLen);

        return new ImageDto(
            entity.MaHinhAnh,
            entity.TenFile,
            entity.TenGoc,
            entity.DuongDan,
            entity.LoaiHinhAnh,
            entity.DinhDang,
            entity.KichThuoc,
            entity.ChieuRong,
            entity.ChieuCao,
            entity.HashMD5,
            entity.LaNhayCam,
            entity.MoTa,
            entity.MaHoSoThiThe,
            entity.MaHoSoMatTich,
            entity.MaTinBao,
            entity.MaXacMinh,
            entity.MaTaiKhoanTaiLen,
            uploader?.HoTen ?? "Cán bộ",
            entity.ThoiGianTaiLen,
            entity.TrangThai
        );
    }

    public async Task<bool> XoaHinhAnhAsync(long maHinhAnh, long maTaiKhoan)
    {
        var maNguoiThaoTac = AccessControl.UserId ?? throw AccessControl.NotLoggedIn(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionDelete);

        var img = await db.Images.FirstOrDefaultAsync(x => x.MaHinhAnh == maHinhAnh);
        if (img == null || img.TrangThai == "DaXoa")
            return false;

        // Chủ tệp được xóa tệp của mình; người khác phải có FILE:DELETE (tham số maTaiKhoan từ UI bị bỏ qua)
        AccessControl.RequireSelfOr(img.MaTaiKhoanTaiLen, AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionDelete);

        img.TrangThai = "DaXoa";
        await db.SaveChangesAsync();

        try
        {
            var act = $"XOA_ANH#{maHinhAnh}";
            var target = $"HinhAnh#{maHinhAnh}";
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy {act}, {target}, {maNguoiThaoTac}");
        }
        catch { }

        return true;
    }

    public async Task<byte[]?> DocDuLieuHinhAnhAsync(long maHinhAnh, bool coQuyenXemNhayCam)
    {
        AccessControl.Require(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionView);
        coQuyenXemNhayCam = AccessControl.CanPolicy(AuthorizationPolicies.RequireSensitiveDataViewer);

        var img = await db.Images.AsNoTracking().FirstOrDefaultAsync(x => x.MaHinhAnh == maHinhAnh && x.TrangThai == "HoatDong");
        if (img == null)
            return null;

        AccessControl.RequireOwnerOrAll(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionView, img.MaTaiKhoanTaiLen);

        if (img.LaNhayCam && !coQuyenXemNhayCam)
            return null; // NFR1.2: Chặn đọc file nhạy cảm trái phép

        var physicalPath = LayDuongDanVatLy(img.DuongDan);
        if (!File.Exists(physicalPath))
            return null;

        return await File.ReadAllBytesAsync(physicalPath);
    }
}
