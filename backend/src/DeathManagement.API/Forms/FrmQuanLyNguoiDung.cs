using System.Data;
using DeathManagement.API.Common;
using DeathManagement.API.Data;
using DeathManagement.API.Entities;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Forms;

public class FrmQuanLyNguoiDung : Form
{
    private readonly DataGridView dgvUsers = new()
    {
        Dock = DockStyle.Fill,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        RowHeadersVisible = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private readonly TextBox txtTimKiem = new() { Font = UITheme.FontRegular(10), PlaceholderText = "Tìm theo tên đăng nhập, họ tên, email, CCCD..." };
    private readonly ComboBox cboLocVaiTro = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly ComboBox cboLocTrangThai = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly Label lblTongSo = new() { AutoSize = true, Font = UITheme.FontBold(9.5f), ForeColor = UITheme.Navy };

    // Quick role assignment controls
    private readonly ComboBox cboGanVaiTro = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f), Width = 200 };
    private readonly Button btnXacNhanGanRole = new()
    {
        Text = "GÁN VAI TRÒ",
        Size = new Size(120, 32),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Blue,
        ForeColor = Color.White,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };

    public FrmQuanLyNguoiDung()
    {
        Text = "Quản lý Người dùng & Phân quyền Tài khoản";
        ClientSize = new Size(1120, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        LoadFiltersAndData();
    }

    private void BuildUI()
    {
        // 1. Top Header Banner (Đồng bộ Navy & Gold của FrmLogin)
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = UITheme.Navy };
        var lblTitle = new Label
        {
            Text = "QUẢN TRỊ TÀI KHOẢN NGƯỜI DÙNG & PHÂN QUYỀN (ACCOUNT & PERMISSION)",
            ForeColor = Color.White,
            Font = UITheme.FontBold(11),
            AutoSize = true,
            Location = new Point(24, 14)
        };
        var lblSub = new Label
        {
            Text = "Quản lý danh sách người dùng, cấp quyền vai trò và kiểm soát trạng thái truy cập hệ thống",
            ForeColor = UITheme.Gold,
            Font = UITheme.FontRegular(8.5f),
            AutoSize = true,
            Location = new Point(24, 38)
        };
        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Gold, 2);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // Khoảng đệm phân tách giữa Header và Thanh công cụ
        var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = UITheme.Bg };

        // 2. Toolbar Tìm kiếm & Lọc (Tách biệt, thoáng và chuẩn tỷ lệ)
        var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.White };
        pnlFilter.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, 0, pnlFilter.Width, 0);
            e.Graphics.DrawLine(pen, 0, pnlFilter.Height - 1, pnlFilter.Width, pnlFilter.Height - 1);
        };

        var lblTim = new Label { Text = "Tìm kiếm:", Location = new Point(24, 21), AutoSize = true, Font = UITheme.FontBold(9) };
        txtTimKiem.Location = new Point(98, 17);
        txtTimKiem.Width = 260;
        txtTimKiem.TextChanged += (s, e) => LoadUsers();

        var lblVT = new Label { Text = "Vai trò:", Location = new Point(378, 21), AutoSize = true, Font = UITheme.FontBold(9) };
        cboLocVaiTro.Location = new Point(442, 17);
        cboLocVaiTro.Width = 170;
        cboLocVaiTro.SelectedIndexChanged += (s, e) => LoadUsers();

        var lblTT = new Label { Text = "Trạng thái:", Location = new Point(630, 21), AutoSize = true, Font = UITheme.FontBold(9) };
        cboLocTrangThai.Location = new Point(712, 17);
        cboLocTrangThai.Width = 125;
        cboLocTrangThai.Items.AddRange(new object[] { "Tất cả", "HoatDong", "KhoaTam", "Khoa" });
        cboLocTrangThai.SelectedIndex = 0;
        cboLocTrangThai.SelectedIndexChanged += (s, e) => LoadUsers();

        var btnThem = new Button
        {
            Text = "+ THÊM MỚI",
            Location = new Point(856, 14),
            Size = new Size(115, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Navy,
            ForeColor = Color.White,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        btnThem.FlatAppearance.BorderSize = 0;
        btnThem.Click += (s, e) =>
        {
            using var f = new FrmThemSuaNguoiDung();
            if (f.ShowDialog() == DialogResult.OK) LoadUsers();
        };

        var btnLamMoi = new Button
        {
            Text = "LÀM MỚI",
            Location = new Point(984, 14),
            Size = new Size(95, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        btnLamMoi.FlatAppearance.BorderColor = UITheme.Line;
        btnLamMoi.Click += (s, e) =>
        {
            txtTimKiem.Clear();
            cboLocVaiTro.SelectedIndex = 0;
            cboLocTrangThai.SelectedIndex = 0;
            LoadUsers();
        };

        pnlFilter.Controls.AddRange(new Control[] { lblTim, txtTimKiem, lblVT, cboLocVaiTro, lblTT, cboLocTrangThai, btnThem, btnLamMoi });

        // 3. Grid Panel (Chính giữa)
        var pnlGridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 20, 15), BackColor = UITheme.Bg };
        var pnlGridBorder = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        pnlGridBorder.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlGridBorder.Width - 1, pnlGridBorder.Height - 1);
        };

        SetupDataGrid();
        pnlGridBorder.Controls.Add(dgvUsers);
        pnlGridContainer.Controls.Add(pnlGridBorder);

        // 4. Action Panel ở dưới (Bottom Action Bar)
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        lblTongSo.Location = new Point(20, 20);

        var btnSua = new Button
        {
            Text = "CHỈNH SỬA",
            Location = new Point(260, 14),
            Size = new Size(110, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        btnSua.FlatAppearance.BorderColor = UITheme.Line;
        btnSua.Click += (s, e) => SuaNguoiDungDuocChon();

        var btnKhoa = new Button
        {
            Text = "KHÓA / MỞ KHÓA",
            Location = new Point(380, 14),
            Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = UITheme.Red,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        btnKhoa.FlatAppearance.BorderColor = UITheme.Red;
        btnKhoa.Click += async (s, e) => await KhoaMoKhoaNguoiDung();

        // Gán Role nhanh
        var lblQuickRole = new Label
        {
            Text = "Đổi vai trò thành:",
            Location = new Point(530, 21),
            AutoSize = true,
            Font = UITheme.FontBold(8.5f),
            ForeColor = UITheme.Navy
        };
        cboGanVaiTro.Location = new Point(645, 15);
        btnXacNhanGanRole.Location = new Point(855, 14);
        btnXacNhanGanRole.FlatAppearance.BorderSize = 0;
        btnXacNhanGanRole.Click += async (s, e) => await XuLyGanVaiTro();

        var btnDong = new Button
        {
            Text = "ĐÓNG",
            Location = new Point(990, 14),
            Size = new Size(80, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Soft,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        btnDong.FlatAppearance.BorderColor = UITheme.Line;
        btnDong.Click += (s, e) => Close();

        pnlBottom.Controls.AddRange(new Control[] { lblTongSo, btnSua, btnKhoa, lblQuickRole, cboGanVaiTro, btnXacNhanGanRole, btnDong });

        // Add to Form
        Controls.Add(pnlGridContainer);
        Controls.Add(pnlBottom);
        Controls.Add(pnlFilter);
        Controls.Add(pnlSpacer);
        Controls.Add(pnlHeader);
    }

    private void SetupDataGrid()
    {
        dgvUsers.EnableHeadersVisualStyles = false;
        dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Navy;
        dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvUsers.ColumnHeadersDefaultCellStyle.Font = UITheme.FontBold(9);
        dgvUsers.ColumnHeadersHeight = 38;
        dgvUsers.RowTemplate.Height = 34;
        dgvUsers.DefaultCellStyle.Font = UITheme.FontRegular(9);
        dgvUsers.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#d0e1fd");
        dgvUsers.DefaultCellStyle.SelectionForeColor = UITheme.Navy;
        dgvUsers.GridColor = UITheme.Line;

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaTaiKhoan", HeaderText = "Mã TK", FillWeight = 40 });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenDangNhap", HeaderText = "Tên đăng nhập", FillWeight = 75 });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = "Họ và tên", FillWeight = 110 });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "VaiTro", HeaderText = "Vai trò", FillWeight = 100 });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị công tác", FillWeight = 110 });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoDienThoai", HeaderText = "Số ĐT", FillWeight = 65 });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CCCD", HeaderText = "Số CCCD", FillWeight = 70 });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 60 });

        dgvUsers.CellFormatting += (s, e) =>
        {
            if (dgvUsers.Columns[e.ColumnIndex].Name == "TrangThai" && e.Value != null)
            {
                var val = e.Value.ToString();
                if (val == "HoatDong")
                {
                    e.CellStyle!.ForeColor = UITheme.Green;
                    e.CellStyle.Font = UITheme.FontBold(8.5f);
                }
                else
                {
                    e.CellStyle!.ForeColor = UITheme.Red;
                    e.CellStyle.Font = UITheme.FontBold(8.5f);
                }
            }
        };

        dgvUsers.SelectionChanged += (s, e) => CapNhatNutChucNang();
    }

    private void LoadFiltersAndData()
    {
        try
        {
            using var db = Program.NewDb();
            var roles = db.Roles.AsNoTracking().ToList();

            // Filter vai trò
            var filterRoles = new List<Role> { new() { MaVaiTro = 0, TenVaiTro = "-- Tất cả vai trò --" } };
            filterRoles.AddRange(roles);
            cboLocVaiTro.DataSource = filterRoles;
            cboLocVaiTro.DisplayMember = "TenVaiTro";
            cboLocVaiTro.ValueMember = "MaVaiTro";

            // Quick Role combobox
            cboGanVaiTro.DataSource = roles;
            cboGanVaiTro.DisplayMember = "TenVaiTro";
            cboGanVaiTro.ValueMember = "MaVaiTro";

            LoadUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadUsers()
    {
        try
        {
            using var db = Program.NewDb();
            var query = db.Users.AsNoTracking().Include(x => x.Role).AsQueryable();

            // Lọc theo từ khóa
            var kw = txtTimKiem.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(kw))
            {
                query = query.Where(x => x.TenDangNhap.ToLower().Contains(kw) ||
                                         x.HoTen.ToLower().Contains(kw) ||
                                         (x.Email != null && x.Email.ToLower().Contains(kw)) ||
                                         (x.CCCD != null && x.CCCD.Contains(kw)) ||
                                         (x.SoDienThoai != null && x.SoDienThoai.Contains(kw)));
            }

            // Lọc theo vai trò
            if (cboLocVaiTro.SelectedValue is int roleId && roleId > 0)
            {
                query = query.Where(x => x.MaVaiTro == roleId);
            }

            // Lọc theo trạng thái
            var tt = cboLocTrangThai.SelectedItem?.ToString();
            if (tt != null && tt != "Tất cả")
            {
                query = query.Where(x => x.TrangThai == tt);
            }

            var list = query.OrderBy(x => x.MaTaiKhoan).ToList();

            dgvUsers.Rows.Clear();
            foreach (var u in list)
            {
                dgvUsers.Rows.Add(
                    u.MaTaiKhoan,
                    u.TenDangNhap,
                    u.HoTen,
                    u.Role?.TenVaiTro ?? "Chưa phân vai trò",
                    u.DonVi ?? "-",
                    u.SoDienThoai ?? "-",
                    u.CCCD ?? "-",
                    u.TrangThai
                );
            }

            lblTongSo.Text = $"Tổng số tài khoản: {list.Count}";
            CapNhatNutChucNang();
        }
        catch (Exception ex)
        {
            lblTongSo.Text = "Lỗi tải dữ liệu: " + ex.Message;
        }
    }

    private User? GetSelectedUser()
    {
        if (dgvUsers.SelectedRows.Count == 0) return null;
        var row = dgvUsers.SelectedRows[0];
        if (row.Cells["MaTaiKhoan"].Value is long id)
        {
            using var db = Program.NewDb();
            return db.Users.Include(x => x.Role).FirstOrDefault(x => x.MaTaiKhoan == id);
        }
        return null;
    }

    private void CapNhatNutChucNang()
    {
        var u = GetSelectedUser();
        if (u != null && u.Role != null)
        {
            cboGanVaiTro.SelectedValue = u.MaVaiTro;
        }
    }

    private void SuaNguoiDungDuocChon()
    {
        var u = GetSelectedUser();
        if (u == null)
        {
            MessageBox.Show("Vui lòng chọn một tài khoản để chỉnh sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var f = new FrmThemSuaNguoiDung(u.MaTaiKhoan);
        if (f.ShowDialog() == DialogResult.OK) LoadUsers();
    }

    private async Task KhoaMoKhoaNguoiDung()
    {
        var u = GetSelectedUser();
        if (u == null) return;

        var curId = Session.TaiKhoan?.MaTaiKhoan;
        if (curId.HasValue && curId.Value == u.MaTaiKhoan)
        {
            MessageBox.Show("Bạn không thể tự khóa tài khoản quản trị của chính mình!", "Cảnh báo an toàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var trangThaiMoi = u.TrangThai == "HoatDong" ? "Khoa" : "HoatDong";
        var hanhDong = trangThaiMoi == "Khoa" ? "Khóa" : "Mở khóa";

        var confirm = MessageBox.Show($"Bạn có chắc chắn muốn {hanhDong.ToLower()} tài khoản '{u.TenDangNhap}'?",
            "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            using var db = Program.NewDb();
            var target = await db.Users.FirstOrDefaultAsync(x => x.MaTaiKhoan == u.MaTaiKhoan);
            if (target != null)
            {
                target.TrangThai = trangThaiMoi;
                target.NgayCapNhat = DateTime.UtcNow;
                await db.SaveChangesAsync();

                // Ghi vết nhật ký
                try
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy N'{hanhDong}TaiKhoan', {target.TenDangNhap}, {curId}");
                }
                catch { }

                MessageBox.Show($"{hanhDong} tài khoản thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadUsers();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi cập nhật trạng thái: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task XuLyGanVaiTro()
    {
        var u = GetSelectedUser();
        if (u == null)
        {
            MessageBox.Show("Vui lòng chọn tài khoản cần gán vai trò.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var curId = Session.TaiKhoan?.MaTaiKhoan;
        if (curId.HasValue && curId.Value == u.MaTaiKhoan)
        {
            MessageBox.Show("Quy tắc an toàn: Quản trị viên không được phép tự thay đổi vai trò của chính mình!",
                "Chống leo thang đặc quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (cboGanVaiTro.SelectedValue is not int newRoleId || newRoleId <= 0)
        {
            MessageBox.Show("Vai trò không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = Program.NewDb();
            var roleRepo = new Repositories.RoleRepository(db);
            var roleService = new RoleService(roleRepo);
            await roleService.AssignRoleAsync(curId ?? 0, new DTOs.Role.AssignRoleRequest
            {
                MaTaiKhoan = u.MaTaiKhoan,
                MaVaiTro = newRoleId
            });

            // Ghi vết nhật ký
            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy N'PhanQuyenVaiTro', {u.TenDangNhap}, {curId}");
            }
            catch { }

            MessageBox.Show($"Đã gán vai trò '{RoleConstants.GetName(newRoleId)}' cho tài khoản '{u.TenDangNhap}' thành công!",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Thông báo kiểm soát phân quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
