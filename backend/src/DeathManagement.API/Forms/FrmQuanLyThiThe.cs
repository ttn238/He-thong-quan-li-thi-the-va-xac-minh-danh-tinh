using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Repositories;
using DeathManagement.API.Security;
using DeathManagement.API.Services;

namespace DeathManagement.API.Forms;

public class FrmQuanLyThiThe : Form
{
    private readonly TextBox keyword = new() { Width = 220, PlaceholderText = "Mã hồ sơ / địa điểm / đặc điểm", MaxLength = 255 };
    private readonly ComboBox status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly ComboBox gender = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly DateTimePicker from = new() { ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short, Width = 120 };
    private readonly DateTimePicker to = new() { ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short, Width = 120 };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AutoGenerateColumns = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false };
    private readonly Label message = new() { AutoSize = true, Padding = new Padding(8) };
    private readonly Button previous = new() { Text = "Trang trước", AutoSize = true };
    private readonly Button next = new() { Text = "Trang sau", AutoSize = true };
    private readonly Button search = new() { Text = "Tìm kiếm", AutoSize = true };
    private int page = 1;
    private int total;
    private bool loading;
    private BodyFilterRequest active = new();

    public FrmQuanLyThiThe()
    {
        Text = "Danh sách hồ sơ thi thể";
        Size = new Size(1200, 700);
        MinimumSize = new Size(900, 500);
        StartPosition = FormStartPosition.CenterParent;
        Font = UITheme.FontRegular(10);
        status.Items.AddRange(["Tất cả trạng thái", "MoiTiepNhan", "DangLuuGiu", "DangXacMinh", "DaXacDinh", "DaBanGiao", "DongHoSo", "TiepTucLuuGiu"]);
        gender.Items.AddRange(["Tất cả giới tính", "Nam", "Nu", "Khac", "KhongXacDinh"]);
        status.SelectedIndex = gender.SelectedIndex = 0;
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10), AutoScroll = true };
        filters.Controls.AddRange([keyword, status, gender, new Label { Text = "Từ ngày", AutoSize = true }, from,
            new Label { Text = "Đến ngày", AutoSize = true }, to, search]);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(8) };
        footer.Controls.AddRange([previous, next, message]);
        AddColumn(nameof(BodyDto.MaHoSoThiThe), "Mã hồ sơ");
        AddColumn(nameof(BodyDto.ThoiGianPhatHien), "Thời gian phát hiện");
        grid.Columns[nameof(BodyDto.ThoiGianPhatHien)]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        AddColumn(nameof(BodyDto.NhanTrangThai), "Trạng thái");
        AddColumn(nameof(BodyDto.NhanGioiTinh), "Giới tính");
        AddColumn(nameof(BodyDto.TuoiUocLuong), "Tuổi ước lượng");
        var restricted = AccessControl.RoleCode == "NHA_XAC" || AccessControl.ScopeOf("BODY", "VIEW") == "PARTIAL";
        if (restricted)
        {
            keyword.PlaceholderText = "Tìm mã hồ sơ";
            AddColumn(nameof(BodyDto.Khu), "Khu");
            AddColumn(nameof(BodyDto.Tu), "Tủ");
            AddColumn(nameof(BodyDto.Ngan), "Ngăn");
        }
        else
        {
            AddColumn(nameof(BodyDto.DiaDiem), "Địa điểm");
            AddColumn(nameof(BodyDto.DacDiemCoThe), "Đặc điểm");
        }
        grid.RowPrePaint += (_, e) =>
        {
            var row = grid.Rows[e.RowIndex];
            if (row.DataBoundItem is BodyDto body)
                row.Cells[nameof(BodyDto.NhanTrangThai)].Style.BackColor = body.TrangThai switch
                {
                    "MoiTiepNhan" => Color.LightBlue, "DangXacMinh" => Color.Khaki,
                    "DaXacDinh" => Color.LightGreen, "DaBanGiao" => Color.Thistle,
                    "DongHoSo" => Color.LightGray, _ => Color.Bisque
                };
        };
        Controls.Add(grid);
        Controls.Add(filters);
        Controls.Add(footer);
        search.Click += async (_, _) =>
        {
            active = new BodyFilterRequest { TuKhoa = keyword.Text.Trim(),
                TrangThai = status.SelectedIndex == 0 ? null : status.Text,
                GioiTinh = gender.SelectedIndex == 0 ? null : gender.Text,
                TuNgay = from.Checked ? from.Value.Date : null,
                DenNgay = to.Checked ? to.Value.Date.AddDays(1).AddTicks(-1) : null };
            page = 1;
            await LoadPageAsync();
        };
        previous.Click += async (_, _) => { page--; await LoadPageAsync(); };
        next.Click += async (_, _) => { page++; await LoadPageAsync(); };
        Shown += async (_, _) => await LoadPageAsync();
        AcceptButton = search;
    }

    private void AddColumn(string property, string title) => grid.Columns.Add(
        new DataGridViewTextBoxColumn { Name = property, DataPropertyName = property, HeaderText = title, SortMode = DataGridViewColumnSortMode.NotSortable });

    private async Task LoadPageAsync()
    {
        if (loading) return;
        loading = true;
        search.Enabled = previous.Enabled = next.Enabled = false;
        message.Text = "Đang tải hồ sơ…";
        try
        {
            using var db = Program.NewDb();
            active.Trang = page;
            var result = await new BodyService(new BodyRepository(db)).LayDanhSachAsync(active);
            if (IsDisposed) return;
            total = result.Total;
            grid.DataSource = result.Items;
            message.Text = total == 0 ? "Không có hồ sơ phù hợp." : $"Trang {page} / {Math.Max(1, (total + 19) / 20)} · {total} hồ sơ";
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
                previous.Enabled = page > 1;
                next.Enabled = (long)page * 20 < total;
            }
        }
    }
}
