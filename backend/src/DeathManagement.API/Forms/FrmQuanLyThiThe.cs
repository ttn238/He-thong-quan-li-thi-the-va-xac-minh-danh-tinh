using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Security;
using DeathManagement.API.Services;

namespace DeathManagement.API.Forms;

public class FrmQuanLyThiThe : Form
{
    private readonly TextBox keyword = new() { Width = 210, PlaceholderText = "Mã hồ sơ / địa điểm / đặc điểm", MaxLength = 255 };
    private readonly ComboBox status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175 };
    private readonly ComboBox gender = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 135 };
    private readonly DateTimePicker from = new() { ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short, Width = 115 };
    private readonly DateTimePicker to = new() { ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short, Width = 115 };
    private readonly DataGridView grid = new()
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
        BorderStyle = BorderStyle.None
    };
    private readonly Label message = new() { AutoSize = true, Padding = new Padding(8, 8, 8, 0), Font = UITheme.FontBold(9), ForeColor = UITheme.Navy };
    private readonly Button previous = new() { Text = "◀ Trang trước", AutoSize = true };
    private readonly Button next = new() { Text = "Trang sau ▶", AutoSize = true };
    private readonly Button search = new() { Text = "Tìm kiếm", AutoSize = true, BackColor = UITheme.Blue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    private readonly Button detailBtn = new() { Text = "👁 Chi tiết & Trạng thái", AutoSize = true, BackColor = UITheme.Soft, ForeColor = UITheme.Navy, FlatStyle = FlatStyle.Flat };
    private readonly Button create = new() { Text = "+ Lập hồ sơ mới", AutoSize = true, BackColor = UITheme.Navy, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    private int page = 1;
    private int total;
    private bool loading;
    private BodyFilterRequest active = new();

    private sealed record FilterOption(string? Code, string Label)
    {
        public override string ToString() => Label;
    }

    public FrmQuanLyThiThe()
    {
        Text = "Danh sách hồ sơ thi thể & Quản lý trạng thái";
        Size = new Size(1240, 720);
        MinimumSize = new Size(960, 540);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        // Bộ lọc trạng thái hiển thị nhãn tiếng Việt thống nhất
        status.Items.Add(new FilterOption(null, "-- Tất cả trạng thái --"));
        foreach (var s in Body.ValidStatuses)
            status.Items.Add(new FilterOption(s, $"{Body.GetStatusLabel(s)} ({s})"));

        gender.Items.AddRange(
        [
            new FilterOption(null, "-- Tất cả giới tính --"),
            new FilterOption("Nam", "Nam"),
            new FilterOption("Nu", "Nữ"),
            new FilterOption("Khac", "Khác"),
            new FilterOption("KhongXacDinh", "Không xác định")
        ]);
        status.SelectedIndex = gender.SelectedIndex = 0;

        // Header Banner
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.Navy };
        pnlHeader.Controls.Add(new Label
        {
            Text = "QUẢN LÝ HỒ SƠ THI THỂ & TRẠNG THÁI NGHIỆP VỤ",
            Font = UITheme.FontBold(11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(18, 10)
        });
        pnlHeader.Controls.Add(new Label
        {
            Text = "Tra cứu danh sách, theo dõi nhãn màu trạng thái, xem chi tiết, chuyển trạng thái theo quy tắc thống nhất và xem lịch sử truy vết",
            Font = UITheme.FontRegular(8.5f),
            ForeColor = UITheme.Gold,
            AutoSize = true,
            Location = new Point(18, 34)
        });

        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(12, 12, 12, 8),
            BackColor = Color.White,
            AutoScroll = true,
            WrapContents = false
        };

        create.Visible = AccessControl.Can(AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionCreate);
        create.Click += async (_, _) =>
        {
            if (!PermissionUi.Check(this, AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionCreate)) return;
            using var f = new FrmLapHoSoThiThe();
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                page = 1;
                await LoadPageAsync();
            }
        };

        detailBtn.Click += async (_, _) => await OpenSelectedDetailAsync();

        filters.Controls.AddRange(
        [
            keyword,
            status,
            gender,
            new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(6, 6, 2, 0) },
            from,
            new Label { Text = "Đến ngày:", AutoSize = true, Margin = new Padding(6, 6, 2, 0) },
            to,
            search,
            detailBtn,
            create
        ]);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = Color.White
        };
        footer.Controls.AddRange([previous, next, message]);

        // Cấu hình bảng hiển thị
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Navy;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = UITheme.FontBold(9f);
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 32;

        AddColumn(nameof(BodyDto.MaHoSoThiThe), "Mã hồ sơ", 55);
        AddColumn(nameof(BodyDto.ThoiGianPhatHien), "Thời gian phát hiện", 95);
        grid.Columns[nameof(BodyDto.ThoiGianPhatHien)]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        AddColumn(nameof(BodyDto.NhanTrangThai), "Trạng thái", 95);
        AddColumn(nameof(BodyDto.NhanGioiTinh), "Giới tính", 70);
        AddColumn(nameof(BodyDto.TuoiUocLuong), "Tuổi ước lượng", 65);
        AddColumn(nameof(BodyDto.ViTriLuuTruHienThi), "Vị trí lưu trữ", 90);

        var restricted = AccessControl.RoleCode == AuthorizationPolicies.RoleMorgue
                         || AccessControl.ScopeOf("BODY", "VIEW") == AuthorizationPolicies.ScopePartial;
        if (restricted)
        {
            keyword.PlaceholderText = "Tìm mã hồ sơ";
        }
        else
        {
            AddColumn(nameof(BodyDto.DiaDiem), "Địa điểm phát hiện", 140);
            AddColumn(nameof(BodyDto.DacDiemCoThe), "Đặc điểm nhận dạng", 150);
        }

        // Hiển thị nhãn màu trạng thái nhất quán theo quy chuẩn trong Entity Body
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name == nameof(BodyDto.NhanTrangThai) &&
                grid.Rows[e.RowIndex].DataBoundItem is BodyDto body)
            {
                var bg = ColorTranslator.FromHtml(body.MauTrangThai);
                var fg = ColorTranslator.FromHtml(body.MauChuTrangThai);
                e.CellStyle!.BackColor = bg;
                e.CellStyle.ForeColor = fg;
                e.CellStyle.SelectionBackColor = bg;
                e.CellStyle.SelectionForeColor = fg;
                e.CellStyle.Font = UITheme.FontBold(9f);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        };

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenSelectedDetailAsync();
        };

        var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = UITheme.Bg };
        pnlGrid.Controls.Add(grid);

        Controls.Add(pnlGrid);
        Controls.Add(footer);
        Controls.Add(filters);
        Controls.Add(pnlHeader);

        search.Click += async (_, _) =>
        {
            active = new BodyFilterRequest
            {
                TuKhoa = keyword.Text.Trim(),
                TrangThai = (status.SelectedItem as FilterOption)?.Code,
                GioiTinh = (gender.SelectedItem as FilterOption)?.Code,
                TuNgay = from.Checked ? from.Value.Date : null,
                DenNgay = to.Checked ? to.Value.Date.AddDays(1).AddTicks(-1) : null
            };
            page = 1;
            await LoadPageAsync();
        };
        previous.Click += async (_, _) => { page--; await LoadPageAsync(); };
        next.Click += async (_, _) => { page++; await LoadPageAsync(); };
        Shown += async (_, _) => await LoadPageAsync();
        AcceptButton = search;
    }

    private void AddColumn(string property, string title, float fillWeight = 100) => grid.Columns.Add(
        new DataGridViewTextBoxColumn
        {
            Name = property,
            DataPropertyName = property,
            HeaderText = title,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

    private async Task OpenSelectedDetailAsync()
    {
        if (grid.CurrentRow?.DataBoundItem is not BodyDto selected)
        {
            MessageBox.Show(this, "Vui lòng chọn một hồ sơ thi thể trong danh sách để xem chi tiết hoặc chuyển trạng thái.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!PermissionUi.Check(this, AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionView))
            return;

        using var dlg = new FrmChiTietThiThe(selected.MaHoSoThiThe);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await LoadPageAsync();
        }
    }

    private async Task LoadPageAsync()
    {
        if (loading) return;
        loading = true;
        search.Enabled = previous.Enabled = next.Enabled = detailBtn.Enabled = false;
        message.Text = "Đang tải hồ sơ…";
        try
        {
            using var db = Program.NewDb();
            active.Trang = page;
            var result = await new BodyService(new BodyRepository(db)).LayDanhSachAsync(active);
            if (IsDisposed) return;
            total = result.Total;
            grid.DataSource = result.Items;
            message.Text = total == 0
                ? "Không có hồ sơ phù hợp."
                : $"Trang {page} / {Math.Max(1, (total + 19) / 20)} · Tổng {total} hồ sơ (Nhấp đúp vào dòng hoặc bấm [Chi tiết & Trạng thái] để xem/chuyển trạng thái)";
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            grid.DataSource = null;
            total = 0;
            message.Text = ex is ForbiddenException or UnauthenticatedException or ArgumentException
                ? ex.Message : "Không tải được dữ liệu. Kiểm tra kết nối và thử lại.";
        }
        finally
        {
            loading = false;
            if (!IsDisposed)
            {
                search.Enabled = true;
                detailBtn.Enabled = total > 0;
                previous.Enabled = page > 1;
                next.Enabled = (long)page * 20 < total;
            }
        }
    }
}

/// <summary>
/// Màn hình chi tiết hồ sơ thi thể, hiển thị nhãn màu trạng thái nhất quán,
/// cho phép chuyển đổi trạng thái hợp lệ, cập nhật thông tin và xem lịch sử truy vết trạng thái.
/// </summary>
public sealed class FrmChiTietThiThe : Form
{
    private readonly long _maHoSoThiThe;
    private BodyDto? _current;
    private bool _changed;

    // Nhãn trạng thái nổi bật (Badge màu)
    private readonly Label lblBadgeTrangThai = new()
    {
        AutoSize = false,
        Size = new Size(190, 32),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = UITheme.FontBold(9.5f)
    };

    private readonly Label lblMaHoSo = new() { AutoSize = true, Font = UITheme.FontHeading(12f), ForeColor = UITheme.Navy };
    private readonly Label lblViTri = new() { AutoSize = true, Font = UITheme.FontBold(9.5f), ForeColor = UITheme.Blue };
    private readonly Label lblThongBao = new() { AutoSize = true, Font = UITheme.FontRegular(9f), ForeColor = UITheme.Muted };

    // Các trường thông tin hồ sơ
    private readonly DateTimePicker dtpThoiGian = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
        Width = 200
    };
    private readonly TextBox txtDiaDiem = new() { Dock = DockStyle.Fill, MaxLength = 255 };
    private readonly ComboBox cboGioiTinh = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private readonly NumericUpDown nudTuoi = new() { Minimum = 0, Maximum = 130, Width = 90 };
    private readonly CheckBox chkTuoiNull = new() { Text = "Không rõ", AutoSize = true };
    private readonly NumericUpDown nudChieuCao = new() { Minimum = 1, Maximum = 300, DecimalPlaces = 1, Value = 165, Width = 90 };
    private readonly CheckBox chkChieuCaoNull = new() { Text = "Không rõ", AutoSize = true };
    private readonly TextBox txtDacDiem = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Height = 75 };
    private readonly Button btnLuuThongTin = new()
    {
        Text = "💾 LƯU THÔNG TIN HỒ SƠ",
        Size = new Size(190, 34),
        BackColor = UITheme.Blue,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };

    // Khu vực chuyển đổi trạng thái
    private readonly ComboBox cboTrangThaiMoi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
    private readonly TextBox txtLyDo = new()
    {
        Width = 310,
        PlaceholderText = "Nhập lý do / ghi chú chuyển trạng thái (bắt buộc khi mở lại hồ sơ đã đóng)...",
        MaxLength = 400
    };
    private readonly Button btnChuyenTrangThai = new()
    {
        Text = "🔄 CHUYỂN TRẠNG THÁI",
        Size = new Size(175, 34),
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };
    private readonly Label lblGoiYChuyen = new() { AutoSize = true, Font = UITheme.FontRegular(8.5f), ForeColor = UITheme.Muted };

    // Bảng lịch sử thay đổi trạng thái (Truy vết)
    private readonly DataGridView dgvLichSu = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoGenerateColumns = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None
    };

    private sealed record StatusOption(string Code, string Label)
    {
        public override string ToString() => $"{Label} ({Code})";
    }

    public FrmChiTietThiThe(long maHoSoThiThe)
    {
        _maHoSoThiThe = maHoSoThiThe;
        Text = $"Chi tiết hồ sơ thi thể #{maHoSoThiThe} & Quản lý trạng thái";
        Size = new Size(980, 730);
        MinimumSize = new Size(860, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        Shown += async (_, _) => await TaiDuLieuAsync();
    }

    private void BuildUI()
    {
        cboGioiTinh.Items.AddRange(["KhongXacDinh", "Nam", "Nu", "Khac"]);
        chkTuoiNull.CheckedChanged += (_, _) => nudTuoi.Enabled = !chkTuoiNull.Checked;
        chkChieuCaoNull.CheckedChanged += (_, _) => nudChieuCao.Enabled = !chkChieuCaoNull.Checked;

        // 1. Top Banner + Status Badge
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White, Padding = new Padding(18, 12, 18, 10) };
        pnlTop.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };

        lblMaHoSo.Location = new Point(18, 10);
        lblViTri.Location = new Point(18, 36);
        lblBadgeTrangThai.Location = new Point(740, 16);
        lblBadgeTrangThai.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pnlTop.Controls.AddRange([lblMaHoSo, lblViTri, lblBadgeTrangThai]);

        // 2. Khu vực Chuyển trạng thái theo State Machine
        var grpStatus = new GroupBox
        {
            Text = "Quy trình Chuyển đổi Trạng thái Hồ sơ (State Machine)",
            Dock = DockStyle.Top,
            Height = 115,
            Padding = new Padding(14, 10, 14, 10),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };

        var flowStatus = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        flowStatus.Controls.AddRange(
        [
            new Label { Text = "Trạng thái kế tiếp:", AutoSize = true, Margin = new Padding(0, 8, 6, 0), Font = UITheme.FontBold(9f) },
            cboTrangThaiMoi,
            new Label { Text = "Lý do / Ghi chú:", AutoSize = true, Margin = new Padding(10, 8, 6, 0), Font = UITheme.FontBold(9f) },
            txtLyDo,
            btnChuyenTrangThai
        ]);

        lblGoiYChuyen.Dock = DockStyle.Bottom;
        grpStatus.Controls.Add(flowStatus);
        grpStatus.Controls.Add(lblGoiYChuyen);

        // 3. Khu vực Thông tin chi tiết hồ sơ
        var grpInfo = new GroupBox
        {
            Text = "Thông tin Chi tiết Hồ sơ Thi thể",
            Dock = DockStyle.Top,
            Height = 250,
            Padding = new Padding(14, 8, 14, 8),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        tbl.Controls.Add(MakeLabel("Thời gian phát hiện:"), 0, 0);
        tbl.Controls.Add(dtpThoiGian, 1, 0);
        tbl.Controls.Add(MakeLabel("Giới tính ước tính:"), 2, 0);
        tbl.Controls.Add(cboGioiTinh, 3, 0);

        var pnlTuoi = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        pnlTuoi.Controls.AddRange([nudTuoi, chkTuoiNull]);
        var pnlChieuCao = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        pnlChieuCao.Controls.AddRange([nudChieuCao, new Label { Text = "cm", AutoSize = true, Margin = new Padding(2, 6, 6, 0) }, chkChieuCaoNull]);

        tbl.Controls.Add(MakeLabel("Tuổi ước lượng:"), 0, 1);
        tbl.Controls.Add(pnlTuoi, 1, 1);
        tbl.Controls.Add(MakeLabel("Chiều cao:"), 2, 1);
        tbl.Controls.Add(pnlChieuCao, 3, 1);

        tbl.Controls.Add(MakeLabel("Địa điểm phát hiện:"), 0, 2);
        tbl.Controls.Add(txtDiaDiem, 1, 2);
        tbl.SetColumnSpan(txtDiaDiem, 3);

        tbl.Controls.Add(MakeLabel("Đặc điểm cơ thể:"), 0, 3);
        tbl.Controls.Add(txtDacDiem, 1, 3);
        tbl.SetColumnSpan(txtDacDiem, 2);

        var pnlSaveBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        pnlSaveBtn.Controls.Add(btnLuuThongTin);
        tbl.Controls.Add(pnlSaveBtn, 3, 3);

        grpInfo.Controls.Add(tbl);

        // 4. Bảng Lịch sử thay đổi trạng thái (Audit Trail)
        var grpHistory = new GroupBox
        {
            Text = "Lịch sử Thay đổi Trạng thái & Truy vết Thao tác (NhatKyHeThong)",
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            BackColor = Color.White
        };

        dgvLichSu.EnableHeadersVisualStyles = false;
        dgvLichSu.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Navy;
        dgvLichSu.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvLichSu.ColumnHeadersDefaultCellStyle.Font = UITheme.FontBold(9f);
        dgvLichSu.ColumnHeadersHeight = 34;
        dgvLichSu.RowTemplate.Height = 30;

        dgvLichSu.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nameof(BodyStatusHistoryDto.ThoiGian),
            DataPropertyName = nameof(BodyStatusHistoryDto.ThoiGian),
            HeaderText = "Thời điểm",
            FillWeight = 85,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm:ss" }
        });
        dgvLichSu.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nameof(BodyStatusHistoryDto.HanhDong),
            DataPropertyName = nameof(BodyStatusHistoryDto.HanhDong),
            HeaderText = "Hành động",
            FillWeight = 75
        });
        dgvLichSu.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nameof(BodyStatusHistoryDto.NhanTrangThaiCu),
            DataPropertyName = nameof(BodyStatusHistoryDto.NhanTrangThaiCu),
            HeaderText = "Trạng thái cũ",
            FillWeight = 80
        });
        dgvLichSu.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nameof(BodyStatusHistoryDto.NhanTrangThaiMoi),
            DataPropertyName = nameof(BodyStatusHistoryDto.NhanTrangThaiMoi),
            HeaderText = "Trạng thái mới",
            FillWeight = 85
        });
        dgvLichSu.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nameof(BodyStatusHistoryDto.NguoiThucHien),
            DataPropertyName = nameof(BodyStatusHistoryDto.NguoiThucHien),
            HeaderText = "Người thực hiện",
            FillWeight = 110
        });
        dgvLichSu.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nameof(BodyStatusHistoryDto.LyDo),
            DataPropertyName = nameof(BodyStatusHistoryDto.LyDo),
            HeaderText = "Lý do / Ghi chú",
            FillWeight = 135
        });

        dgvLichSu.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvLichSu.Rows[e.RowIndex].DataBoundItem is not BodyStatusHistoryDto item) return;

            var colName = dgvLichSu.Columns[e.ColumnIndex].Name;
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

        grpHistory.Controls.Add(dgvLichSu);

        // 5. Thanh dưới
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White, Padding = new Padding(16, 8, 16, 8) };
        var btnDong = new Button
        {
            Text = "ĐÓNG",
            Size = new Size(95, 32),
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Soft,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(8.5f)
        };
        btnDong.Click += (_, _) =>
        {
            DialogResult = _changed ? DialogResult.OK : DialogResult.Cancel;
            Close();
        };
        lblThongBao.Dock = DockStyle.Left;
        pnlBottom.Controls.Add(lblThongBao);
        pnlBottom.Controls.Add(btnDong);

        var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = UITheme.Bg };
        pnlBody.Controls.Add(grpHistory);
        pnlBody.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        pnlBody.Controls.Add(grpInfo);
        pnlBody.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        pnlBody.Controls.Add(grpStatus);

        Controls.Add(pnlBody);
        Controls.Add(pnlBottom);
        Controls.Add(pnlTop);

        btnChuyenTrangThai.Click += async (_, _) => await XuLyChuyenTrangThaiAsync();
        btnLuuThongTin.Click += async (_, _) => await XuLyLuuThongTinAsync();
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = UITheme.FontBold(9f),
        ForeColor = UITheme.DarkText,
        Padding = new Padding(0, 0, 6, 0)
    };

    private async Task TaiDuLieuAsync()
    {
        try
        {
            using var db = Program.NewDb();
            var service = new BodyService(new BodyRepository(db));
            _current = await service.LayChiTietAsync(_maHoSoThiThe);

            if (_current == null)
            {
                MessageBox.Show(this, $"Không tìm thấy hồ sơ thi thể #{_maHoSoThiThe}.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            HienThiChiTiet(_current);

            var history = await service.LayLichSuTrangThaiAsync(_maHoSoThiThe);
            dgvLichSu.DataSource = history;
        }
        catch (Exception ex)
        {
            lblThongBao.Text = "⛔ " + ex.Message;
            lblThongBao.ForeColor = UITheme.Red;
        }
    }

    private void HienThiChiTiet(BodyDto dto)
    {
        lblMaHoSo.Text = $"HỒ SƠ THI THỂ #{dto.MaHoSoThiThe}" + (dto.MaTinBao.HasValue ? $" (Tin báo #{dto.MaTinBao})" : "");
        lblViTri.Text = $"📍 Vị trí lưu trữ hiện tại: {dto.ViTriLuuTruHienThi}";

        // Cập nhật nhãn màu trạng thái nhất quán với màn hình danh sách
        lblBadgeTrangThai.Text = dto.NhanTrangThai.ToUpperInvariant();
        lblBadgeTrangThai.BackColor = ColorTranslator.FromHtml(dto.MauTrangThai);
        lblBadgeTrangThai.ForeColor = ColorTranslator.FromHtml(dto.MauChuTrangThai);

        dtpThoiGian.Value = dto.ThoiGianPhatHien;
        txtDiaDiem.Text = dto.DiaDiem ?? string.Empty;
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

        if (dto.ChieuCao.HasValue && dto.ChieuCao.Value > 0)
        {
            chkChieuCaoNull.Checked = false;
            nudChieuCao.Value = Math.Clamp(dto.ChieuCao.Value, 1m, 300m);
        }
        else
        {
            chkChieuCaoNull.Checked = true;
        }

        txtDacDiem.Text = dto.DacDiemCoThe ?? string.Empty;

        // Nạp các trạng thái kế tiếp hợp lệ theo State Machine
        cboTrangThaiMoi.Items.Clear();
        foreach (var next in dto.TrangThaiTiepTheoHopLe)
        {
            cboTrangThaiMoi.Items.Add(new StatusOption(next, Body.GetStatusLabel(next)));
        }

        var canUpdate = AccessControl.Can(AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate);
        var restricted = AccessControl.RoleCode == AuthorizationPolicies.RoleMorgue
                         || AccessControl.ScopeOf("BODY", "UPDATE") == AuthorizationPolicies.ScopePartial;
        var isClosed = dto.TrangThai == Body.StatusDongHoSo;

        if (cboTrangThaiMoi.Items.Count > 0)
        {
            cboTrangThaiMoi.SelectedIndex = 0;
            cboTrangThaiMoi.Enabled = canUpdate;
            btnChuyenTrangThai.Enabled = canUpdate;
            var nextLabels = string.Join("  |  ", dto.TrangThaiTiepTheoHopLe.Select(s => $"{Body.GetStatusLabel(s)} ({s})"));
            lblGoiYChuyen.Text = $"ℹ Từ trạng thái [{dto.NhanTrangThai}], chỉ được phép chuyển sang: {nextLabels}.";
        }
        else
        {
            cboTrangThaiMoi.Enabled = false;
            btnChuyenTrangThai.Enabled = false;
            lblGoiYChuyen.Text = "ℹ Hồ sơ không có trạng thái chuyển tiếp.";
        }

        // Khóa sửa thông tin khi hồ sơ đã đóng hoặc thiếu quyền hoặc là nhân viên nhà xác (chỉ xem phần hạn chế)
        var canEditInfo = canUpdate && !restricted && !isClosed;
        dtpThoiGian.Enabled = txtDiaDiem.Enabled = cboGioiTinh.Enabled = txtDacDiem.Enabled = canEditInfo;
        chkTuoiNull.Enabled = chkChieuCaoNull.Enabled = canEditInfo;
        nudTuoi.Enabled = canEditInfo && !chkTuoiNull.Checked;
        nudChieuCao.Enabled = canEditInfo && !chkChieuCaoNull.Checked;
        btnLuuThongTin.Enabled = canEditInfo;
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

        btnChuyenTrangThai.Enabled = false;
        try
        {
            using var db = Program.NewDb();
            var service = new BodyService(new BodyRepository(db));
            await service.ChuyenTrangThaiAsync(_maHoSoThiThe, selected.Code, string.IsNullOrWhiteSpace(lyDo) ? null : lyDo);

            _changed = true;
            txtLyDo.Clear();
            lblThongBao.Text = $"✅ Đã chuyển trạng thái hồ sơ sang '{selected.Label}' thành công.";
            lblThongBao.ForeColor = UITheme.Green;

            await TaiDuLieuAsync();
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

        btnLuuThongTin.Enabled = false;
        try
        {
            var req = new UpdateBodyRequest
            {
                ThoiGianPhatHien = dtpThoiGian.Value,
                DiaDiem = txtDiaDiem.Text.Trim(),
                GioiTinh = cboGioiTinh.SelectedItem?.ToString(),
                TuoiUocLuong = chkTuoiNull.Checked ? null : (short)nudTuoi.Value,
                ChieuCao = chkChieuCaoNull.Checked ? null : nudChieuCao.Value,
                DacDiemCoThe = txtDacDiem.Text.Trim()
            };

            using var db = Program.NewDb();
            var service = new BodyService(new BodyRepository(db));
            await service.CapNhatAsync(_maHoSoThiThe, req);

            _changed = true;
            lblThongBao.Text = "✅ Đã lưu cập nhật thông tin hồ sơ thành công.";
            lblThongBao.ForeColor = UITheme.Green;

            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            lblThongBao.Text = "⛔ " + ex.Message;
            lblThongBao.ForeColor = UITheme.Red;
            MessageBox.Show(this, ex.Message, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            btnLuuThongTin.Enabled = _current?.TrangThai != Body.StatusDongHoSo &&
                                     AccessControl.Can(AuthorizationPolicies.ResourceBody, AuthorizationPolicies.ActionUpdate);
        }
    }
}
