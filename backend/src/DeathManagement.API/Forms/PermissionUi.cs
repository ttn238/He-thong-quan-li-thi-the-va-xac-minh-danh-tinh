using DeathManagement.API.Security;

namespace DeathManagement.API.Forms;

/// <summary>Cầu nối UI -> AccessControl: kiểm tra quyền thật, nếu thiếu thì báo lỗi và trả false.</summary>
public static class PermissionUi
{
    public static bool Check(IWin32Window? owner, string resource, string action)
        => Run(owner, () => AccessControl.Require(resource, action));

    public static bool CheckPolicy(IWin32Window? owner, string policy)
        => Run(owner, () => AccessControl.RequirePolicy(policy));

    private static bool Run(IWin32Window? owner, Action kiemTra)
    {
        try
        {
            kiemTra();
            return true;
        }
        catch (UnauthenticatedException ex)
        {
            MessageBox.Show(owner, ex.Message + "\nVui lòng đăng nhập lại.", "Phiên đăng nhập",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        catch (ForbiddenException ex)
        {
            MessageBox.Show(owner, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return false;
        }
    }
}