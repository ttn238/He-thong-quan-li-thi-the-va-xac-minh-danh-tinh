using DeathManagement.API.Data;
using DeathManagement.API.Forms;
using DeathManagement.API.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DeathManagement.API;

static class Program
{
    // Đọc cấu hình từ appsettings.json (phải khai báo TRƯỚC Options vì khởi tạo theo thứ tự)
    internal static readonly IConfigurationRoot Config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .Build();

    // ĐĂNG KÝ "policy" ở tầng dữ liệu: interceptor kiểm tra quyền cho MỌI lệnh ghi
    static readonly DbContextOptions<ApplicationDbContext> Options =
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(Config.GetConnectionString("DefaultConnection"))
            .AddInterceptors(new AuthorizationSaveChangesInterceptor())
            .Options;

    // Mỗi lần cần truy cập CSDL thì tạo một DbContext mới
    public static ApplicationDbContext NewDb() => new(Options);
    public static string AiBaseUrl => Config["AiService:BaseUrl"] ?? "http://localhost:8000";
    public static int AiTimeoutSeconds => int.TryParse(Config["AiService:TimeoutSeconds"], out var s) && s > 0 ? s : 30;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 1. Cấu hình JWT (khóa lấy từ appsettings "Jwt:Key" nếu có; để trống => khóa ngẫu nhiên mỗi lần chạy)
        JwtTokenService.Configure(new JwtOptions
        {
            Key = Config["Jwt:Key"] ?? string.Empty,
            Issuer = Config["Jwt:Issuer"] ?? "DeathManagement.API",
            Audience = Config["Jwt:Audience"] ?? "DeathManagement.Client",
            ExpireMinutes = int.TryParse(Config["Jwt:ExpireMinutes"], out var m) && m > 0 ? m : 480
        });

        // 2. Ghi Audit Log mọi lần bị từ chối quyền
        AccessControl.Denied += (userId, resource, action, reason) =>
        {
            try
            {
                using var db = NewDb();
                var hanhDong = "TuChoiTruyCap";
                var doiTuong = $"{resource}:{action} - {reason}";
                if (doiTuong.Length > 255) doiTuong = doiTuong[..255];
                db.Database.ExecuteSqlInterpolated($"EXEC sp_GhiNhatKy {hanhDong}, {doiTuong}, {userId}");
            }
            catch { /* không để lỗi ghi log làm hỏng luồng chính */ }
        };

        // 3. Lỗi 401/403 ném từ các handler async của Form được xử lý tập trung tại đây
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => XuLyLoiToanCuc(e.Exception);

        while (true)
        {
            using var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK) break;     // đóng form đăng nhập = thoát

            using var home = new FrmDashboard();
            home.ShowDialog();
            if (home.DialogResult != DialogResult.Retry) break;   // bấm Đăng xuất = quay lại đăng nhập
        }
    }

    static void XuLyLoiToanCuc(Exception ex)
    {
        switch (ex)
        {
            case UnauthenticatedException:
                {
                    MessageBox.Show(ex.Message + "\nVui lòng đăng nhập lại.", "Phiên đăng nhập",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Session.Clear();
                    var forms = Application.OpenForms.Cast<Form>().ToList();
                    foreach (var con in forms.Where(x => x is not FrmDashboard)) con.Close();             // đóng form con trước
                    foreach (var dash in forms.OfType<FrmDashboard>()) dash.DialogResult = DialogResult.Retry; // rồi về màn đăng nhập
                    break;
                }

            case ForbiddenException:
                MessageBox.Show(ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                break;

            default:
                MessageBox.Show("Đã xảy ra lỗi không mong muốn:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;
        }
    }
}