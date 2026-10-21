using DeathManagement.API.Data;
using DeathManagement.API.Entities;
using DeathManagement.API.Security;
using Microsoft.EntityFrameworkCore;
using static DeathManagement.API.Security.AuthorizationPolicies;

namespace DeathManagement.API.Forms;

public class FrmThemSuaNguoiDung : Form
{
    private readonly long? _userId;
    private readonly TextBox txtTenDangNhap = new() { Font = UITheme.FontRegular(10) };
    private readonly TextBox txtMatKhau = new() { Font = UITheme.FontRegular(10), UseSystemPasswordChar = true };
    private readonly TextBox txtHoTen = new() { Font = UITheme.FontRegular(10) };
    private readonly TextBox txtEmail = new() { Font = UITheme.FontRegular(10) };
    private readonly TextBox txtSoDienThoai = new() { Font = UITheme.FontRegular(10) };
    private readonly TextBox txtCCCD = new() { Font = UITheme.FontRegular(10) };
    private readonly TextBox txtDonVi = new() { Font = UITheme.FontRegular(10) };
    private readonly TextBox txtChucVu = new() { Font = UITheme.FontRegular(10) };
    private readonly ComboBox cboVaiTro = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly ComboBox cboTrangThai = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };

    private readonly Button btnLuu = new()
    {
        Text = "LƯU THÔNG TIN",
        Size = new Size(150, 38),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        Font = UITheme.FontBold(9),
        Cursor = Cursors.Hand
    };

    private readonly Button btnHuy = new()
    {
        Text = "HỦY BỎ",
        Size = new Size(100, 38),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = UITheme.Navy,
        Font = UITheme.FontBold(9),
        Cursor = Cursors.Hand
    };

    public FrmThemSuaNguoiDung(long? userId = null)
    {
        _userId = userId;
        Text = _userId == null ? "Thêm tài khoản người dùng mới" : "Cập nhật tài khoản người dùng";
        ClientSize = new Size(580, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        LoadRolesAndData();
    }

    private void BuildUI()
    {
        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.Navy };
        var lblTitle = new Label
        {
            Text = _userId == null ? "KHỞI TẠO TÀI KHOẢN NGƯỜI DÙNG" : "CẬP NHẬT THÔNG TIN TÀI KHOẢN",
            ForeColor = Color.White,
            Font = UITheme.FontBold(11),
            AutoSize = true,
            Location = new Point(20, 18)
        };
        pnlHeader.Controls.Add(lblTitle);

        // Body
        var pnlCard = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(540, 465),
            BackColor = Color.White
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        int y = 15;
        // Tên đăng nhập & Mật khẩu
        AddControlRow(pnlCard, "Tên đăng nhập *", txtTenDangNhap, 20, y, 230);
        AddControlRow(pnlCard, _userId == null ? "Mật khẩu khởi tạo *" : "Mật khẩu mới (để trống nếu không đổi)", txtMatKhau, 280, y, 230);
        y += 55;

        // Họ tên & Vai trò
        AddControlRow(pnlCard, "Họ và tên đầy đủ *", txtHoTen, 20, y, 230);
        AddControlRow(pnlCard, "Vai trò hệ thống *", cboVaiTro, 280, y, 230);
        y += 55;

        // Email & Số điện thoại
        AddControlRow(pnlCard, "Email liên hệ", txtEmail, 20, y, 230);
        AddControlRow(pnlCard, "Số điện thoại", txtSoDienThoai, 280, y, 230);
        y += 55;

        // CCCD & Trạng thái
        AddControlRow(pnlCard, "Số CCCD / Định danh", txtCCCD, 20, y, 230);
        AddControlRow(pnlCard, "Trạng thái tài khoản", cboTrangThai, 280, y, 230);
        y += 55;

        // Đơn vị & Chức vụ
        AddControlRow(pnlCard, "Đơn vị công tác", txtDonVi, 20, y, 230);
        AddControlRow(pnlCard, "Chức vụ", txtChucVu, 280, y, 230);
        y += 70;

        btnLuu.Location = new Point(20, y);
        btnLuu.FlatAppearance.BorderSize = 0;
        btnLuu.Click += async (s, e) => await XuLyLuu();

        btnHuy.Location = new Point(185, y);
        btnHuy.FlatAppearance.BorderColor = UITheme.Line;
        btnHuy.Click += (s, e) => Close();

        pnlCard.Controls.Add(btnLuu);
        pnlCard.Controls.Add(btnHuy);

        Controls.Add(pnlCard);
        Controls.Add(pnlHeader);

        // Nạp trạng thái
        cboTrangThai.Items.AddRange(new object[] { "HoatDong", "KhoaTam", "Khoa" });
        cboTrangThai.SelectedIndex = 0;
    }

    private void AddControlRow(Panel pnl, string label, Control ctrl, int x, int y, int width)
    {
        var lbl = new Label
        {
            Text = label,
            Font = UITheme.FontBold(8.5f),
            ForeColor = UITheme.Navy,
            Location = new Point(x, y),
            AutoSize = true
        };
        pnl.Controls.Add(lbl);

        ctrl.Location = new Point(x, y + 20);
        ctrl.Width = width;
        pnl.Controls.Add(ctrl);
    }

    private void LoadRolesAndData()
    {
        try
        {
            using var db = Program.NewDb();
            var roles = db.Roles.AsNoTracking().ToList();
            cboVaiTro.DataSource = roles;
            cboVaiTro.DisplayMember = "TenVaiTro";
            cboVaiTro.ValueMember = "MaVaiTro";

            if (_userId.HasValue)
            {
                txtTenDangNhap.ReadOnly = true;
                txtTenDangNhap.BackColor = UITheme.Soft;

                var u = db.Users.AsNoTracking().FirstOrDefault(x => x.MaTaiKhoan == _userId.Value);
                if (u != null)
                {
                    txtTenDangNhap.Text = u.TenDangNhap;
                    txtHoTen.Text = u.HoTen;
                    txtEmail.Text = u.Email ?? "";
                    txtSoDienThoai.Text = u.SoDienThoai ?? "";
                    txtCCCD.Text = u.CCCD ?? "";
                    txtDonVi.Text = u.DonVi ?? "";
                    txtChucVu.Text = u.ChucVu ?? "";
                    cboVaiTro.SelectedValue = u.MaVaiTro;
                    cboTrangThai.SelectedItem = u.TrangThai;

                    // Sửa chính mình: không cho đổi vai trò/trạng thái; sửa người khác: đổi vai trò cần PERMISSION:UPDATE
                    var laChinhMinh = u.MaTaiKhoan == AccessControl.UserId;
                    cboTrangThai.Enabled = !laChinhMinh;
                    cboVaiTro.Enabled = !laChinhMinh && AccessControl.Can(ResourcePermission, ActionUpdate);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi tải thông tin vai trò: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task XuLyLuu()
    {
        var tenDangNhap = txtTenDangNhap.Text.Trim();
        var hoTen = txtHoTen.Text.Trim();
        if (!PermissionUi.Check(this, ResourceAccount, _userId.HasValue ? ActionUpdate : ActionCreate)) return;
        var matKhau = txtMatKhau.Text;

        if (string.IsNullOrWhiteSpace(tenDangNhap))
        {
            MessageBox.Show("Tên đăng nhập không được để trống.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtTenDangNhap.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(hoTen))
        {
            MessageBox.Show("Họ tên không được để trống.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtHoTen.Focus();
            return;
        }

        if (!_userId.HasValue && string.IsNullOrEmpty(matKhau))
        {
            MessageBox.Show("Vui lòng nhập mật khẩu khởi tạo cho tài khoản mới.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.IsNullOrEmpty(matKhau) && matKhau.Length < 6)
        {
            MessageBox.Show("Mật khẩu phải có tối thiểu 6 ký tự.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = Program.NewDb();
            var maVaiTro = (int)cboVaiTro.SelectedValue!;
            var trangThai = cboTrangThai.SelectedItem?.ToString() ?? "HoatDong";

            var email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
            var sdt = string.IsNullOrWhiteSpace(txtSoDienThoai.Text) ? null : txtSoDienThoai.Text.Trim();
            var cccd = string.IsNullOrWhiteSpace(txtCCCD.Text) ? null : txtCCCD.Text.Trim();
            var idHienTai = _userId ?? 0;
            if (email != null && await db.Users.AnyAsync(x => x.Email == email && x.MaTaiKhoan != idHienTai))
            { MessageBox.Show("Email đã được sử dụng bởi tài khoản khác.", "Trùng lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (sdt != null && await db.Users.AnyAsync(x => x.SoDienThoai == sdt && x.MaTaiKhoan != idHienTai))
            { MessageBox.Show("Số điện thoại đã được sử dụng bởi tài khoản khác.", "Trùng lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cccd != null && await db.Users.AnyAsync(x => x.CCCD == cccd && x.MaTaiKhoan != idHienTai))
            { MessageBox.Show("CCCD đã được sử dụng bởi tài khoản khác.", "Trùng lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!_userId.HasValue)
            {
                // Thêm mới
                if (await db.Users.AnyAsync(x => x.TenDangNhap == tenDangNhap))
                {
                    MessageBox.Show("Tên đăng nhập này đã tồn tại trong hệ thống.", "Trùng lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var newUser = new User
                {
                    TenDangNhap = tenDangNhap,
                    MatKhauHash = PasswordHasher.HashPassword(matKhau),
                    HoTen = hoTen,
                    Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    SoDienThoai = string.IsNullOrWhiteSpace(txtSoDienThoai.Text) ? null : txtSoDienThoai.Text.Trim(),
                    CCCD = string.IsNullOrWhiteSpace(txtCCCD.Text) ? null : txtCCCD.Text.Trim(),
                    DonVi = string.IsNullOrWhiteSpace(txtDonVi.Text) ? null : txtDonVi.Text.Trim(),
                    ChucVu = string.IsNullOrWhiteSpace(txtChucVu.Text) ? null : txtChucVu.Text.Trim(),
                    MaVaiTro = maVaiTro,
                    TrangThai = trangThai,
                    NgayTao = DateTime.UtcNow,
                    NgayCapNhat = DateTime.UtcNow
                };

                db.Users.Add(newUser);
                await db.SaveChangesAsync();

                MessageBox.Show("Thêm người dùng mới thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Sửa
                var user = await db.Users.FirstOrDefaultAsync(x => x.MaTaiKhoan == _userId.Value);
                if (user == null)
                {
                    MessageBox.Show("Không tìm thấy người dùng cần sửa.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                user.HoTen = hoTen;
                user.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                user.SoDienThoai = string.IsNullOrWhiteSpace(txtSoDienThoai.Text) ? null : txtSoDienThoai.Text.Trim();
                user.CCCD = string.IsNullOrWhiteSpace(txtCCCD.Text) ? null : txtCCCD.Text.Trim();
                user.DonVi = string.IsNullOrWhiteSpace(txtDonVi.Text) ? null : txtDonVi.Text.Trim();
                user.ChucVu = string.IsNullOrWhiteSpace(txtChucVu.Text) ? null : txtChucVu.Text.Trim();
                user.MaVaiTro = maVaiTro;
                user.TrangThai = trangThai;
                user.NgayCapNhat = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(matKhau))
                {
                    user.MatKhauHash = PasswordHasher.HashPassword(matKhau);
                }

                await db.SaveChangesAsync();
                MessageBox.Show("Cập nhật thông tin tài khoản thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
