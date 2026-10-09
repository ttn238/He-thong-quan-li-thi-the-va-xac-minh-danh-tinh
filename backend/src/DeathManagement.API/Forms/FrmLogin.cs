using System.Drawing.Drawing2D;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;

namespace DeathManagement.API.Forms;

// Thay cho frontend/src/pages/auth/Login.tsx (+ index.css)
public class FrmLogin : Form
{
    // ===== Màu & dữ liệu lấy từ index.css / Login.tsx =====
    static readonly Color Navy = ColorTranslator.FromHtml("#0f2b4d");
    static readonly Color Blue = ColorTranslator.FromHtml("#1f5fae");
    static readonly Color Muted = ColorTranslator.FromHtml("#7a8699");
    static readonly Color Line = ColorTranslator.FromHtml("#d9e0ea");
    static readonly Color Bg = ColorTranslator.FromHtml("#eef2f7");
    static readonly Color Soft = ColorTranslator.FromHtml("#f6f8fb");
    static readonly Color Red = ColorTranslator.FromHtml("#c0392b");
    static readonly Color Green = ColorTranslator.FromHtml("#1fa971");
    static readonly Pen LinePen = new(Line);

    static readonly string[] DonVi =
    {
        "Trung tâm Giám định Pháp y Quốc gia - Phòng Hồ sơ DVI",
        "Công an TP. Thủ Đức",
        "Công an Quận 1",
        "Nhà xác Bệnh viện Chợ Rẫy",
    };

    // Thay cho localStorage 'qltldt.lastUser'
    static readonly string LastUserFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QLTLDT", "lastUser.txt");

    const string TextNutDangNhap = "XÁC THỰC VÀ ĐĂNG NHẬP";

    static Font F(float size, FontStyle style = FontStyle.Regular, string name = "Segoe UI") => new(name, size, style);

    // ===== Các control cần dùng trong xử lý =====
    readonly TextBox txtUser = new() { BorderStyle = BorderStyle.None, Font = F(10), PlaceholderText = "cstt.admin_dvi" };
    readonly TextBox txtPass = new() { BorderStyle = BorderStyle.None, Font = F(10), PlaceholderText = "••••••••••••", UseSystemPasswordChar = true };
    readonly ComboBox cboDonVi = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = F(9) };
    readonly CheckBox chkNho = new() { Text = "Ghi nhớ phiên làm việc trên máy trạm", Checked = true, AutoSize = true, ForeColor = Navy, Font = F(8.25f) };
    readonly Label lblLoi = new()
    {
        Size = new Size(404, 38),
        BackColor = ColorTranslator.FromHtml("#fdecea"),
        ForeColor = Red,
        Font = F(8.5f),
        Padding = new Padding(10, 0, 10, 0),
        TextAlign = ContentAlignment.MiddleLeft,
        Visible = false,
    };
    readonly Button btnLogin = new()
    {
        Text = TextNutDangNhap,
        Size = new Size(404, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = ColorTranslator.FromHtml("#0f2b4d"),
        ForeColor = Color.White,
        Font = F(9.5f, FontStyle.Bold),
        Cursor = Cursors.Hand,
    };

    public FrmLogin()
    {
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Cổng Đăng Nhập Hệ Thống Quản Trị";
        Font = F(9);
        BackColor = Bg;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1000, 780);
        MinimumSize = new Size(720, 420);

        cboDonVi.Items.AddRange(DonVi);
        cboDonVi.SelectedIndex = 0;
        txtUser.Text = DocTenDangNhap();

        btnLogin.FlatAppearance.BorderColor = Navy;
        btnLogin.Click += BtnLogin_Click;
        AcceptButton = btnLogin;                       // nhấn Enter = đăng nhập

        // Vùng giữa: có thanh cuộn nếu màn hình thấp (giống trang web cuộn xuống)
        var main = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 0, 0, 20) };
        var card = BuildCard();
        main.Controls.Add(card);
        main.Resize += (_, _) => card.Left = Math.Max(0, (main.ClientSize.Width - card.Width) / 2);

        // Thứ tự add: Fill trước, Top/Bottom sau (để dock đúng)
        Controls.Add(main);
        Controls.Add(BuildHeader());
        Controls.Add(BuildFooter());

        Load += (_, _) =>
        {
            var wa = Screen.FromControl(this).WorkingArea;
            if (Height > wa.Height) { Height = wa.Height; Top = wa.Top; }
            card.Left = Math.Max(0, (main.ClientSize.Width - card.Width) / 2);
        };
        Shown += (_, _) => (txtUser.Text.Length == 0 ? txtUser : txtPass).Focus();
    }

    // ================= XỬ LÝ ĐĂNG NHẬP (thay cho submit() + AuthController) =================
    async void BtnLogin_Click(object? sender, EventArgs e)
    {
        var user = txtUser.Text.Trim();
        if (user.Length == 0 || txtPass.Text.Length == 0)
        {
            HienLoi("Vui lòng nhập tên đăng nhập và mật khẩu.");
            return;
        }

        btnLogin.Enabled = false;
        btnLogin.Text = "ĐANG XÁC THỰC...";
        lblLoi.Visible = false;
        try
        {
            using var db = Program.NewDb();
            var (ket, tk) = await new AuthService(db).DangNhapAsync(user, txtPass.Text);

            if (ket == KetQuaDangNhap.ThanhCong)
            {
                Session.TaiKhoan = tk;
                Session.DonVi = cboDonVi.Text;
                LuuTenDangNhap(chkNho.Checked ? tk!.TenDangNhap : null);
                DialogResult = DialogResult.OK;        // đóng form -> Program mở FrmDashboard
            }
            else if (ket == KetQuaDangNhap.TaiKhoanKhoa)
                HienLoi("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên để được mở khóa.");
            else
                HienLoi("Sai tên đăng nhập hoặc mật khẩu.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);    // xem chi tiết lỗi ở cửa sổ Output của Visual Studio
            HienLoi("Không kết nối được cơ sở dữ liệu.");
        }
        finally
        {
            btnLogin.Enabled = true;
            btnLogin.Text = TextNutDangNhap;
        }
    }

    void HienLoi(string noiDung)
    {
        lblLoi.Text = noiDung;
        lblLoi.Visible = true;
    }

    static string DocTenDangNhap()
    {
        try { return File.Exists(LastUserFile) ? File.ReadAllText(LastUserFile).Trim() : ""; }
        catch { return ""; }
    }

    static void LuuTenDangNhap(string? ten)
    {
        try
        {
            if (ten is null) { File.Delete(LastUserFile); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(LastUserFile)!);
            File.WriteAllText(LastUserFile, ten);
        }
        catch { /* không lưu được thì bỏ qua */ }
    }

    // ================= DỰNG GIAO DIỆN =================
    FlowLayoutPanel BuildCard()
    {
        var card = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
            Top = 20,
        };
        card.Paint += (_, e) => e.Graphics.DrawRectangle(LinePen, 0, 0, card.Width - 1, card.Height - 1);

        // Thêm control vào thẻ, cách lề trái/phải 28px như .lg-body
        void Add(Control c, int top = 0, int bottom = 0)
        {
            c.Margin = new Padding(28, top, 28, bottom);
            card.Controls.Add(c);
        }

        // Thanh tiêu đề xanh
        card.Controls.Add(new Label
        {
            Text = "Cổng Đăng Nhập Hệ Thống Quản Trị - Phiên bản 4.2.0 - PROD",
            Size = new Size(460, 32),
            BackColor = Navy,
            ForeColor = Color.White,
            Font = F(8.25f, FontStyle.Bold),
            Padding = new Padding(12, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
        });

        // Logo
        var logo = new Panel { Size = new Size(56, 56), Margin = new Padding(202, 24, 202, 8) };
        logo.Paint += DrawLogo;
        card.Controls.Add(logo);

        Add(new Label
        {
            Text = "HỆ THỐNG QUẢN LÝ THI THỂ VÀ XÁC MINH DANH TÍNH",
            Size = new Size(404, 44),
            Font = F(11, FontStyle.Bold),
            ForeColor = Navy,
            TextAlign = ContentAlignment.MiddleCenter,
        });
        Add(new Label
        {
            Text = "Phần mềm quản lý hồ sơ tử thi, giám định ADN và quy trình lưu ký quốc gia (DVI)",
            Size = new Size(404, 34),
            Font = F(8.5f),
            ForeColor = Blue,
            TextAlign = ContentAlignment.MiddleCenter,
        }, 6);

        Add(lblLoi, 6);                                // dòng báo lỗi (ẩn cho tới khi có lỗi)

        Add(FieldLabel("TÊN ĐĂNG NHẬP / MÃ CÔNG VỤ", "Mã cán bộ / Thẻ ngành", true), 14, 4);
        Add(Box("👤", txtUser));

        Add(FieldLabel("MẬT KHẨU BẢO MẬT", "Cấp phát bởi BCA / BYT", true), 14, 4);
        var boxPass = Box("🔒", txtPass, 34);
        var eye = new Button
        {
            Text = "👁",
            Font = F(9, FontStyle.Regular, "Segoe UI Emoji"),
            ForeColor = Muted,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Bounds = new Rectangle(404 - 35, 1, 34, 34),
        };
        eye.FlatAppearance.BorderSize = 0;
        eye.Click += (_, _) => txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;   // hiện/ẩn mật khẩu
        boxPass.Controls.Add(eye);
        Add(boxPass);

        Add(FieldLabel("ĐƠN VỊ / CHI CỤC LƯU HỒ SƠ", null, false), 14, 4);
        Add(Box("🏢", cboDonVi));

        Add(BuildRememberRow(), 12);
        Add(btnLogin, 14);

        var pki = new Button
        {
            Text = "ĐĂNG NHẬP BẰNG THẺ PKI / TOKEN CHUYÊN DỤNG (USB CRYPTO)",
            Size = new Size(404, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorTranslator.FromHtml("#f3f6fa"),
            ForeColor = Navy,
            Font = F(7.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        pki.FlatAppearance.BorderColor = Line;
        pki.Click += (_, _) => MessageBox.Show("Chức năng đăng nhập bằng thẻ PKI / USB Token chưa được triển khai.");
        Add(pki, 10);

        Add(BuildWarning(), 10);
        Add(BuildHelpRow(), 10, 14);

        var status = BuildStatus();
        status.Margin = Padding.Empty;
        card.Controls.Add(status);

        return card;
    }

    // Nhãn phía trên ô nhập: chữ đậm bên trái, gợi ý nhỏ bên phải
    static Panel FieldLabel(string text, string? hint, bool required)
    {
        var p = new Panel { Size = new Size(404, 18) };
        var lbl = new Label { Text = text, AutoSize = true, Font = F(8, FontStyle.Bold), ForeColor = Navy, Location = new Point(0, 0) };
        p.Controls.Add(lbl);
        if (required)
            p.Controls.Add(new Label { Text = "*", AutoSize = true, Font = F(8, FontStyle.Bold), ForeColor = Red, Location = new Point(lbl.PreferredWidth - 4, 0) });
        if (hint != null)
            p.Controls.Add(new Label { Text = hint, Font = F(7.5f), ForeColor = Muted, TextAlign = ContentAlignment.TopRight, Bounds = new Rectangle(244, 1, 160, 16) });
        return p;
    }

    // Ô nhập có viền + biểu tượng bên trái (.lg-input)
    static Panel Box(string icon, Control c, int rightReserve = 0)
    {
        var p = new Panel { Size = new Size(404, 36), BackColor = Color.White };
        p.Paint += (_, e) => e.Graphics.DrawRectangle(LinePen, 0, 0, p.Width - 1, p.Height - 1);

        p.Controls.Add(new Label
        {
            Text = icon,
            Font = F(10, FontStyle.Regular, "Segoe UI Emoji"),
            ForeColor = Muted,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(1, 1, 33, 34),
        });

        c.Left = 36;
        c.Width = 404 - 36 - 4 - rightReserve;
        c.Top = (36 - c.Height) / 2;
        p.Controls.Add(c);
        return p;
    }

    Panel BuildRememberRow()
    {
        var p = new Panel { Size = new Size(404, 24) };
        chkNho.Location = new Point(0, 2);

        var lnk = new LinkLabel
        {
            Text = "Quên mật khẩu ?",
            AutoSize = true,
            Font = F(8.5f),
            LinkColor = Muted,
            ActiveLinkColor = Blue,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Top = 4,
        };
        lnk.Left = 404 - lnk.PreferredWidth;
        lnk.LinkClicked += (_, _) => MessageBox.Show("Vui lòng liên hệ quản trị viên để được cấp lại mật khẩu.");

        p.Controls.Add(chkNho);
        p.Controls.Add(lnk);
        return p;
    }

    static Panel BuildWarning()
    {
        var tint = ColorTranslator.FromHtml("#3b5d8a");
        var p = new Panel { Size = new Size(404, 78), BackColor = ColorTranslator.FromHtml("#eaf1fb") };
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(ColorTranslator.FromHtml("#d3e1f5"));
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        p.Controls.Add(new Label { Text = "🛡", Font = F(11, FontStyle.Regular, "Segoe UI Emoji"), ForeColor = tint, Bounds = new Rectangle(8, 6, 26, 24) });
        p.Controls.Add(new Label { Text = "CẢNH BÁO BẢO MẬT PHÁP LÝ:", Font = F(8, FontStyle.Bold), ForeColor = Navy, Bounds = new Rectangle(36, 6, 360, 16) });
        p.Controls.Add(new Label
        {
            Text = "Hệ thống lưu trữ dữ liệu nhân thân và nhận dạng cấp quốc gia. Mọi hành vi truy cập trái phép " +
                   "hoặc sao chép hồ sơ lưu ký từ thi thể đều bị ghi nhật ký và xử lý theo Luật Bảo vệ bí mật nhà nước.",
            Font = F(8),
            ForeColor = tint,
            Bounds = new Rectangle(36, 22, 360, 54),
        });
        return p;
    }

    static Panel BuildHelpRow()
    {
        var p = new Panel { Size = new Size(404, 18) };
        p.Controls.Add(new Label { Text = "Trực ban giám sát: 1900-0909 (Nhánh 2)", Font = F(7.5f), ForeColor = Muted, Bounds = new Rectangle(0, 0, 220, 16) });
        p.Controls.Add(new Label { Text = "Tài liệu quy chuẩn (PDF)", Font = F(7.5f), ForeColor = Blue, TextAlign = ContentAlignment.TopRight, Bounds = new Rectangle(244, 0, 160, 16) });
        return p;
    }

    static Panel BuildStatus()
    {
        var p = new Panel { Size = new Size(460, 30), BackColor = Soft };
        p.Paint += (_, e) => e.Graphics.DrawLine(LinePen, 0, 0, p.Width, 0);
        var mono = F(6.5f, FontStyle.Regular, "Consolas");
        p.Controls.Add(new Label { Text = "TLS 1.3 / AES-256 GCM | MÁY TRẠM: 10.24.88.192 (LAN AN NINH)", Font = mono, ForeColor = Muted, Bounds = new Rectangle(12, 8, 330, 16) });
        p.Controls.Add(new Label { Text = "PHIÊN: SEC-VN-9982", Font = mono, ForeColor = Muted, TextAlign = ContentAlignment.TopRight, Bounds = new Rectangle(342, 8, 106, 16) });
        return p;
    }

    // Thanh trên cùng
    static Panel BuildHeader()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.White };
        p.Paint += (_, e) => e.Graphics.DrawLine(LinePen, 0, p.Height - 1, p.Width, p.Height - 1);

        var mono = F(7.5f, FontStyle.Bold, "Consolas");
        var left = new Label { Text = "SECURE-GATEWAY // DVI-NATIONAL-IDENTIFICATION-TERMINAL", AutoSize = true, Font = mono, ForeColor = Navy, Location = new Point(20, 8) };
        var right = new Label { Text = "● TRẠM TRỰC TUYẾN · NODE: VN-HAN-DVI-04", AutoSize = true, Font = mono, ForeColor = Green, Top = 8 };
        p.Resize += (_, _) =>
        {
            right.Left = p.Width - right.Width - 20;
            p.Invalidate();
        };

        var nav = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Location = new Point(20, 32) };
        string[] menu = { "Hệ thống", "Giám định hồ sơ", "Đồng bộ ADN & Sinh trắc", "Tra cứu chuỗi lưu ký", "Báo cáo & Thống kê", "Trợ giúp (F1)" };
        for (int i = 0; i < menu.Length; i++)
            nav.Controls.Add(new Label
            {
                Text = menu[i],
                UseMnemonic = false,                   // để dấu & hiện ra đúng
                AutoSize = true,
                Margin = new Padding(0, 0, 20, 0),
                Font = F(8.25f, i == 0 ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = i == 0 ? Navy : Muted,
            });

        p.Controls.Add(left);
        p.Controls.Add(right);
        p.Controls.Add(nav);
        return p;
    }

    // Thanh dưới cùng
    static Panel BuildFooter()
    {
        var p = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.White };
        p.Paint += (_, e) => e.Graphics.DrawLine(LinePen, 0, 0, p.Width, 0);

        var left = new Label
        {
            Text = "● CƠ QUAN CHỦ QUẢN: LIÊN BỘ Y TẾ & BỘ CÔNG AN VIỆT NAM",
            UseMnemonic = false,
            AutoSize = true,
            Font = F(8, FontStyle.Bold, "Consolas"),
            ForeColor = Muted,
            Location = new Point(20, 9),
        };
        var right = new Label
        {
            Text = "TRUNG TÂM DỮ LIỆU ĐỊNH DANH NẠN NHÂN THIÊN TAI VÀ THẢM HỌA (DVI VIETNAM)",
            AutoSize = true,
            Font = F(8),
            ForeColor = Muted,
            Top = 9,
        };
        p.Resize += (_, _) =>
        {
            right.Left = p.Width - right.Width - 20;
            p.Invalidate();
        };

        p.Controls.Add(left);
        p.Controls.Add(right);
        return p;
    }

    // ===== Logo =====
    static void DrawLogo(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(56f / 64, 56f / 64);

        using var bg = new SolidBrush(Bg);
        using var navy = new SolidBrush(Navy);
        using var gold = new SolidBrush(ColorTranslator.FromHtml("#f2c94c"));
        using var whitePen = new Pen(Color.White, 1.6f);

        g.FillRectangle(bg, 0, 0, 64, 64);
        g.FillPolygon(navy, Pts("32,9 51,16 51,30 46,42 32,53 18,42 13,30 13,16"));
        g.DrawPolygon(whitePen, Pts("32,15 45,20 45,30 41,39 32,47 23,39 19,30 19,20"));
        g.FillPolygon(gold, Star());
    }

    static PointF[] Pts(string s) => s.Split(' ')
        .Select(p => p.Split(','))
        .Select(a => new PointF(float.Parse(a[0]), float.Parse(a[1])))
        .ToArray();

    // Ngôi sao 5 cánh 
    static PointF[] Star() => Enumerable.Range(0, 10).Select(i =>
    {
        double a = -Math.PI / 2 + i * Math.PI / 5;
        double r = i % 2 == 0 ? 9 : 3.8;
        return new PointF((float)(32 + r * Math.Cos(a)), (float)(31 + r * Math.Sin(a)));
    }).ToArray();
}