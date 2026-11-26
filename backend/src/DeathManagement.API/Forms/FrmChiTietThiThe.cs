using DeathManagement.API.DTOs.Body;
using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Forms;

/// <summary>
/// Màn hình Xem chi tiết hồ sơ thi thể chia 5 Tab (Task 6.3 — EPIC 6),
/// đồng thời tích hợp quy trình chuyển trạng thái State Machine & lịch sử truy vết (Task 6.5).
/// Các tab: (1) Thông tin chung & Tin báo, (2) Đặc điểm & Đồ vật, (3) Hình ảnh, (4) Lưu trữ & Lịch sử, (5) Kết quả Đối sánh.
/// </summary>
public sealed class FrmChiTietThiThe : Form
{
    private readonly long _maHoSoThiThe;
    private BodyDetailDto? _current;
    private bool _changed;
    private System.Drawing.Image? _previewBitmap;

    // ─────────────── Top Header & Status Badge ───────────────
    private readonly Label lblMaHoSo = new() { AutoSize = true, Font = UITheme.FontHeading(12f), ForeColor = UITheme.Navy };
    private readonly Label lblViTri = new() { AutoSize = true, Font = UITheme.FontBold(9.5f), ForeColor = UITheme.Blue };
    private readonly Label lblBadgeTrangThai = new()
    {
        AutoSize = false,
        Size = new Size(185, 32),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = UITheme.FontBold(9.5f)
    };
    private readonly Button btnLamMoi = new()
    {
        Text = "🔄 Làm mới",
        AutoSize = true,
        MinimumSize = new Size(110, 32),
        Padding = new Padding(8, 2, 8, 2),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Soft,
        ForeColor = UITheme.Navy,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };
    private readonly Label lblThongBao = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.Muted };

    // ─────────────── TabControl chính ───────────────
    private readonly TabControl tabMain = new()
    {
        Dock = DockStyle.Fill,
        Font = UITheme.FontBold(9.5f),
        Padding = new Point(14, 6)
    };
    private readonly TabPage tabThongTinChung = new("Thông tin chung") { BackColor = UITheme.Bg, Padding = new Padding(10) };
    private readonly TabPage tabDacDiemDoVat = new("Đặc điểm & Đồ vật") { BackColor = UITheme.Bg, Padding = new Padding(10) };
    private readonly TabPage tabHinhAnh = new("Hình ảnh") { BackColor = UITheme.Bg, Padding = new Padding(10) };
    private readonly TabPage tabLuuTru = new("Lưu trữ & Lịch sử") { BackColor = UITheme.Bg, Padding = new Padding(10) };
    private readonly TabPage tabDoiSanh = new("Kết quả Đối sánh") { BackColor = UITheme.Bg, Padding = new Padding(10) };

    // ─────────────── Tab 1: State Machine + Thông tin cơ bản + Tin báo ───────────────
    private readonly ComboBox cboTrangThaiMoi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4) };
    private readonly TextBox txtLyDo = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 8, 4),
        PlaceholderText = "Nhập lý do / ghi chú (bắt buộc khi mở lại hồ sơ đã đóng)...",
        MaxLength = 400
    };
    private readonly Button btnChuyenTrangThai = new()
    {
        Text = "🔄 Chuyển trạng thái",
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(165, 32),
        Padding = new Padding(10, 2, 10, 2),
        Margin = new Padding(0, 2, 0, 2),
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };
    private readonly Label lblGoiYChuyen = new() { AutoSize = true, UseMnemonic = false, Font = UITheme.FontRegular(8.5f), ForeColor = UITheme.Muted };

    private readonly DateTimePicker dtpThoiGian = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
        Width = 195
    };
    private readonly TextBox txtDiaDiem = new() { Dock = DockStyle.Fill, MaxLength = 255 };
    private readonly ComboBox cboGioiTinh = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175 };
    private readonly NumericUpDown nudTuoi = new() { Minimum = 0, Maximum = 130, Width = 90 };
    private readonly CheckBox chkTuoiNull = new() { Text = "Không rõ", AutoSize = true };
    private readonly NumericUpDown nudChieuCao = new() { Minimum = 1, Maximum = 300, DecimalPlaces = 1, Value = 165, Width = 90 };
    private readonly CheckBox chkChieuCaoNull = new() { Text = "Không rõ", AutoSize = true };
    private readonly Button btnLuuThongTin = new()
    {
        Text = "💾 LƯU THÔNG TIN",
        AutoSize = true,
        MinimumSize = new Size(165, 32),
        Padding = new Padding(10, 2, 10, 2),
        BackColor = UITheme.Blue,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };

    private readonly Label lblTinBaoTrangThai = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.Muted };
    private readonly Label lblTbMaTin = new() { AutoSize = true, Font = UITheme.FontBold(9.5f), ForeColor = UITheme.Navy };
    private readonly Label lblTbLoaiUuTien = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.DarkText };
    private readonly Label lblTbTieuDe = new() { AutoSize = true, Font = UITheme.FontBold(9.5f), ForeColor = UITheme.DarkText };
    private readonly Label lblTbThoiGianDiaDiem = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.DarkText };
    private readonly Label lblTbNguoiBao = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.DarkText };
    private readonly TextBox txtTbNoiDung = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        BackColor = ColorTranslator.FromHtml("#f8fafc"),
        BorderStyle = BorderStyle.FixedSingle,
        Font = UITheme.FontRegular(9f)
    };
    private readonly Panel pnlTinBaoChiTiet = new() { Dock = DockStyle.Fill };

    // ─────────────── Tab 2: Đặc điểm & Đồ vật cá nhân ───────────────
    private readonly Label lblDacDiemHanChe = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.Muted, Visible = false };
    private readonly TextBox txtDacDiem = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        Font = UITheme.FontRegular(9.5f)
    };
    private readonly Button btnLuuDacDiem = new()
    {
        Text = "💾 LƯU ĐẶC ĐIỂM",
        AutoSize = true,
        MinimumSize = new Size(155, 32),
        Padding = new Padding(10, 2, 10, 2),
        BackColor = UITheme.Blue,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };
    private readonly Label lblDemDoVat = new() { AutoSize = true, Font = UITheme.FontBold(9f), ForeColor = UITheme.Navy };
    private readonly DataGridView dgvDoVat = CreateStyledGrid();

    // ─────────────── Tab 3: Hình ảnh ───────────────
    private readonly Label lblDemHinhAnh = new() { AutoSize = true, Font = UITheme.FontBold(9f), ForeColor = UITheme.Navy };
    private readonly Button btnTaiThemAnh = new()
    {
        Text = "➕ Tải thêm ảnh",
        AutoSize = true,
        MinimumSize = new Size(135, 30),
        Padding = new Padding(8, 2, 8, 2),
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };
    private readonly Button btnXemChiTietAnh = new()
    {
        Text = "👁 Xem ảnh phóng to",
        AutoSize = true,
        MinimumSize = new Size(155, 30),
        Padding = new Padding(8, 2, 8, 2),
        BackColor = UITheme.Soft,
        ForeColor = UITheme.Navy,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };
    private readonly DataGridView dgvHinhAnh = CreateStyledGrid();
    private readonly PictureBox picPreview = new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = ColorTranslator.FromHtml("#1e293b")
    };
    private readonly Label lblAnhMeta = new()
    {
        Dock = DockStyle.Bottom,
        Height = 68,
        Font = UITheme.FontRegular(8.5f),
        ForeColor = UITheme.DarkText,
        BackColor = Color.White,
        Padding = new Padding(8, 6, 8, 6)
    };

    // ─────────────── Tab 4: Lưu trữ & Lịch sử ───────────────
    private readonly Label lblViTriChiTiet = new() { AutoSize = true, Font = UITheme.FontBold(10f), ForeColor = UITheme.Navy };
    private readonly Label lblViTriPhu = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.Muted };
    private readonly DataGridView dgvLichSuLuuTru = CreateStyledGrid();
    private readonly DataGridView dgvLichSuTrangThai = CreateStyledGrid();

    // ─────────────── Tab 5: Kết quả Đối sánh ───────────────
    private readonly Label lblDemDoiSanh = new() { AutoSize = true, Font = UITheme.FontBold(9f), ForeColor = UITheme.Navy };
    private readonly DataGridView dgvDoiSanh = CreateStyledGrid();

    private sealed record StatusOption(string Code, string Label)
    {
        public override string ToString() => Label;
    }

    private static string GetStatusActionText(string? statusCode) => statusCode switch
    {
        Body.StatusDangLuuGiu    => "📦 Đang lưu giữ",
        Body.StatusDangXacMinh   => "🔍 Đang xác minh",
        Body.StatusDaXacDinh     => "✅ Đã xác định",
        Body.StatusDaBanGiao     => "🤝 Đã bàn giao",
        Body.StatusDongHoSo      => "🔒 Đóng hồ sơ",
        Body.StatusTiepTucLuuGiu => "🔓 Tiếp tục lưu giữ",
        _                        => "🔄 Chuyển trạng thái"
    };

    public FrmChiTietThiThe(long maHoSoThiThe)
    {
        _maHoSoThiThe = maHoSoThiThe;
        Text = $"Chi tiết hồ sơ thi thể #{maHoSoThiThe}";
        Size = new Size(1120, 780);
        MinimumSize = new Size(940, 660);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        Shown += async (_, _) => await TaiDuLieuAsync();
    }

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

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        UseMnemonic = false,
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = UITheme.FontBold(9f),
        ForeColor = UITheme.DarkText,
        Padding = new Padding(0, 0, 8, 0)
    };

    private void BuildUI()
    {
        cboGioiTinh.Items.AddRange(["KhongXacDinh", "Nam", "Nu", "Khac"]);
        chkTuoiNull.CheckedChanged += (_, _) => nudTuoi.Enabled = !chkTuoiNull.Checked && btnLuuThongTin.Enabled;
        chkChieuCaoNull.CheckedChanged += (_, _) => nudChieuCao.Enabled = !chkChieuCaoNull.Checked && btnLuuThongTin.Enabled;

        // 1. Top Header Banner
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White, Padding = new Padding(18, 10, 18, 10) };
        pnlTop.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };
        lblMaHoSo.Location = new Point(18, 10);
        lblViTri.Location = new Point(18, 38);

        var pnlTopRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        btnLamMoi.FlatAppearance.BorderColor = UITheme.Line;
        btnLamMoi.Margin = new Padding(10, 0, 0, 0);
        btnLamMoi.Click += async (_, _) => await TaiDuLieuAsync();
        pnlTopRight.Controls.Add(btnLamMoi);
        pnlTopRight.Controls.Add(lblBadgeTrangThai);

        pnlTop.Controls.AddRange([lblMaHoSo, lblViTri, pnlTopRight]);

        // 2. Build 5 Tabs
        BuildTabThongTinChung();
        BuildTabDacDiemDoVat();
        BuildTabHinhAnh();
        BuildTabLuuTru();
        BuildTabDoiSanh();

        tabMain.TabPages.AddRange([tabThongTinChung, tabDacDiemDoVat, tabHinhAnh, tabLuuTru, tabDoiSanh]);

        var pnlTabs = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 6), BackColor = UITheme.Bg };
        pnlTabs.Controls.Add(tabMain);

        // 3. Bottom Footer
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White, Padding = new Padding(16, 8, 16, 8) };
        pnlBottom.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };
        var btnDong = new Button
        {
            Text = "ĐÓNG",
            Size = new Size(100, 32),
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Soft,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        btnDong.Click += (_, _) =>
        {
            DialogResult = _changed ? DialogResult.OK : DialogResult.Cancel;
            Close();
        };
        lblThongBao.Dock = DockStyle.Left;
        lblThongBao.Padding = new Padding(0, 6, 0, 0);
        pnlBottom.Controls.Add(lblThongBao);
        pnlBottom.Controls.Add(btnDong);

        Controls.Add(pnlTabs);
        Controls.Add(pnlBottom);
        Controls.Add(pnlTop);

        cboTrangThaiMoi.SelectedIndexChanged += (_, _) => CapNhatNutChuyenTrangThai();
        btnChuyenTrangThai.Click += async (_, _) => await XuLyChuyenTrangThaiAsync();
        btnLuuThongTin.Click += async (_, _) => await XuLyLuuThongTinAsync();
        btnLuuDacDiem.Click += async (_, _) => await XuLyLuuThongTinAsync();
    }

    private void CapNhatNutChuyenTrangThai()
    {
        var selected = cboTrangThaiMoi.SelectedItem as StatusOption;
        btnChuyenTrangThai.Text = GetStatusActionText(selected?.Code);
    }

    private void BuildTabThongTinChung()
    {
        // Group 1: State Machine (TableLayoutPanel để không cắt chữ ở mọi mức DPI)
        var grpStatus = new GroupBox
        {
            Text = "Quy trình Chuyển đổi Trạng thái Hồ sơ",
            Dock = DockStyle.Top,
            Height = 114,
            Padding = new Padding(12, 8, 12, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };
        var tblStatus = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 5,
            RowCount = 1
        };
        tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblStatus.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        tblStatus.Controls.Add(new Label { Text = "Trạng thái kế tiếp:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 6, 0), Font = UITheme.FontBold(9f) }, 0, 0);
        tblStatus.Controls.Add(cboTrangThaiMoi, 1, 0);
        tblStatus.Controls.Add(new Label { Text = "Lý do / Ghi chú:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(6, 8, 6, 0), Font = UITheme.FontBold(9f) }, 2, 0);
        tblStatus.Controls.Add(txtLyDo, 3, 0);
        tblStatus.Controls.Add(btnChuyenTrangThai, 4, 0);

        lblGoiYChuyen.Dock = DockStyle.Bottom;
        grpStatus.Controls.Add(tblStatus);
        grpStatus.Controls.Add(lblGoiYChuyen);

        // Group 2: Thông tin cơ bản
        var grpInfo = new GroupBox
        {
            Text = "Thông tin Nhân trắc học && Phát hiện (HoSoThiThe)",
            Dock = DockStyle.Top,
            Height = 204,
            Padding = new Padding(12, 8, 12, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4 };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        tbl.Controls.Add(MakeFieldLabel("Thời gian phát hiện:"), 0, 0);
        tbl.Controls.Add(dtpThoiGian, 1, 0);
        tbl.Controls.Add(MakeFieldLabel("Giới tính ước tính:"), 2, 0);
        tbl.Controls.Add(cboGioiTinh, 3, 0);

        var pnlTuoi = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        pnlTuoi.Controls.AddRange([nudTuoi, chkTuoiNull]);
        var pnlChieuCao = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        pnlChieuCao.Controls.AddRange([nudChieuCao, new Label { Text = "cm", AutoSize = true, Margin = new Padding(2, 6, 6, 0) }, chkChieuCaoNull]);

        tbl.Controls.Add(MakeFieldLabel("Tuổi ước lượng:"), 0, 1);
        tbl.Controls.Add(pnlTuoi, 1, 1);
        tbl.Controls.Add(MakeFieldLabel("Chiều cao:"), 2, 1);
        tbl.Controls.Add(pnlChieuCao, 3, 1);

        tbl.Controls.Add(MakeFieldLabel("Địa điểm phát hiện:"), 0, 2);
        tbl.Controls.Add(txtDiaDiem, 1, 2);
        tbl.SetColumnSpan(txtDiaDiem, 3);

        var pnlSave = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        pnlSave.Controls.Add(btnLuuThongTin);
        tbl.Controls.Add(pnlSave, 0, 3);
        tbl.SetColumnSpan(pnlSave, 4);

        grpInfo.Controls.Add(tbl);

        // Group 3: Tin báo gốc
        var grpIncident = new GroupBox
        {
            Text = "Thông tin Tin báo gốc liên kết (TinBao)",
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };

        lblTinBaoTrangThai.Dock = DockStyle.Top;

        var tblInc = new TableLayoutPanel { Dock = DockStyle.Top, Height = 105, ColumnCount = 2, RowCount = 4 };
        tblInc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tblInc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tblInc.Controls.Add(lblTbMaTin, 0, 0);
        tblInc.Controls.Add(lblTbLoaiUuTien, 1, 0);
        tblInc.Controls.Add(lblTbTieuDe, 0, 1);
        tblInc.SetColumnSpan(lblTbTieuDe, 2);
        tblInc.Controls.Add(lblTbThoiGianDiaDiem, 0, 2);
        tblInc.SetColumnSpan(lblTbThoiGianDiaDiem, 2);
        tblInc.Controls.Add(lblTbNguoiBao, 0, 3);
        tblInc.SetColumnSpan(lblTbNguoiBao, 2);

        var lblNdHeader = new Label
        {
            Text = "Nội dung tin báo:",
            Dock = DockStyle.Top,
            Height = 22,
            Font = UITheme.FontBold(8.5f),
            ForeColor = UITheme.Muted
        };

        pnlTinBaoChiTiet.Controls.Add(txtTbNoiDung);
        pnlTinBaoChiTiet.Controls.Add(lblNdHeader);
        pnlTinBaoChiTiet.Controls.Add(tblInc);

        grpIncident.Controls.Add(pnlTinBaoChiTiet);
        grpIncident.Controls.Add(lblTinBaoTrangThai);

        tabThongTinChung.Controls.Add(grpIncident);
        tabThongTinChung.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        tabThongTinChung.Controls.Add(grpInfo);
        tabThongTinChung.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        tabThongTinChung.Controls.Add(grpStatus);
    }

    private void BuildTabDacDiemDoVat()
    {
        var grpDacDiem = new GroupBox
        {
            Text = "Đặc điểm cơ thể && Dấu vết nhận dạng (HoSoThiThe.DacDiemCoThe)",
            Dock = DockStyle.Top,
            Height = 190,
            Padding = new Padding(12, 8, 12, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };

        var pnlDacDiemBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 4, 0, 0)
        };
        pnlDacDiemBottom.Controls.Add(btnLuuDacDiem);

        lblDacDiemHanChe.Dock = DockStyle.Top;
        grpDacDiem.Controls.Add(txtDacDiem);
        grpDacDiem.Controls.Add(lblDacDiemHanChe);
        grpDacDiem.Controls.Add(pnlDacDiemBottom);

        var grpDoVat = new GroupBox
        {
            Text = "Danh sách Đồ vật cá nhân kèm theo",
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };

        lblDemDoVat.Dock = DockStyle.Top;
        lblDemDoVat.Height = 26;

        dgvDoVat.Columns.AddRange(
        [
            MakeCol(nameof(BodyPersonalItemDto.MaDoVat), "Mã", 45),
            MakeCol(nameof(BodyPersonalItemDto.Ten), "Tên đồ vật", 130),
            MakeCol(nameof(BodyPersonalItemDto.TenLoaiDoVat), "Loại đồ vật", 100),
            MakeCol(nameof(BodyPersonalItemDto.TinhTrang), "Tình trạng", 100),
            MakeCol(nameof(BodyPersonalItemDto.MoTa), "Mô tả chi tiết", 180)
        ]);

        grpDoVat.Controls.Add(dgvDoVat);
        grpDoVat.Controls.Add(lblDemDoVat);

        tabDacDiemDoVat.Controls.Add(grpDoVat);
        tabDacDiemDoVat.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        tabDacDiemDoVat.Controls.Add(grpDacDiem);
    }

    private void BuildTabHinhAnh()
    {
        var pnlBar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.White, Padding = new Padding(12, 7, 12, 7) };
        lblDemHinhAnh.Location = new Point(12, 13);

        var flowBtn = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true
        };
        btnXemChiTietAnh.FlatAppearance.BorderColor = UITheme.Line;
        btnTaiThemAnh.Margin = new Padding(8, 0, 0, 0);
        flowBtn.Controls.Add(btnTaiThemAnh);
        flowBtn.Controls.Add(btnXemChiTietAnh);

        pnlBar.Controls.Add(lblDemHinhAnh);
        pnlBar.Controls.Add(flowBtn);

        dgvHinhAnh.Columns.AddRange(
        [
            MakeCol(nameof(BodyImageItemDto.MaHinhAnh), "ID", 45),
            MakeCol(nameof(BodyImageItemDto.TenGoc), "Tên tệp", 140),
            MakeCol(nameof(BodyImageItemDto.LoaiHinhAnh), "Loại ảnh", 110),
            MakeCol(nameof(BodyImageItemDto.KichThuocHienThi), "Dung lượng", 70),
            MakeCol(nameof(BodyImageItemDto.NhanNhayCam), "Phân loại", 75),
            MakeCol(nameof(BodyImageItemDto.NguoiTaiLen), "Người tải", 105),
            MakeCol(nameof(BodyImageItemDto.ThoiGianTaiLen), "Thời gian", 100, "dd/MM/yyyy HH:mm")
        ]);

        dgvHinhAnh.SelectionChanged += (_, _) => HienThiXemTruocAnh();
        dgvHinhAnh.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0) await MoChiTietAnhAsync();
        };
        btnXemChiTietAnh.Click += async (_, _) => await MoChiTietAnhAsync();
        btnTaiThemAnh.Click += async (_, _) =>
        {
            if (!PermissionUi.Check(this, AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionCreate))
                return;
            using var f = new FrmTaiLenHinhAnh(defaultMaThiThe: _maHoSoThiThe);
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                _changed = true;
                await TaiDuLieuAsync();
            }
        };

        var tblImages = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UITheme.Bg
        };
        tblImages.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        tblImages.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        tblImages.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var grpList = new GroupBox
        {
            Text = "Danh sách Hình ảnh",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 8, 0),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };
        grpList.Controls.Add(dgvHinhAnh);
        tblImages.Controls.Add(grpList, 0, 0);

        var grpPreview = new GroupBox
        {
            Text = "Xem nhanh ảnh đang chọn",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };
        grpPreview.Controls.Add(picPreview);
        grpPreview.Controls.Add(lblAnhMeta);
        tblImages.Controls.Add(grpPreview, 1, 0);

        tabHinhAnh.Controls.Add(tblImages);
        tabHinhAnh.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        tabHinhAnh.Controls.Add(pnlBar);
    }

    private void BuildTabLuuTru()
    {
        var grpCurrent = new GroupBox
        {
            Text = "Vị trí ngăn lạnh hiện tại",
            Dock = DockStyle.Top,
            Height = 86,
            Padding = new Padding(14, 10, 14, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };
        lblViTriChiTiet.Location = new Point(16, 26);
        lblViTriPhu.Location = new Point(16, 52);
        grpCurrent.Controls.AddRange([lblViTriChiTiet, lblViTriPhu]);

        // Dùng TableLayoutPanel theo % để 2 bảng lịch sử luôn cân đối, không bị bẹp trên màn hình DPI cao
        var tblHistory = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UITheme.Bg
        };
        tblHistory.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblHistory.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        tblHistory.RowStyles.Add(new RowStyle(SizeType.Percent, 58));

        var grpMove = new GroupBox
        {
            Text = "Lịch sử xếp / điều chuyển ngăn lưu trữ",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };
        dgvLichSuLuuTru.Columns.AddRange(
        [
            MakeCol(nameof(BodyStorageHistoryDto.ThoiGian), "Thời điểm xếp / chuyển", 110, "dd/MM/yyyy HH:mm:ss"),
            MakeCol(nameof(BodyStorageHistoryDto.ViTriHienThi), "Vị trí (Khu - Tủ - Ngăn)", 140),
            MakeCol(nameof(BodyStorageHistoryDto.Khu), "Khu", 70),
            MakeCol(nameof(BodyStorageHistoryDto.Tu), "Tủ", 70),
            MakeCol(nameof(BodyStorageHistoryDto.Ngan), "Ngăn", 70)
        ]);
        grpMove.Controls.Add(dgvLichSuLuuTru);
        tblHistory.Controls.Add(grpMove, 0, 0);

        var grpAudit = new GroupBox
        {
            Text = "Lịch sử Thay đổi Trạng thái && Truy vết Thao tác",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };
        dgvLichSuTrangThai.Columns.AddRange(
        [
            MakeCol(nameof(BodyStatusHistoryDto.ThoiGian), "Thời điểm", 90, "dd/MM/yyyy HH:mm:ss"),
            MakeCol(nameof(BodyStatusHistoryDto.HanhDong), "Hành động", 80),
            MakeCol(nameof(BodyStatusHistoryDto.NhanTrangThaiCu), "Trạng thái cũ", 85),
            MakeCol(nameof(BodyStatusHistoryDto.NhanTrangThaiMoi), "Trạng thái mới", 90),
            MakeCol(nameof(BodyStatusHistoryDto.NguoiThucHien), "Người thực hiện", 110),
            MakeCol(nameof(BodyStatusHistoryDto.LyDo), "Lý do / Ghi chú", 140)
        ]);
        dgvLichSuTrangThai.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvLichSuTrangThai.Rows[e.RowIndex].DataBoundItem is not BodyStatusHistoryDto item) return;

            var colName = dgvLichSuTrangThai.Columns[e.ColumnIndex].Name;
            if (colName == nameof(BodyStatusHistoryDto.NhanTrangThaiMoi) && !string.IsNullOrEmpty(item.TrangThaiMoi))
            {
                e.CellStyle!.BackColor = ColorTranslator.FromHtml(Body.GetStatusColorHex(item.TrangThaiMoi));
                e.CellStyle.ForeColor = ColorTranslator.FromHtml(Body.GetStatusTextColorHex(item.TrangThaiMoi));
                e.CellStyle.Font = UITheme.FontBold(8.5f);
            }
            else if (colName == nameof(BodyStatusHistoryDto.NhanTrangThaiCu) && !string.IsNullOrEmpty(item.TrangThaiCu))
            {
                e.CellStyle!.BackColor = ColorTranslator.FromHtml(Body.GetStatusColorHex(item.TrangThaiCu));
                e.CellStyle.ForeColor = ColorTranslator.FromHtml(Body.GetStatusTextColorHex(item.TrangThaiCu));
            }
        };
        grpAudit.Controls.Add(dgvLichSuTrangThai);
        tblHistory.Controls.Add(grpAudit, 0, 1);

        tabLuuTru.Controls.Add(tblHistory);
        tabLuuTru.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        tabLuuTru.Controls.Add(grpCurrent);
    }

    private void BuildTabDoiSanh()
    {
        var grpMatch = new GroupBox
        {
            Text = "Danh sách Kết quả Đối sánh với Hồ sơ Người mất tích",
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };

        lblDemDoiSanh.Dock = DockStyle.Top;
        lblDemDoiSanh.Height = 28;

        dgvDoiSanh.Columns.AddRange(
        [
            MakeCol(nameof(BodyMatchResultDto.MaDoiSanh), "Mã ĐS", 50),
            MakeCol(nameof(BodyMatchResultDto.MaHoSoMatTich), "Mã MT", 55),
            MakeCol(nameof(BodyMatchResultDto.HoTenNguoiMatTich), "Họ tên người mất tích", 130),
            MakeCol(nameof(BodyMatchResultDto.DiemHienThi), "Điểm (%)", 65),
            MakeCol(nameof(BodyMatchResultDto.NhanMucDoPhuHop), "Mức độ phù hợp", 90),
            MakeCol(nameof(BodyMatchResultDto.NhanTrangThai), "Trạng thái", 95),
            MakeCol(nameof(BodyMatchResultDto.TieuChi), "Chi tiết điểm tiêu chí", 145),
            MakeCol(nameof(BodyMatchResultDto.ThoiGianYeuCau), "Thời gian", 95, "dd/MM/yyyy HH:mm"),
            MakeCol(nameof(BodyMatchResultDto.NguoiYeuCau), "Người yêu cầu", 100)
        ]);

        dgvDoiSanh.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvDoiSanh.Rows[e.RowIndex].DataBoundItem is not BodyMatchResultDto item) return;
            if (dgvDoiSanh.Columns[e.ColumnIndex].Name == nameof(BodyMatchResultDto.NhanMucDoPhuHop))
            {
                (Color bg, Color fg) = item.MucDoPhuHop switch
                {
                    "UuTien"     => (ColorTranslator.FromHtml("#FADBD8"), ColorTranslator.FromHtml("#922B21")),
                    "CanXacMinh" => (ColorTranslator.FromHtml("#FDEBD0"), ColorTranslator.FromHtml("#935116")),
                    "CoKhaNang"  => (ColorTranslator.FromHtml("#D6EAF8"), ColorTranslator.FromHtml("#1B4F72")),
                    _            => (ColorTranslator.FromHtml("#EAEDED"), ColorTranslator.FromHtml("#566573"))
                };
                e.CellStyle!.BackColor = bg;
                e.CellStyle.ForeColor = fg;
                e.CellStyle.Font = UITheme.FontBold(8.5f);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        };

        grpMatch.Controls.Add(dgvDoiSanh);
        grpMatch.Controls.Add(lblDemDoiSanh);
        tabDoiSanh.Controls.Add(grpMatch);
    }

    private async Task TaiDuLieuAsync()
    {
        try
        {
            lblThongBao.Text = "⏳ Đang tải chi tiết hồ sơ...";
            lblThongBao.ForeColor = UITheme.Blue;

            using var db = Program.NewDb();
            var service = new BodyService(new BodyRepository(db));
            var detail = await service.GetByIdAsync(_maHoSoThiThe);

            if (detail == null)
            {
                MessageBox.Show(this, $"Không tìm thấy hồ sơ thi thể #{_maHoSoThiThe}.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }

            HienThiDuLieu(detail);
            lblThongBao.Text = $"✅ Đã tải xong chi tiết hồ sơ #{detail.MaHoSoThiThe}.";
            lblThongBao.ForeColor = UITheme.Muted;
        }
        catch (Exception ex)
        {
            lblThongBao.Text = "⛔ " + ex.Message;
            lblThongBao.ForeColor = UITheme.Red;
        }
    }

    /// <summary>Đổ dữ liệu tổng hợp của BodyDetailDto lên toàn bộ các Tab (tách riêng để dễ kiểm thử).</summary>
    public void HienThiDuLieu(BodyDetailDto dto)
    {
        _current = dto;

        // 1. Header
        var tinBaoTag = !dto.CheDoHanCheNhaXac && dto.TinBao != null
            ? $"  •  Tin báo {dto.TinBao.MaTin}"
            : (!dto.CheDoHanCheNhaXac && dto.MaTinBao.HasValue ? $"  •  Tin báo #{dto.MaTinBao}" : string.Empty);

        lblMaHoSo.Text = $"HỒ SƠ THI THỂ #{dto.MaHoSoThiThe}{tinBaoTag}";
        lblViTri.Text = dto.ViTriHienTai != null
            ? $"📍 Vị trí ngăn lạnh hiện tại: {dto.ViTriHienTai.ViTriHienThi} (Sức chứa: {dto.ViTriHienTai.SucChua} · Xếp lúc {dto.ViTriHienTai.ThoiGianXep:dd/MM/yyyy HH:mm})"
            : $"📍 Vị trí lưu trữ hiện tại: {dto.ViTriLuuTruHienThi}";

        lblBadgeTrangThai.Text = dto.NhanTrangThai.ToUpperInvariant();
        lblBadgeTrangThai.BackColor = ColorTranslator.FromHtml(dto.MauTrangThai);
        lblBadgeTrangThai.ForeColor = ColorTranslator.FromHtml(dto.MauChuTrangThai);

        // 2. Tab 1: Thông tin cơ bản & State Machine
        dtpThoiGian.Value = dto.ThoiGianPhatHien >= dtpThoiGian.MinDate && dto.ThoiGianPhatHien <= dtpThoiGian.MaxDate
            ? dto.ThoiGianPhatHien
            : DateTime.Now;
        txtDiaDiem.Text = dto.CheDoHanCheNhaXac ? "(Ẩn theo phân quyền Nhà xác)" : (dto.DiaDiem ?? string.Empty);
        cboGioiTinh.SelectedItem = dto.GioiTinh ?? "KhongXacDinh";
        if (cboGioiTinh.SelectedIndex < 0) cboGioiTinh.SelectedIndex = 0;

        if (dto.TuoiUocLuong.HasValue)
        {
            chkTuoiNull.Checked = false;
            nudTuoi.Value = Math.Clamp((int)dto.TuoiUocLuong.Value, 0, 130);
        }
        else
        {
            chkTuoiNull.Checked = true;
        }

        if (!dto.CheDoHanCheNhaXac && dto.ChieuCao.HasValue && dto.ChieuCao.Value > 0)
        {
            chkChieuCaoNull.Checked = false;
            nudChieuCao.Value = Math.Clamp(dto.ChieuCao.Value, 1m, 300m);
        }
        else
        {
            chkChieuCaoNull.Checked = true;
        }

        // State Machine dropdown
        cboTrangThaiMoi.Items.Clear();
        foreach (var next in dto.TrangThaiTiepTheoHopLe)
            cboTrangThaiMoi.Items.Add(new StatusOption(next, Body.GetStatusLabel(next)));

        var canUpdate = AccessControl.Can(AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate);
        var restrictedUpdate = dto.CheDoHanCheNhaXac
                               || AccessControl.RoleCode == AuthorizationPolicies.RoleMorgue
                               || AccessControl.ScopeOf(AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate) == AuthorizationPolicies.ScopePartial;
        var isClosed = dto.TrangThai == Body.StatusDongHoSo;

        if (cboTrangThaiMoi.Items.Count > 0)
        {
            cboTrangThaiMoi.SelectedIndex = 0;
            cboTrangThaiMoi.Enabled = canUpdate;
            btnChuyenTrangThai.Enabled = canUpdate;
            CapNhatNutChuyenTrangThai();
            var nextLabels = string.Join("  |  ", dto.TrangThaiTiepTheoHopLe.Select(s => $"{Body.GetStatusLabel(s)} ({s})"));
            lblGoiYChuyen.Text = $"ℹ Từ trạng thái [{dto.NhanTrangThai}], chỉ được phép chuyển sang: {nextLabels}.";
        }
        else
        {
            cboTrangThaiMoi.Enabled = false;
            btnChuyenTrangThai.Enabled = false;
            btnChuyenTrangThai.Text = GetStatusActionText(null);
            lblGoiYChuyen.Text = "ℹ Hồ sơ không có trạng thái chuyển tiếp.";
        }

        var canEditInfo = canUpdate && !restrictedUpdate && !isClosed;
        dtpThoiGian.Enabled = txtDiaDiem.Enabled = cboGioiTinh.Enabled = canEditInfo;
        chkTuoiNull.Enabled = chkChieuCaoNull.Enabled = canEditInfo;
        nudTuoi.Enabled = canEditInfo && !chkTuoiNull.Checked;
        nudChieuCao.Enabled = canEditInfo && !chkChieuCaoNull.Checked;
        btnLuuThongTin.Enabled = canEditInfo;

        // Tin báo gốc
        if (dto.CheDoHanCheNhaXac)
        {
            lblTinBaoTrangThai.Visible = true;
            lblTinBaoTrangThai.Text = "🔒 Thông tin Tin báo gốc được ẩn theo phân quyền hạn chế của Nhà xác (PARTIAL).";
            pnlTinBaoChiTiet.Visible = false;
        }
        else if (dto.TinBao == null)
        {
            lblTinBaoTrangThai.Visible = true;
            lblTinBaoTrangThai.Text = "Hồ sơ thi thể này không liên kết với tin báo sự việc nào.";
            pnlTinBaoChiTiet.Visible = false;
        }
        else
        {
            var tb = dto.TinBao;
            lblTinBaoTrangThai.Visible = false;
            pnlTinBaoChiTiet.Visible = true;
            lblTbMaTin.Text = $"Mã tin: {tb.MaTin} (#{tb.MaTinBao}) — Trạng thái: {tb.NhanTrangThai}";
            lblTbLoaiUuTien.Text = $"Loại: {tb.TenLoaiTinBao ?? "Chưa phân loại"}  |  Ưu tiên: {tb.NhanMucDoUuTien}  |  Nguồn: {tb.NhanNguonTin}";
            lblTbTieuDe.Text = $"Tiêu đề: {tb.TieuDe}";
            lblTbThoiGianDiaDiem.Text = $"Thời gian xảy ra: {tb.ThoiGianXayRa:dd/MM/yyyy HH:mm}  |  Địa điểm: {tb.DiaDiem}";
            var nguoiBao = string.IsNullOrWhiteSpace(tb.HoTenNguoiBao) ? "Ẩn danh / Cán bộ" : tb.HoTenNguoiBao;
            var sdt = string.IsNullOrWhiteSpace(tb.SoDienThoaiNguoiBao) ? "Không có" : tb.SoDienThoaiNguoiBao;
            lblTbNguoiBao.Text = $"Người báo: {nguoiBao}  |  SĐT: {sdt}  |  Tiếp nhận: {tb.NgayTiepNhan:dd/MM/yyyy HH:mm}";
            txtTbNoiDung.Text = tb.NoiDung;
        }

        // 3. Tab 2: Đặc điểm & Đồ vật cá nhân
        if (dto.CheDoHanCheNhaXac)
        {
            lblDacDiemHanChe.Visible = true;
            lblDacDiemHanChe.Text = "🔒 Đặc điểm cơ thể chi tiết được ẩn theo phân quyền Nhà xác (v_HoSoThiTheNhaXac).";
            txtDacDiem.Visible = false;
            btnLuuDacDiem.Visible = false;
        }
        else
        {
            lblDacDiemHanChe.Visible = false;
            txtDacDiem.Visible = true;
            txtDacDiem.Text = dto.DacDiemCoThe ?? string.Empty;
            txtDacDiem.ReadOnly = !canEditInfo;
            btnLuuDacDiem.Visible = true;
            btnLuuDacDiem.Enabled = canEditInfo;
        }

        dgvDoVat.DataSource = dto.DoVatCaNhan;
        lblDemDoVat.Text = dto.DoVatCaNhan.Count == 0
            ? "Chưa ghi nhận đồ vật cá nhân kèm theo hồ sơ này."
            : $"Tổng số đồ vật cá nhân: {dto.DoVatCaNhan.Count}";
        tabDacDiemDoVat.Text = $"Đặc điểm & Đồ vật ({dto.DoVatCaNhan.Count})";

        // 4. Tab 3: Hình ảnh
        dgvHinhAnh.DataSource = dto.HinhAnh;
        lblDemHinhAnh.Text = !dto.CoQuyenXemHinhAnh
            ? "Bạn không có quyền xem hình ảnh của hồ sơ này."
            : (dto.HinhAnh.Count == 0 ? "Chưa có hình ảnh nào gắn với hồ sơ thi thể." : $"Tổng số hình ảnh: {dto.HinhAnh.Count}");
        btnTaiThemAnh.Visible = AccessControl.Can(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionCreate);
        btnTaiThemAnh.Enabled = !isClosed;
        btnXemChiTietAnh.Enabled = dto.HinhAnh.Count > 0;
        tabHinhAnh.Text = $"Hình ảnh ({dto.HinhAnh.Count})";
        HienThiXemTruocAnh();

        // 5. Tab 4: Lưu trữ & Lịch sử
        if (dto.ViTriHienTai != null)
        {
            var vt = dto.ViTriHienTai;
            lblViTriChiTiet.Text = $"Ngăn hiện tại: {vt.ViTriHienThi} (Mã vị trí #{vt.MaViTri})";
            lblViTriPhu.Text = $"Sức chứa ngăn: {vt.SucChua}  |  Thời điểm xếp gần nhất: {vt.ThoiGianXep:dd/MM/yyyy HH:mm:ss}  |  Số lần xếp/chuyển: {dto.LichSuLuuTru.Count}";
        }
        else
        {
            lblViTriChiTiet.Text = "Chưa được xếp vào vị trí ngăn lạnh.";
            lblViTriPhu.Text = $"Số lần xếp/chuyển trong lịch sử: {dto.LichSuLuuTru.Count}";
        }
        dgvLichSuLuuTru.DataSource = dto.LichSuLuuTru;
        dgvLichSuTrangThai.DataSource = dto.LichSuTrangThai;
        tabLuuTru.Text = $"Lưu trữ & Lịch sử ({dto.LichSuLuuTru.Count})";

        // 6. Tab 5: Kết quả Đối sánh (ẩn hoàn toàn khi không có quyền hoặc chế độ Nhà xác)
        var showMatchingTab = dto.CoQuyenXemDoiSanh && !dto.CheDoHanCheNhaXac;
        if (showMatchingTab)
        {
            if (!tabMain.TabPages.Contains(tabDoiSanh))
                tabMain.TabPages.Add(tabDoiSanh);
            dgvDoiSanh.DataSource = dto.KetQuaDoiSanh;
            lblDemDoiSanh.Text = dto.KetQuaDoiSanh.Count == 0
                ? "Chưa có kết quả đối sánh nào với hồ sơ người mất tích."
                : $"Tổng số ứng viên đối sánh: {dto.KetQuaDoiSanh.Count} (sắp xếp theo điểm tương đồng giảm dần)";
            tabDoiSanh.Text = $"Kết quả Đối sánh ({dto.KetQuaDoiSanh.Count})";
        }
        else if (tabMain.TabPages.Contains(tabDoiSanh))
        {
            tabMain.TabPages.Remove(tabDoiSanh);
        }
    }

    private void HienThiXemTruocAnh()
    {
        var old = _previewBitmap;
        _previewBitmap = null;
        picPreview.Image = null;
        old?.Dispose();

        if (dgvHinhAnh.CurrentRow?.DataBoundItem is not BodyImageItemDto img)
        {
            lblAnhMeta.Text = "Chọn một hình ảnh trong danh sách để xem trước (nhấp đúp để mở trình xem phóng to).";
            btnXemChiTietAnh.Enabled = false;
            return;
        }

        btnXemChiTietAnh.Enabled = true;
        lblAnhMeta.Text = $"{img.TenGoc} ({img.KichThuocHienThi})\nLoại: {img.LoaiHinhAnh} · {img.NhanNhayCam} · Người tải: {img.NguoiTaiLen}";

        if (img.DuongDan == "PRIVATE_PROTECTED_IMAGE")
        {
            lblAnhMeta.Text += "\nẢnh được bảo vệ bởi phân quyền hệ thống.";
            return;
        }

        try
        {
            using var db = Program.NewDb();
            var imageService = new ImageService(db);
            var physicalPath = imageService.LayDuongDanVatLy(img.DuongDan);
            if (File.Exists(physicalPath))
            {
                using var ms = new MemoryStream(File.ReadAllBytes(physicalPath));
                using var temp = System.Drawing.Image.FromStream(ms);
                _previewBitmap = new Bitmap(temp);
                picPreview.Image = _previewBitmap;
            }
        }
        catch
        {
            // Giữ khung xem trước trống nếu tệp vật lý không tồn tại trên máy hiện tại
        }
    }

    private async Task MoChiTietAnhAsync()
    {
        if (dgvHinhAnh.CurrentRow?.DataBoundItem is not BodyImageItemDto img)
            return;

        if (img.DuongDan == "PRIVATE_PROTECTED_IMAGE")
        {
            MessageBox.Show(this, "Bạn không có quyền xem đường dẫn vật lý của hình ảnh nhạy cảm này.", "Giới hạn quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ImageDto fullDto;
        try
        {
            using var db = Program.NewDb();
            var entity = await db.Images.AsNoTracking().FirstOrDefaultAsync(x => x.MaHinhAnh == img.MaHinhAnh);
            fullDto = new ImageDto(
                img.MaHinhAnh,
                entity?.TenFile ?? Path.GetFileName(img.DuongDan),
                img.TenGoc,
                img.DuongDan,
                img.LoaiHinhAnh,
                img.DinhDang,
                img.KichThuoc,
                entity?.ChieuRong,
                entity?.ChieuCao,
                entity?.HashMD5,
                img.LaNhayCam,
                img.MoTa,
                _maHoSoThiThe,
                entity?.MaHoSoMatTich,
                entity?.MaTinBao,
                entity?.MaXacMinh,
                entity?.MaTaiKhoanTaiLen ?? 0,
                img.NguoiTaiLen,
                img.ThoiGianTaiLen,
                entity?.TrangThai ?? "HoatDong");
        }
        catch
        {
            fullDto = new ImageDto(
                img.MaHinhAnh,
                Path.GetFileName(img.DuongDan),
                img.TenGoc,
                img.DuongDan,
                img.LoaiHinhAnh,
                img.DinhDang,
                img.KichThuoc,
                null,
                null,
                null,
                img.LaNhayCam,
                img.MoTa,
                _maHoSoThiThe,
                null,
                null,
                null,
                0,
                img.NguoiTaiLen,
                img.ThoiGianTaiLen,
                "HoatDong");
        }

        using var dlg = new FrmXemChiTietAnh(fullDto, _previewBitmap);
        dlg.ShowDialog(this);
    }

    private async Task XuLyChuyenTrangThaiAsync()
    {
        if (!PermissionUi.Check(this, AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate))
            return;

        if (cboTrangThaiMoi.SelectedItem is not StatusOption selected)
        {
            MessageBox.Show(this, "Vui lòng chọn trạng thái mới hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lyDo = txtLyDo.Text.Trim();
        if (_current?.TrangThai == Body.StatusDongHoSo && selected.Code == Body.StatusTiepTucLuuGiu && string.IsNullOrWhiteSpace(lyDo))
        {
            MessageBox.Show(this, "Mở lại hồ sơ đã đóng bắt buộc phải nhập lý do.", "Yêu cầu nhập lý do", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtLyDo.Focus();
            return;
        }

        // Xác nhận trước khi chuyển trạng thái để tránh bấm nhầm (đặc biệt khi Đóng hồ sơ)
        var canhBao = selected.Code == Body.StatusDongHoSo
            ? "\n\n⚠ Lưu ý: Sau khi Đóng hồ sơ, toàn bộ thông tin sẽ bị khóa (chỉ cho xem) và muốn sửa lại phải Mở lại hồ sơ kèm lý do."
            : string.Empty;
        var confirm = MessageBox.Show(
            this,
            $"Xác nhận chuyển trạng thái hồ sơ #{_maHoSoThiThe}?\n\n• Hiện tại: {_current?.NhanTrangThai}\n• Chuyển sang: {selected.Label}{canhBao}",
            "Xác nhận chuyển trạng thái",
            MessageBoxButtons.YesNo,
            selected.Code == Body.StatusDongHoSo ? MessageBoxIcon.Warning : MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes)
            return;

        btnChuyenTrangThai.Enabled = false;
        try
        {
            using var db = Program.NewDb();
            var service = new BodyService(new BodyRepository(db));
            await service.ChuyenTrangThaiAsync(_maHoSoThiThe, selected.Code, string.IsNullOrWhiteSpace(lyDo) ? null : lyDo);

            _changed = true;
            txtLyDo.Clear();
            await TaiDuLieuAsync();
            lblThongBao.Text = $"✅ Đã chuyển trạng thái hồ sơ sang '{selected.Label}' thành công.";
            lblThongBao.ForeColor = UITheme.Green;
        }
        catch (Exception ex)
        {
            lblThongBao.Text = "⛔ " + ex.Message;
            lblThongBao.ForeColor = UITheme.Red;
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            btnChuyenTrangThai.Enabled = cboTrangThaiMoi.Items.Count > 0 &&
                                         AccessControl.Can(AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate);
        }
    }

    private async Task XuLyLuuThongTinAsync()
    {
        if (!PermissionUi.Check(this, AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate))
            return;

        btnLuuThongTin.Enabled = btnLuuDacDiem.Enabled = false;
        try
        {
            var req = new UpdateBodyRequest
            {
                ThoiGianPhatHien = dtpThoiGian.Value,
                DiaDiem          = txtDiaDiem.Text.Trim(),
                GioiTinh         = cboGioiTinh.SelectedItem?.ToString(),
                TuoiUocLuong     = chkTuoiNull.Checked ? null : (short)nudTuoi.Value,
                ChieuCao         = chkChieuCaoNull.Checked ? null : nudChieuCao.Value,
                DacDiemCoThe     = txtDacDiem.Text.Trim()
            };

            using var db = Program.NewDb();
            var service = new BodyService(new BodyRepository(db));
            await service.CapNhatAsync(_maHoSoThiThe, req);

            _changed = true;
            await TaiDuLieuAsync();
            lblThongBao.Text = "✅ Đã lưu cập nhật thông tin hồ sơ thành công.";
            lblThongBao.ForeColor = UITheme.Green;
        }
        catch (Exception ex)
        {
            lblThongBao.Text = "⛔ " + ex.Message;
            lblThongBao.ForeColor = UITheme.Red;
            MessageBox.Show(this, ex.Message, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            var canEdit = _current != null
                          && !_current.CheDoHanCheNhaXac
                          && _current.TrangThai != Body.StatusDongHoSo
                          && AccessControl.Can(AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate);
            btnLuuThongTin.Enabled = btnLuuDacDiem.Enabled = canEdit;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewBitmap?.Dispose();
            _previewBitmap = null;
        }
        base.Dispose(disposing);
    }
}
