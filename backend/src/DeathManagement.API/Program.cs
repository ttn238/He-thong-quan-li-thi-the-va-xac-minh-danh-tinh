using DeathManagement.API.Data;
using DeathManagement.API.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DeathManagement.API;

static class Program
{
    // Đọc chuỗi kết nối từ appsettings.json
    static readonly DbContextOptions<ApplicationDbContext> Options =
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build()
                .GetConnectionString("DefaultConnection"))
            .Options;

    // Mỗi lần cần truy cập CSDL thì tạo một DbContext mới
    public static ApplicationDbContext NewDb() => new(Options);

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        while (true)
        {
            using var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK) break;     // đóng form đăng nhập = thoát

            using var home = new FrmDashboard();
            home.ShowDialog();
            if (home.DialogResult != DialogResult.Retry) break;   // bấm Đăng xuất = quay lại đăng nhập
        }
    }
}
