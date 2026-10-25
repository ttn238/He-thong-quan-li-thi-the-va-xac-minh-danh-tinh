using System.Drawing.Drawing2D;
using DeathManagement.API.Data;
using DeathManagement.API.Security;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Forms;

public class FrmDoiMatKhau : Form
{
    private readonly TextBox txtMatKhauCu = new() { BorderStyle = BorderStyle.None, Font = UITheme.FontRegular(10), UseSystemPasswordChar = true, PlaceholderText = "Mật khẩu đang sử dụng" };
    private readonly TextBox txtMatKhauMoi = new() { BorderStyle = BorderStyle.None, Font = UITheme.FontRegular(10), UseSystemPasswordChar = true, PlaceholderText = "Tối thiểu 6 ký tự" };
    private readonly TextBox txtXacNhan = new() { BorderStyle = BorderStyle.None, Font = UITheme.FontRegular(10), UseSystemPasswordChar = true, PlaceholderText = "Nhập lại mật khẩu mới" };

    private readonly Label lblThongBao = new()
    {
        Size = new Size(380, 36),
        BackColor = ColorTranslator.FromHtml("#fdecea"),
        ForeColor = UITheme.Red,
        Font = UITheme.FontRegular(8.5f),
        Padding = new Padding(10, 0, 10, 0),
        TextAlign = ContentAlignment.MiddleLeft,
        Visible = false
    };

    private readonly Button btnXacNhan = new()
    {
        Text = "CẬP NHẬT MẬT KHẨU",
        Size = new Size(190, 38),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        Font = UITheme.FontBold(9),
        Cursor = Cursors.Hand
    };

    private readonly Button btnHuy = new()
    {
        Text = "ĐÓNG",
        Size = new Size(100, 38),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = UITheme.Navy,
        Font = UITheme.FontBold(9),
        Cursor = Cursors.Hand
    };

    public FrmDoiMatKhau()
    {
        Text = "Đổi mật khẩu người dùng";
        ClientSize = new Size(460, 430);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
    }

    private void BuildUI()
    {
        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = UITheme.Navy };
        var lblTitle = new Label
        {
            Text = "THAY ĐỔI MẬT KHẨU TÀI KHOẢN",
            ForeColor = Color.White,
            Font = UITheme.FontBold(11),
            AutoSize = true,
            Location = new Point(20, 14)
        };
        var lblSub = new Label
        {
            Text = $"Tài khoản: {Session.TaiKhoan?.TenDangNhap} ({Session.TaiKhoan?.HoTen})",
            ForeColor = UITheme.Gold,
            Font = UITheme.FontRegular(8.5f),
            AutoSize = true,
            Location = new Point(20, 38)
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // Body Card
        var pnlCard = new Panel
        {
            Location = new Point(20, 80),
            Size = new Size(420, 330),
            BackColor = Color.White
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        int top = 15;
        lblThongBao.Location = new Point(20, top);
        pnlCard.Controls.Add(lblThongBao);
        top += 42;

        top = AddField(pnlCard, "Mật khẩu hiện tại *", txtMatKhauCu, top);
        top = AddField(pnlCard, "Mật khẩu mới *", txtMatKhauMoi, top);
        top = AddField(pnlCard, "Xác nhận mật khẩu mới *", txtXacNhan, top);

        top += 10;
        btnXacNhan.Location = new Point(20, top);
        btnXacNhan.FlatAppearance.BorderSize = 0;
        btnXacNhan.Click += async (s, e) => await XuLyDoiMatKhau();

        btnHuy.Location = new Point(220, top);
        btnHuy.FlatAppearance.BorderColor = UITheme.Line;
        btnHuy.Click += (s, e) => Close();

        pnlCard.Controls.Add(btnXacNhan);
        pnlCard.Controls.Add(btnHuy);

        Controls.Add(pnlCard);
        Controls.Add(pnlHeader);
    }

    private int AddField(Panel pnl, string labelText, TextBox txt, int y)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = UITheme.FontBold(8.5f),
            ForeColor = UITheme.Navy,
            Location = new Point(20, y),
            AutoSize = true
        };
        pnl.Controls.Add(lbl);

        var pnlBox = new Panel
        {
            Location = new Point(20, y + 20),
            Size = new Size(380, 32),
            BackColor = UITheme.Soft
        };
        pnlBox.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlBox.Width - 1, pnlBox.Height - 1);
        };

        txt.Location = new Point(8, 6);
        txt.Width = 364;
        txt.BackColor = UITheme.Soft;
        pnlBox.Controls.Add(txt);
        pnl.Controls.Add(pnlBox);

        return y + 60;
    }

    private async Task XuLyDoiMatKhau()
    {
        lblThongBao.Visible = false;

        var cu = txtMatKhauCu.Text;
        var moi = txtMatKhauMoi.Text;
        var xn = txtXacNhan.Text;

        if (string.IsNullOrEmpty(cu) || string.IsNullOrEmpty(moi) || string.IsNullOrEmpty(xn))
        {
            HienLoi("Vui lòng nhập đầy đủ các trường mật khẩu.");
            return;
        }

        if (moi.Length < 6)
        {
            HienLoi("Mật khẩu mới phải có tối thiểu 6 ký tự.");
            return;
        }

        if (moi != xn)
        {
            HienLoi("Mật khẩu xác nhận không khớp với mật khẩu mới.");
            return;
        }

        btnXacNhan.Enabled = false;
        btnXacNhan.Text = "ĐANG XỬ LÝ...";

        try
        {
            using var db = Program.NewDb();
            var maTaiKhoan = Session.TaiKhoan?.MaTaiKhoan;
            if (maTaiKhoan == null)
            {
                HienLoi("Không xác định được phiên làm việc.");
                return;
            }

            var user = await db.Users.FirstOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan.Value);
            if (user == null)
            {
                HienLoi("Không tìm thấy thông tin tài khoản.");
                return;
            }

            if (!PasswordHasher.Verify(cu, user.MatKhauHash))
            {
                HienLoi("Mật khẩu hiện tại không chính xác.");
                return;
            }

            // Băm mật khẩu mới chuẩn PBKDF2
            var newHash = PasswordHasher.HashPassword(moi);
            user.MatKhauHash = newHash;
            user.NgayCapNhat = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Ghi vết nhật ký hệ thống qua thủ tục
            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy N'DoiMatKhau', {user.TenDangNhap}, {user.MaTaiKhoan}");
            }
            catch { /* bỏ qua nếu procedure chưa sẵn sàng */ }

            MessageBox.Show("Đổi mật khẩu thành công! Vui lòng ghi nhớ mật khẩu mới cho lần đăng nhập sau.",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            HienLoi($"Lỗi khi đổi mật khẩu: {ex.Message}");
        }
        finally
        {
            btnXacNhan.Enabled = true;
            btnXacNhan.Text = "CẬP NHẬT MẬT KHẨU";
        }
    }

    private void HienLoi(string thongBao)
    {
        lblThongBao.Text = "⚠️ " + thongBao;
        lblThongBao.Visible = true;
    }
}
