using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static DeathManagement.API.Security.AuthorizationPolicies;
using EntityImage = DeathManagement.API.Entities.Image;

namespace DeathManagement.API.Security;

/// <summary>
/// "Lưới an toàn" ở tầng dữ liệu: MỌI lệnh ghi (Add / Update / Delete) qua ApplicationDbContext
/// đều bị kiểm tra quyền tại đây, bất kể được gọi từ Form hay Service. Nhờ vậy dù UI quên ẩn nút
/// hoặc Form truy cập thẳng DbContext thì vẫn không ghi được khi thiếu quyền.
/// </summary>
public sealed class AuthorizationSaveChangesInterceptor : SaveChangesInterceptor
{
    // Trường hệ thống được phép cập nhật khi đăng nhập (kể cả CHƯA có phiên) - đếm sai, khóa tạm, nâng cấp hash
    private static readonly HashSet<string> LoginFields = new()
    {
        nameof(User.SoLanDangNhapSai), nameof(User.TrangThai), nameof(User.KhoaDenThoiDiem),
        nameof(User.LanDangNhapCuoi), nameof(User.MatKhauHash), nameof(User.NgayCapNhat)
    };

    // Trường mà MỌI người dùng đã đăng nhập được tự sửa trên tài khoản CỦA MÌNH
    private static readonly HashSet<string> SelfServiceFields = new()
    {
        nameof(User.MatKhauHash), nameof(User.NgayCapNhat), nameof(User.HoTen),
        nameof(User.Email), nameof(User.SoDienThoai)
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Check(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Check(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void Check(DbContext? context)
    {
        if (context == null) return;
        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added && entry.State != EntityState.Modified && entry.State != EntityState.Deleted)
                continue;

            switch (entry.Entity)
            {
                case User user: CheckUser(entry, user); break;
                case EntityImage img: CheckImage(entry, img); break;
            }
        }
    }

    private static HashSet<string> ChangedFields(EntityEntry entry)
        => entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToHashSet();

    // ------------------------------------------------------------------ TÀI KHOẢN
    private static void CheckUser(EntityEntry entry, User user)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                AccessControl.Require(ResourceAccount, ActionCreate);
                break;

            case EntityState.Deleted:
                // Không xóa cứng tài khoản (mất dấu vết Audit); chỉ khóa tài khoản.
                throw AccessControl.NoPermission(AccessControl.Principal, ResourceAccount, ActionDelete,
                    "Không được xóa cứng tài khoản. Hãy chuyển trạng thái sang Khoa.");

            case EntityState.Modified:
                {
                    var changed = ChangedFields(entry);
                    if (changed.Count == 0) return;

                    var uid = AccessControl.UserId;
                    var self = uid.HasValue && uid.Value == user.MaTaiKhoan;

                    // (1) Chưa đăng nhập: chỉ được ghi các trường phục vụ chính quá trình đăng nhập
                    if (!AccessControl.IsAuthenticated)
                    {
                        if (changed.IsSubsetOf(LoginFields)) return;
                        throw AccessControl.NotLoggedIn(ResourceAccount, ActionUpdate);
                    }

                    // (2) Tự phục vụ: đổi mật khẩu / hồ sơ cá nhân của chính mình (mọi vai trò)
                    if (self && changed.IsSubsetOf(SelfServiceFields)) return;

                    // (3) Còn lại: cần quyền quản lý tài khoản
                    AccessControl.Require(ResourceAccount, ActionUpdate);

                    // (4) Đổi vai trò = phân quyền => cần PERMISSION:UPDATE, và không được tự đổi vai trò của chính mình
                    if (changed.Contains(nameof(User.MaVaiTro)))
                    {
                        AccessControl.Require(ResourcePermission, ActionUpdate);
                        if (self)
                            throw AccessControl.NoPermission(AccessControl.Principal, ResourcePermission, ActionUpdate,
                                "Không được tự thay đổi vai trò của chính mình (chống leo thang đặc quyền).");
                    }

                    // (5) Không tự khóa / đổi trạng thái của chính mình
                    if (self && changed.Contains(nameof(User.TrangThai)))
                        throw AccessControl.NoPermission(AccessControl.Principal, ResourceAccount, ActionUpdate,
                            "Không được tự thay đổi trạng thái tài khoản của chính mình.");
                    break;
                }
        }
    }

    // ------------------------------------------------------------------ HÌNH ẢNH / TỆP
    private static void CheckImage(EntityEntry entry, EntityImage img)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                {
                    AccessControl.Require(ResourceFile, ActionCreate);

                    // Không cho gán "người tải lên" là người khác
                    if (img.MaTaiKhoanTaiLen != AccessControl.UserId)
                        throw AccessControl.NoPermission(AccessControl.Principal, ResourceFile, ActionCreate,
                            "Không được tải tệp dưới danh nghĩa người khác.");

                    // Ảnh nhạy cảm (tử thi / khám nghiệm) chỉ nhóm được xem dữ liệu nhạy cảm mới được tải
                    if (img.LaNhayCam || img.MaHoSoThiThe.HasValue)
                        AccessControl.RequirePolicy(RequireSensitiveDataViewer);
                    break;
                }

            case EntityState.Modified:
                {
                    var changed = ChangedFields(entry);
                    if (changed.Count == 0) return;

                    var xoaMem = changed.Contains(nameof(EntityImage.TrangThai)) && img.TrangThai == "DaXoa";
                    if (xoaMem)
                        AccessControl.RequireSelfOr(img.MaTaiKhoanTaiLen, ResourceFile, ActionDelete); // chủ tệp hoặc FILE:DELETE
                    else
                        AccessControl.RequireOwnerOrAll(ResourceFile, ActionUpdate, img.MaTaiKhoanTaiLen);
                    break;
                }

            case EntityState.Deleted:
                throw AccessControl.NoPermission(AccessControl.Principal, ResourceFile, ActionDelete,
                    "Không được xóa cứng tệp. Hãy dùng xóa mềm (TrangThai = DaXoa).");
        }
    }
}