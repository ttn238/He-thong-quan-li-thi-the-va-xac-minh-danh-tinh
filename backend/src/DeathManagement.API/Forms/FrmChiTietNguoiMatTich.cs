using DeathManagement.API.DTOs.MissingPerson;
using DeathManagement.API.Repositories;
using DeathManagement.API.Services;

namespace DeathManagement.API.Forms;

/// <summary>
/// Màn hình Xem chi tiết hồ sơ người mất tích chia 4 Tab (Task 8.4):
/// (1) Thông tin cá nhân, (2) Người thân, (3) Album ảnh, (4) Lịch sử đối sánh.
/// </summary>
public sealed class FrmChiTietNguoiMatTich : Form
{
    private readonly long _maHoSoMatTich;
    private MissingPersonDetailDto? _current;

    // Header
    private readonly Label lblTieuDe = new() { AutoSize = true, Font = UITheme.FontHeading(12f), ForeColor = UITheme.Navy };
    private readonly Label lblPhuDe = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.Muted };
    private readonly Label lblBadgeTrangThai = new()
    {
        AutoSize = false,
        Size = new Size(165, 30),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = UITheme.FontBold(9f)
    };
    private readonly Button btnLamMoi = new()
    {
        Text = "🔄 Làm mới",
        AutoSize = true,
        MinimumSize = new Size(105, 30),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Soft,
        ForeColor = UITheme.Navy,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };
    private readonly Label lblThongBao = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.Muted };

    // Tabs
    private readonly TabControl tabMain = new() { Dock = DockStyle.Fill, Font = UITheme.FontBold(9.5f), Padding = new Point(14, 6) };
    private readonly TabPage tabThongTin = new("Thông tin cá nhân") { BackColor = UITheme.Bg, Padding = new Padding(12) };
    private readonly TabPage tabNguoiThan = new("Người thân") { BackColor = UITheme.Bg, Padding = new Padding(12) };
    private readonly TabPage tabHinhAnh = new("Album ảnh") { BackColor = UITheme.Bg, Padding = new Padding(12) };
    private readonly TabPage tabDoiSanh = new("Lịch sử đối sánh") { BackColor = UITheme.Bg, Padding = new Padding(12) };

    // Tab 1: Thông tin cá nhân
    private readonly Label lblMaHoSo = MakeValLabel();
    private readonly Label lblHoTen = MakeValLabel();
    private readonly Label lblNgaySinh = MakeValLabel();
    private readonly Label lblGioiTinh = MakeValLabel();
    private readonly Label lblThoiGianMatTich = MakeValLabel();
    private readonly Label lblNoiMatTich = MakeValLabel();
    private readonly Label lblTrangThaiText = MakeValLabel();

    // Tab 2: Người thân
    private readonly Label lblDemNguoiThan = new() { AutoSize = true, Font = UITheme.FontBold(9f), ForeColor = UITheme.Navy };
    private readonly DataGridView dgvNguoiThan = CreateStyledGrid();

    // Tab 3: Album ảnh
    private readonly Label lblDemHinhAnh = new() { AutoSize = true, Font = UITheme.FontBold(9f), ForeColor = UITheme.Navy };
    private readonly DataGridView dgvHinhAnh = CreateStyledGrid();

    // Tab 4: Lịch sử đối sánh
    private readonly Label lblDemDoiSanh = new() { AutoSize = true, Font = UITheme.FontBold(9f), ForeColor = UITheme.Navy };
    private readonly DataGridView dgvDoiSanh = CreateStyledGrid();

    public FrmChiTietNguoiMatTich(long maHoSoMatTich)
    {
        _maHoSoMatTich = maHoSoMatTich;
        Text = $"Chi tiết hồ sơ người mất tích #{maHoSoMatTich}";
        Size = new Size(1040, 700);
        MinimumSize = new Size(880, 580);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        Shown += async (_, _) => await TaiDuLieuAsync();
    }

    private static Label MakeKeyLabel(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = UITheme.FontBold(9f),
        ForeColor = UITheme.Muted,
        Padding = new Padding(0, 0, 10, 0)
    };

    private static Label MakeValLabel() => new()
    {
        Text = "—",
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = UITheme.FontRegular(9.5f),
        ForeColor = UITheme.DarkText
    };

    private static DataGridView CreateStyledGrid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false
        };
        g.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Navy;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = UITheme.FontBold(9f);
        g.ColumnHeadersHeight = 34;
        g.RowTemplate.Height = 30;
        return g;
    }

    private static DataGridViewTextBoxColumn MakeCol(string prop, string header, float weight = 100, string? format = null)
    {
        var col = new DataGridViewTextBoxColumn
        {
            Name = prop,
            DataPropertyName = prop,
            HeaderText = header,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        if (!string.IsNullOrEmpty(format))
            col.DefaultCellStyle.Format = format;
        return col;
    }

    private void BuildUI()
    {
        // Header
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White, Padding = new Padding(18, 10, 18, 10) };
        lblTieuDe.Location = new Point(18, 10);
        lblPhuDe.Location = new Point(18, 38);

        var pnlRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        btnLamMoi.Click += async (_, _) => await TaiDuLieuAsync();
        pnlRight.Controls.Add(btnLamMoi);
        pnlRight.Controls.Add(lblBadgeTrangThai);

        pnlTop.Controls.Add(lblTieuDe);
        pnlTop.Controls.Add(lblPhuDe);
        pnlTop.Controls.Add(pnlRight);

        // Tab 1: Thông tin cá nhân
        var grpInfo = new GroupBox
        {
            Text = "Thông tin hành chính & nhân trắc học",
            Dock = DockStyle.Top,
            Height = 290,
            BackColor = Color.White,
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            Padding = new Padding(14)
        };
        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(6)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        for (int i = 0; i < 4; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        tbl.Controls.Add(MakeKeyLabel("Mã hồ sơ:"), 0, 0);
        tbl.Controls.Add(lblMaHoSo, 1, 0);
        tbl.Controls.Add(MakeKeyLabel("Trạng thái:"), 2, 0);
        tbl.Controls.Add(lblTrangThaiText, 3, 0);

        tbl.Controls.Add(MakeKeyLabel("Họ và tên:"), 0, 1);
        tbl.Controls.Add(lblHoTen, 1, 1);
        tbl.Controls.Add(MakeKeyLabel("Giới tính:"), 2, 1);
        tbl.Controls.Add(lblGioiTinh, 3, 1);

        tbl.Controls.Add(MakeKeyLabel("Ngày sinh / Tuổi:"), 0, 2);
        tbl.Controls.Add(lblNgaySinh, 1, 2);
        tbl.Controls.Add(MakeKeyLabel("Thời gian mất tích:"), 2, 2);
        tbl.Controls.Add(lblThoiGianMatTich, 3, 2);

        tbl.Controls.Add(MakeKeyLabel("Nơi mất tích:"), 0, 3);
        tbl.Controls.Add(lblNoiMatTich, 1, 3);
        tbl.SetColumnSpan(lblNoiMatTich, 3);

        grpInfo.Controls.Add(tbl);
        tabThongTin.Controls.Add(grpInfo);

        // Tab 2: Người thân
        dgvNguoiThan.Columns.AddRange(
            MakeCol(nameof(MissingPersonRelativeDto.HoTen), "Họ tên người thân", 120),
            MakeCol(nameof(MissingPersonRelativeDto.QuanHe), "Quan hệ", 70),
            MakeCol(nameof(MissingPersonRelativeDto.SoDienThoai), "Số điện thoại", 80),
            MakeCol(nameof(MissingPersonRelativeDto.DiaChi), "Địa chỉ liên hệ", 160));
        tabNguoiThan.Controls.Add(WrapGridSection("Danh sách người thân liên hệ", lblDemNguoiThan, dgvNguoiThan));

        // Tab 3: Album ảnh
        dgvHinhAnh.Columns.AddRange(
            MakeCol(nameof(MissingPersonImageDto.MaHinhAnh), "Mã ảnh", 50),
            MakeCol(nameof(MissingPersonImageDto.TenGoc), "Tên tệp", 120),
            MakeCol(nameof(MissingPersonImageDto.LoaiHinhAnh), "Loại ảnh", 95),
            MakeCol(nameof(MissingPersonImageDto.KichThuocHienThi), "Dung lượng", 65),
            MakeCol(nameof(MissingPersonImageDto.MoTa), "Mô tả", 140),
            MakeCol(nameof(MissingPersonImageDto.NguoiTaiLen), "Người tải lên", 95),
            MakeCol(nameof(MissingPersonImageDto.ThoiGianTaiLen), "Thời gian tải", 95, "dd/MM/yyyy HH:mm"));
        tabHinhAnh.Controls.Add(WrapGridSection("Album hình ảnh hồ sơ", lblDemHinhAnh, dgvHinhAnh));

        // Tab 4: Lịch sử đối sánh
        dgvDoiSanh.Columns.AddRange(
            MakeCol(nameof(MissingPersonMatchDto.MaDoiSanh), "Mã đối sánh", 60),
            MakeCol(nameof(MissingPersonMatchDto.MaHoSoThiThe), "Mã hồ sơ thi thể", 75),
            MakeCol(nameof(MissingPersonMatchDto.DiemHienThi), "Điểm phù hợp", 70),
            MakeCol(nameof(MissingPersonMatchDto.NhanMucDoPhuHop), "Mức độ", 80),
            MakeCol(nameof(MissingPersonMatchDto.NhanTrangThai), "Trạng thái", 85),
            MakeCol(nameof(MissingPersonMatchDto.TieuChi), "Tiêu chí đối sánh", 140),
            MakeCol(nameof(MissingPersonMatchDto.NguoiYeuCau), "Người yêu cầu", 95),
            MakeCol(nameof(MissingPersonMatchDto.ThoiGianYeuCau), "Thời gian", 95, "dd/MM/yyyy HH:mm"));
        tabDoiSanh.Controls.Add(WrapGridSection("Danh sách các ca đối sánh nghi vấn với thi thể", lblDemDoiSanh, dgvDoiSanh));

        tabMain.TabPages.AddRange([tabThongTin, tabNguoiThan, tabHinhAnh, tabDoiSanh]);

        // Footer
        var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.White, Padding = new Padding(16, 8, 16, 4) };
        pnlFooter.Controls.Add(lblThongBao);

        Controls.Add(tabMain);
        Controls.Add(pnlFooter);
        Controls.Add(pnlTop);
    }

    private static GroupBox WrapGridSection(string title, Label countLabel, DataGridView grid)
    {
        var grp = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            Padding = new Padding(10)
        };
        var pnlBar = new Panel { Dock = DockStyle.Top, Height = 26 };
        countLabel.Location = new Point(2, 4);
        pnlBar.Controls.Add(countLabel);

        grp.Controls.Add(grid);
        grp.Controls.Add(pnlBar);
        return grp;
    }

    public async Task TaiDuLieuAsync()
    {
        btnLamMoi.Enabled = false;
        lblThongBao.Text = "Đang tải chi tiết hồ sơ người mất tích...";
        try
        {
            await using var db = Program.NewDb();
            var service = new MissingPersonService(new MissingPersonRepository(db));
            var dto = await service.GetByIdAsync(_maHoSoMatTich);
            if (IsDisposed) return;

            if (dto == null)
            {
                lblThongBao.Text = $"Không tìm thấy hồ sơ người mất tích #{_maHoSoMatTich}.";
                MessageBox.Show(lblThongBao.Text, "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BindData(dto);
            lblThongBao.Text = $"Đã tải xong hồ sơ #{dto.MaHoSoMatTich} lúc {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            lblThongBao.Text = ex.Message;
            MessageBox.Show(ex.Message, "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed) btnLamMoi.Enabled = true;
        }
    }

    private void BindData(MissingPersonDetailDto dto)
    {
        _current = dto;
        lblTieuDe.Text = $"HỒ SƠ NGƯỜI MẤT TÍCH #{dto.MaHoSoMatTich} — {dto.HoTen}";
        lblPhuDe.Text = dto.ThoiGianMatTich.HasValue
            ? $"Thời điểm mất tích: {dto.ThoiGianMatTich.Value:dd/MM/yyyy HH:mm}  |  Nơi mất tích: {dto.NoiMatTich ?? "Chưa cập nhật"}"
            : $"Nơi mất tích: {dto.NoiMatTich ?? "Chưa cập nhật"}";

        lblBadgeTrangThai.Text = dto.NhanTrangThai;
        lblBadgeTrangThai.BackColor = ColorTranslator.FromHtml(dto.MauTrangThai);
        lblBadgeTrangThai.ForeColor = ColorTranslator.FromHtml(dto.MauChuTrangThai);

        // Tab 1
        lblMaHoSo.Text = $"#{dto.MaHoSoMatTich}";
        lblHoTen.Text = dto.HoTen;
        lblGioiTinh.Text = dto.NhanGioiTinh;
        lblNgaySinh.Text = dto.NgaySinh.HasValue
            ? $"{dto.NgaySinh.Value:dd/MM/yyyy} ({dto.TuoiUocTinh} tuổi)"
            : "Chưa rõ";
        lblThoiGianMatTich.Text = dto.ThoiGianMatTich.HasValue
            ? dto.ThoiGianMatTich.Value.ToString("dd/MM/yyyy HH:mm")
            : "Chưa rõ";
        lblNoiMatTich.Text = string.IsNullOrWhiteSpace(dto.NoiMatTich) ? "Chưa cập nhật" : dto.NoiMatTich;
        lblTrangThaiText.Text = dto.NhanTrangThai;

        // Tab 2
        dgvNguoiThan.DataSource = dto.NguoiThan;
        lblDemNguoiThan.Text = dto.NguoiThan.Count == 0
            ? "Chưa có thông tin người thân liên hệ."
            : $"Tổng số người thân liên hệ: {dto.NguoiThan.Count}";
        tabNguoiThan.Text = $"Người thân ({dto.NguoiThan.Count})";

        // Tab 3
        dgvHinhAnh.DataSource = dto.HinhAnh;
        lblDemHinhAnh.Text = !dto.CoQuyenXemHinhAnh
            ? "Tài khoản không có quyền xem hình ảnh hồ sơ."
            : dto.HinhAnh.Count == 0
                ? "Chưa có hình ảnh nào được tải lên."
                : $"Tổng số hình ảnh: {dto.HinhAnh.Count}";
        tabHinhAnh.Text = $"Album ảnh ({dto.HinhAnh.Count})";

        // Tab 4
        dgvDoiSanh.DataSource = dto.KetQuaDoiSanh;
        lblDemDoiSanh.Text = !dto.CoQuyenXemDoiSanh
            ? "Tài khoản không có quyền xem kết quả đối sánh."
            : dto.KetQuaDoiSanh.Count == 0
                ? "Chưa phát sinh ca đối sánh nghi vấn nào."
                : $"Tổng số ca đối sánh nghi vấn: {dto.KetQuaDoiSanh.Count}";
        tabDoiSanh.Text = $"Lịch sử đối sánh ({dto.KetQuaDoiSanh.Count})";
    }
}
