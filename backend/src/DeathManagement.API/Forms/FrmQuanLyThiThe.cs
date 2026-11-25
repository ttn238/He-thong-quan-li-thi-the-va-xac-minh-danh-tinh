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
    private readonly Button previous = new() { Text = "◀ Trang trước", AutoSize = true, Padding = new Padding(6, 2, 6, 2) };
    private readonly Button next = new() { Text = "Trang sau ▶", AutoSize = true, Padding = new Padding(6, 2, 6, 2) };
    private readonly Button search = new() { Text = "🔍 Tìm kiếm", AutoSize = true, BackColor = UITheme.Blue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Padding = new Padding(8, 2, 8, 2), Cursor = Cursors.Hand };
    private readonly Button detailBtn = new() { Text = "👁 Chi tiết && Trạng thái", AutoSize = true, BackColor = UITheme.Soft, ForeColor = UITheme.Navy, FlatStyle = FlatStyle.Flat, Padding = new Padding(8, 2, 8, 2), Cursor = Cursors.Hand };
    private readonly Button create = new() { Text = "+ Lập hồ sơ mới", AutoSize = true, BackColor = UITheme.Navy, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Padding = new Padding(8, 2, 8, 2), Cursor = Cursors.Hand };

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
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UITheme.Navy };
        pnlHeader.Controls.Add(new Label
        {
            Text = "QUẢN LÝ HỒ SƠ THI THỂ & TRẠNG THÁI NGHIỆP VỤ",
            UseMnemonic = false,
            Font = UITheme.FontBold(11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(18, 10)
        });
        pnlHeader.Controls.Add(new Label
        {
            Text = "Tra cứu danh sách, theo dõi nhãn màu trạng thái, xem chi tiết, chuyển trạng thái theo quy tắc thống nhất và xem lịch sử truy vết",
            UseMnemonic = false,
            Font = UITheme.FontRegular(8.5f),
            ForeColor = UITheme.Gold,
            AutoSize = true,
            Location = new Point(18, 36)
        });

        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 50),
            Padding = new Padding(12, 10, 12, 10),
            BackColor = Color.White,
            WrapContents = true
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
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.RowTemplate.Height = 32;

        AddColumn(nameof(BodyDto.MaHoSoThiThe), "Mã hồ sơ", 55);
        AddColumn(nameof(BodyDto.ThoiGianPhatHien), "Thời gian phát hiện", 95);
        grid.Columns[nameof(BodyDto.ThoiGianPhatHien)]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        AddColumn(nameof(BodyDto.NhanTrangThai), "Trạng thái", 95);
        AddColumn(nameof(BodyDto.NhanGioiTinh), "Giới tính", 68);
        AddColumn(nameof(BodyDto.TuoiUocLuong), "Tuổi ước tính", 78);
        AddColumn(nameof(BodyDto.ViTriLuuTruHienThi), "Vị trí lưu trữ", 95);

        var restricted = AccessControl.RoleCode == AuthorizationPolicies.RoleMorgue
                         || AccessControl.ScopeOf("BODY", "VIEW") == AuthorizationPolicies.ScopePartial;
        if (restricted)
        {
            keyword.PlaceholderText = "Tìm mã hồ sơ";
            AddColumn(nameof(BodyDto.Khu), "Khu", 60);
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

        Controls.Add(grid);
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
