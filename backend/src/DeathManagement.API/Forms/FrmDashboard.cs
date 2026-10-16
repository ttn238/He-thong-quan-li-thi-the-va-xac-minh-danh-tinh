using System.Drawing.Drawing2D;
using DeathManagement.API.Data;
using DeathManagement.API.Security;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Forms;

public class FrmDashboard : Form
{
    private readonly Label lblTongUser = new() { AutoSize = true, Font = UITheme.FontHeading(16), ForeColor = UITheme.Navy };
    private readonly Label lblUserHoatDong = new() { AutoSize = true, Font = UITheme.FontHeading(16), ForeColor = UITheme.Green };
    private readonly Label lblUserKhoa = new() { AutoSize = true, Font = UITheme.FontHeading(16), ForeColor = UITheme.Red };
    private readonly Label lblTongRole = new() { AutoSize = true, Font = UITheme.FontHeading(16), ForeColor = UITheme.Blue };

    public FrmDashboard()
    {
        Text = "Hệ thống Quản lý Thi thể & Xác minh Danh tính - Bàn làm việc";
        ClientSize = new Size(1180, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        LoadThongKe();
    }

    private void BuildUI()
    {
        // 1. Top Navigation Bar (Chuẩn tone màu FrmLogin: Navy #0f2b4d)
        var pnlTopNav = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UITheme.Navy };

        // Logo bên trái
        var pnlLogo = new Panel { Size = new Size(54, 54), Location = new Point(16, 5), BackColor = Color.Transparent };
        pnlLogo.Paint += DrawLogo;

        var lblBrand = new Label
        {
            Text = "HỆ THỐNG QUẢN LÝ THI THỂ VÀ XÁC MINH DANH TÍNH",
            Font = UITheme.FontBold(11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(78, 14)
        };
        var lblSub = new Label
        {
            Text = "VIETNAM DISASTER VICTIM IDENTIFICATION & BODY RECOVERY INFORMATION SYSTEM",
            Font = UITheme.FontRegular(7.5f),
            ForeColor = UITheme.Gold,
            AutoSize = true,
            Location = new Point(78, 36)
        };
        pnlTopNav.Controls.AddRange(new Control[] { pnlLogo, lblBrand, lblSub });

        // Thông tin người đăng nhập bên phải
        var pnlUserRight = new Panel { Dock = DockStyle.Right, Width = 380, BackColor = Color.Transparent };
        var tk = Session.TaiKhoan;
        var lblUser = new Label
        {
            Text = $"👤 {tk?.HoTen} [{tk?.TenVaiTro}]",
            Font = UITheme.FontBold(9),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, 14)
        };
        var lblDonVi = new Label
        {
            Text = $"📍 {Session.DonVi}",
            Font = UITheme.FontRegular(8),
            ForeColor = UITheme.Line,
            AutoSize = true,
            Location = new Point(10, 36)
        };

        var btnDangXuat = new Button
        {
            Text = "ĐĂNG XUẤT",
            Size = new Size(95, 32),
            Location = new Point(270, 16),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorTranslator.FromHtml("#c0392b"),
            ForeColor = Color.White,
            Font = UITheme.FontBold(8),
            Cursor = Cursors.Hand
        };
        btnDangXuat.FlatAppearance.BorderSize = 0;
        btnDangXuat.Click += (s, e) =>
        {
            Session.Clear();
            DialogResult = DialogResult.Retry;
            Close();
        };

        pnlUserRight.Controls.AddRange(new Control[] { lblUser, lblDonVi, btnDangXuat });
        pnlTopNav.Controls.Add(pnlUserRight);

        // 2. Menu Điều hướng (MenuStrip chuẩn WinForms MDI/Navigation)
        var menuStrip = new MenuStrip
        {
            BackColor = Color.White,
            Font = UITheme.FontBold(9),
            Dock = DockStyle.Top,
            Padding = new Padding(15, 6, 0, 6)
        };
        menuStrip.Renderer = new ToolStripProfessionalRenderer(new CustomColorTable());

        var mnuHeThong = new ToolStripMenuItem("⚙ HỆ THỐNG");
        var mnuDoiMatKhau = new ToolStripMenuItem("Thay đổi mật khẩu...", null, (s, e) =>
        {
            using var f = new FrmDoiMatKhau();
            f.ShowDialog(this);
        });
        var mnuDangXuat = new ToolStripMenuItem("Đăng xuất tài khoản", null, (s, e) =>
        {
            Session.Clear();
            DialogResult = DialogResult.Retry;
            Close();
        });
        mnuHeThong.DropDownItems.AddRange(new ToolStripItem[] { mnuDoiMatKhau, new ToolStripSeparator(), mnuDangXuat });

        var mnuQuanTri = new ToolStripMenuItem("👥 QUẢN TRỊ NGƯỜI DÙNG & VAI TRÒ");
        var mnuUsers = new ToolStripMenuItem("Danh sách tài khoản & Phân quyền Role (3.3 / 4.5)...", null, (s, e) =>
        {
            using var f = new FrmQuanLyNguoiDung();
            f.ShowDialog(this);
            LoadThongKe();
        });
        mnuQuanTri.DropDownItems.Add(mnuUsers);

        // Kiểm tra phân quyền: Chỉ ADMIN mới thấy menu Quản trị
        var laAdmin = tk != null && (tk.TenVaiTro.Contains("Quản trị") || tk.TenVaiTro.Contains("ADMIN"));
        mnuQuanTri.Visible = laAdmin;

        var mnuNghiepVu = new ToolStripMenuItem("📋 QUẢN LÝ HỒ SƠ");
        mnuNghiepVu.DropDownItems.Add(new ToolStripMenuItem("Tiếp nhận tin báo sự việc...", null, (s, e) =>
        {
            using var f = new FrmTiepNhanTinBao();
            f.ShowDialog(this);
        }));
        mnuNghiepVu.DropDownItems.Add(new ToolStripMenuItem("Hồ sơ người mất tích..."));
        mnuNghiepVu.DropDownItems.Add(new ToolStripMenuItem("Đối chiếu & Xác minh danh tính..."));

        var mnuTroGiup = new ToolStripMenuItem("ℹ TRỢ GIÚP");
        mnuTroGiup.DropDownItems.Add(new ToolStripMenuItem("Thông tin phần mềm DVI Vietnam"));

        menuStrip.Items.AddRange(new ToolStripItem[] { mnuHeThong, mnuQuanTri, mnuNghiepVu, mnuTroGiup });

        // 3. Main Dashboard Body
        var pnlMainBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25), BackColor = UITheme.Bg };

        // Quick Stats Cards Panel
        var pnlCards = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = Color.Transparent };
        int cardWidth = 260;
        int gap = 20;

        pnlCards.Controls.Add(CreateStatCard("TỔNG TÀI KHOẢN", lblTongUser, "Toàn hệ thống", UITheme.Navy, 0 * (cardWidth + gap)));
        pnlCards.Controls.Add(CreateStatCard("ĐANG HOẠT ĐỘNG", lblUserHoatDong, "Được phép đăng nhập", UITheme.Green, 1 * (cardWidth + gap)));
        pnlCards.Controls.Add(CreateStatCard("ĐANG BỊ KHÓA", lblUserKhoa, "Khóa tạm / vô hiệu hóa", UITheme.Red, 2 * (cardWidth + gap)));
        pnlCards.Controls.Add(CreateStatCard("DANH MỤC VAI TRÒ", lblTongRole, "Cấu hình phân quyền", UITheme.Blue, 3 * (cardWidth + gap)));

        // Action Center Panel
        var pnlActionCenter = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 20, 0, 0)
        };
        pnlActionCenter.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlActionCenter.Width - 1, pnlActionCenter.Height - 1);
        };

        var lblActionTitle = new Label
        {
            Text = "TRUNG TÂM ĐIỀU HÀNH & TRUY CẬP NHANH CHỨC NĂNG",
            Font = UITheme.FontBold(10.5f),
            ForeColor = UITheme.Navy,
            Location = new Point(25, 20),
            AutoSize = true
        };

        var pnlButtons = new FlowLayoutPanel
        {
            Location = new Point(25, 60),
            Size = new Size(1100, 420),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        if (laAdmin)
        {
            pnlButtons.Controls.Add(CreateQuickActionButton("👥 QUẢN LÝ TÀI KHOẢN & PHÂN QUYỀN",
                "Quản lý người dùng, gán vai trò, kiểm soát trạng thái tài khoản (Mục 3.3 / 4.5)", UITheme.Navy, () =>
                {
                    using var f = new FrmQuanLyNguoiDung();
                    f.ShowDialog(this);
                    LoadThongKe();
                }));
        }

        pnlButtons.Controls.Add(CreateQuickActionButton("🔑 THAY ĐỔI MẬT KHẨU",
            "Cập nhật mật khẩu bảo mật cá nhân theo thuật toán mã hóa PBKDF2", UITheme.Blue, () =>
            {
                using var f = new FrmDoiMatKhau();
                f.ShowDialog(this);
            }));

        pnlButtons.Controls.Add(CreateQuickActionButton("📁 TIẾP NHẬN TỬ THI (BODY)",
            "Lập hồ sơ thi thể tiếp nhận, ghi nhận đặc điểm nhận dạng & hiện trường", ColorTranslator.FromHtml("#34495e"), () =>
            {
                MessageBox.Show("Chức năng Tiếp nhận tử thi đang sẵn sàng kết nối.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));

        pnlButtons.Controls.Add(CreateQuickActionButton("🔍 ĐỐI CHIẾU DANH TÍNH (AI MATCHING)",
            "Chạy thuật toán so khớp đa tiêu chí giữa người mất tích và thi thể", ColorTranslator.FromHtml("#27ae60"), () =>
            {
                MessageBox.Show("Chức năng Đối chiếu danh tính AI đang sẵn sàng kết nối.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));

        pnlActionCenter.Controls.Add(lblActionTitle);
        pnlActionCenter.Controls.Add(pnlButtons);

        pnlMainBody.Controls.Add(pnlActionCenter);
        pnlMainBody.Controls.Add(pnlCards);

        // Add All
        Controls.Add(pnlMainBody);
        Controls.Add(menuStrip);
        Controls.Add(pnlTopNav);
    }

    private Panel CreateStatCard(string title, Label lblValue, string sub, Color accentColor, int left)
    {
        var pnl = new Panel
        {
            Location = new Point(left, 0),
            Size = new Size(260, 95),
            BackColor = Color.White
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            using var brush = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(brush, 0, 0, 5, pnl.Height);
        };

        var lblT = new Label
        {
            Text = title,
            Font = UITheme.FontBold(8),
            ForeColor = UITheme.Muted,
            Location = new Point(16, 12),
            AutoSize = true
        };
        lblValue.Location = new Point(14, 32);
        lblValue.Text = "...";

        var lblS = new Label
        {
            Text = sub,
            Font = UITheme.FontRegular(7.5f),
            ForeColor = Color.Gray,
            Location = new Point(16, 68),
            AutoSize = true
        };

        pnl.Controls.AddRange(new Control[] { lblT, lblValue, lblS });
        return pnl;
    }

    private Button CreateQuickActionButton(string title, string desc, Color btnColor, Action onClick)
    {
        var btn = new Button
        {
            Size = new Size(340, 110),
            Margin = new Padding(0, 0, 20, 20),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Soft,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(15, 12, 15, 12)
        };
        btn.FlatAppearance.BorderColor = UITheme.Line;

        var lblT = new Label
        {
            Text = title,
            Font = UITheme.FontBold(9.5f),
            ForeColor = btnColor,
            AutoSize = true,
            Location = new Point(14, 12)
        };
        var lblD = new Label
        {
            Text = desc,
            Font = UITheme.FontRegular(8.5f),
            ForeColor = UITheme.Muted,
            Size = new Size(310, 50),
            Location = new Point(14, 40)
        };

        btn.Controls.Add(lblT);
        btn.Controls.Add(lblD);

        lblT.Click += (s, e) => onClick();
        lblD.Click += (s, e) => onClick();
        btn.Click += (s, e) => onClick();

        btn.MouseEnter += (s, e) => { btn.BackColor = ColorTranslator.FromHtml("#e8f0fe"); btn.FlatAppearance.BorderColor = UITheme.Blue; };
        btn.MouseLeave += (s, e) => { btn.BackColor = UITheme.Soft; btn.FlatAppearance.BorderColor = UITheme.Line; };

        return btn;
    }

    private void LoadThongKe()
    {
        try
        {
            using var db = Program.NewDb();
            var total = db.Users.Count();
            var active = db.Users.Count(x => x.TrangThai == "HoatDong");
            var locked = db.Users.Count(x => x.TrangThai != "HoatDong");
            var roles = db.Roles.Count();

            lblTongUser.Text = total.ToString();
            lblUserHoatDong.Text = active.ToString();
            lblUserKhoa.Text = locked.ToString();
            lblTongRole.Text = roles.ToString();
        }
        catch (Exception ex)
        {
            lblTongUser.Text = "!";
            Console.WriteLine(ex.Message);
        }
    }

    private void DrawLogo(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(50f / 64, 50f / 64);

        using var navy = new SolidBrush(UITheme.Navy);
        using var gold = new SolidBrush(UITheme.Gold);
        using var whitePen = new Pen(Color.White, 1.6f);

        g.FillPolygon(navy, Pts("32,9 51,16 51,30 46,42 32,53 18,42 13,30 13,16"));
        g.DrawPolygon(whitePen, Pts("32,15 45,20 45,30 41,39 32,47 23,39 19,30 19,20"));
        g.FillPolygon(gold, Star());
    }

    static PointF[] Pts(string s) => s.Split(' ')
        .Select(p => p.Split(','))
        .Select(a => new PointF(float.Parse(a[0]), float.Parse(a[1])))
        .ToArray();

    static PointF[] Star() => Enumerable.Range(0, 10).Select(i =>
    {
        double a = -Math.PI / 2 + i * Math.PI / 5;
        double r = i % 2 == 0 ? 9 : 3.8;
        return new PointF((float)(32 + r * Math.Cos(a)), (float)(31 + r * Math.Sin(a)));
    }).ToArray();

    private class CustomColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => ColorTranslator.FromHtml("#e8f0fe");
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuBorder => UITheme.Line;
    }
}