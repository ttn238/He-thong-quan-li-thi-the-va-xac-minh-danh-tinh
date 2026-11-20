using System.Data;
using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;
using static DeathManagement.API.Security.AuthorizationPolicies;

namespace DeathManagement.API.Forms;

/// <summary>
/// Màn hình quản lý danh mục vị trí lưu trữ (Khu - Tủ - Ngăn) trên WinForms Desktop
/// </summary>
public class FrmQuanLyViTriLuuTru : Form
{
    private readonly IStorageService _storageService;
    private bool _isLoading = false;
    private bool _hasPendingSearch = false;
    private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 300 };

    private readonly DataGridView dgvLocations = new()
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

    private readonly TextBox txtTimKiem = new()
    {
        PlaceholderText = "Nhập Khu, Tủ, Ngăn...",
        Width = 230,
        Font = UITheme.FontRegular(9.5f)
    };

    private readonly Button btnTim = new()
    {
        Text = "TÌM",
        Size = new Size(55, 27),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand,
        Margin = new Padding(6, 0, 0, 0)
    };

    private readonly ComboBox cboLocKhu = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 150,
        Font = UITheme.FontRegular(9.5f)
    };

    private readonly Label lblTongSo = new()
    {
        AutoSize = true,
        Font = UITheme.FontBold(9.5f),
        ForeColor = UITheme.Navy
    };

    private readonly Button btnThem = new()
    {
        Text = "THÊM VỊ TRÍ",
        Size = new Size(130, 34),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };

    private readonly Button btnSua = new()
    {
        Text = "CHỈNH SỬA",
        Size = new Size(120, 34),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Blue,
        ForeColor = Color.White,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 10, 0)
    };

    private readonly Button btnXoa = new()
    {
        Text = "XÓA VỊ TRÍ",
        Size = new Size(120, 34),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Red,
        ForeColor = Color.White,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 10, 0)
    };

    private readonly Button btnDong = new()
    {
        Text = "ĐÓNG",
        Size = new Size(90, 34),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.WhiteSmoke,
        ForeColor = UITheme.Navy,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 0, 0)
    };

    public FrmQuanLyViTriLuuTru(IStorageService? storageService = null)
    {
        AccessControl.Require(ResourceStorage, ActionView);
        _storageService = storageService ?? new StorageService();

        Text = "Quản lý Vị trí Lưu trữ Thi thể (Khu - Tủ - Ngăn)";
        ClientSize = new Size(1020, 620);
        MinimumSize = new Size(880, 520);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        InitializeDataAsync();
    }

    private void BuildUI()
    {
        // 1. Header Banner
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = UITheme.Navy };
        var lblTitle = new Label
        {
            Text = "QUẢN LÝ VỊ TRÍ LƯU TRỮ THI THỂ (STORAGE LOCATIONS)",
            ForeColor = Color.White,
            Font = UITheme.FontBold(11),
            AutoSize = true,
            Location = new Point(20, 12)
        };
        var lblSub = new Label
        {
            Text = "Quản trị danh mục phân cấp vật lý: Khu vực -> Tủ lưu trữ -> Ngăn chứa, Sức chứa",
            ForeColor = UITheme.Gold,
            Font = UITheme.FontRegular(8.5f),
            AutoSize = true,
            Location = new Point(20, 36)
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // 2. Toolbar Panel (Top filter)
        var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White };
        pnlFilter.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, pnlFilter.Height - 1, pnlFilter.Width, pnlFilter.Height - 1);
        };

        var flpFilterLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(20, 13, 0, 0),
            WrapContents = false
        };

        var lblLocKhu = new Label
        {
            Text = "Khu vực:",
            AutoSize = true,
            Margin = new Padding(0, 4, 8, 0),
            Font = UITheme.FontBold(9f),
            ForeColor = UITheme.Navy
        };
        cboLocKhu.Margin = new Padding(0, 0, 20, 0);

        var lblTim = new Label
        {
            Text = "Tìm kiếm:",
            AutoSize = true,
            Margin = new Padding(0, 4, 8, 0),
            Font = UITheme.FontBold(9f),
            ForeColor = UITheme.Navy
        };
        txtTimKiem.Margin = new Padding(0, 0, 0, 0);

        btnTim.FlatAppearance.BorderSize = 0;
        btnTim.Click += async (s, e) =>
        {
            _searchTimer.Stop();
            await LoadDataAsync();
        };

        txtTimKiem.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _searchTimer.Stop();
                await LoadDataAsync();
            }
        };

        _searchTimer.Tick += async (s, e) =>
        {
            _searchTimer.Stop();
            await LoadDataAsync();
        };

        txtTimKiem.TextChanged += (s, e) =>
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        };

        cboLocKhu.SelectedIndexChanged += async (s, e) =>
        {
            _searchTimer.Stop();
            if (!_isLoading) await LoadDataAsync();
        };

        flpFilterLeft.Controls.AddRange(new Control[] { lblLocKhu, cboLocKhu, lblTim, txtTimKiem, btnTim });

        var flpFilterRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 20, 0),
            WrapContents = false
        };

        btnThem.FlatAppearance.BorderSize = 0;
        btnThem.Click += (s, e) => ShowAddEditDialog(null);
        btnThem.Visible = AccessControl.Can(ResourceStorage, ActionCreate);
        flpFilterRight.Controls.Add(btnThem);

        pnlFilter.Controls.AddRange(new Control[] { flpFilterLeft, flpFilterRight });

        // 3. Grid Container (Middle Fill)
        var pnlGridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 20, 15), BackColor = UITheme.Bg };
        var pnlGridBorder = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        pnlGridBorder.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlGridBorder.Width - 1, pnlGridBorder.Height - 1);
        };

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MaViTri", HeaderText = "Mã VT", FillWeight = 10 });
        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Khu", HeaderText = "Khu vực", FillWeight = 15 });
        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tu", HeaderText = "Tủ lưu trữ", FillWeight = 18 });
        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Ngan", HeaderText = "Ngăn chứa", FillWeight = 18 });
        
        var colSucChua = new DataGridViewTextBoxColumn { DataPropertyName = "SucChua", HeaderText = "Sức chứa", FillWeight = 14 };
        colSucChua.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvLocations.Columns.Add(colSucChua);

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ViTriDayDu", HeaderText = "Vị trí đầy đủ", FillWeight = 25 });

        dgvLocations.EnableHeadersVisualStyles = false;
        dgvLocations.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Navy;
        dgvLocations.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvLocations.ColumnHeadersDefaultCellStyle.Font = UITheme.FontBold(9.5f);
        dgvLocations.ColumnHeadersHeight = 38;
        dgvLocations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvLocations.RowTemplate.Height = 32;

        pnlGridBorder.Controls.Add(dgvLocations);
        pnlGridContainer.Controls.Add(pnlGridBorder);

        // 4. Bottom Action Bar (Dock = Bottom)
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.White };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        lblTongSo.Dock = DockStyle.Left;
        lblTongSo.Padding = new Padding(20, 0, 0, 0);
        lblTongSo.TextAlign = ContentAlignment.MiddleLeft;
        lblTongSo.AutoSize = true;

        btnSua.FlatAppearance.BorderSize = 0;
        btnSua.Click += (s, e) =>
        {
            var sel = GetSelectedLocation();
            if (sel != null) ShowAddEditDialog(sel);
        };

        btnXoa.FlatAppearance.BorderSize = 0;
        btnXoa.Click += (s, e) =>
        {
            var sel = GetSelectedLocation();
            if (sel != null) HandleDelete(sel);
        };

        btnDong.FlatAppearance.BorderColor = UITheme.Line;
        btnDong.Click += (s, e) => Close();

        btnSua.Visible = AccessControl.Can(ResourceStorage, ActionUpdate);
        btnXoa.Visible = AccessControl.Can(ResourceStorage, ActionDelete) || AccessControl.Can(ResourceStorage, ActionUpdate);

        // Dùng FlowLayoutPanel căn phải cho các nút thao tác dưới
        var flpActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 10, 20, 0),
            WrapContents = false
        };
        flpActions.Controls.AddRange(new Control[] { btnSua, btnXoa, btnDong });

        pnlBottom.Controls.AddRange(new Control[] { lblTongSo, flpActions });

        // Add controls in standard reverse dock order
        Controls.Add(pnlGridContainer);
        Controls.Add(pnlBottom);
        Controls.Add(pnlFilter);
        Controls.Add(pnlHeader);
    }

    private async void InitializeDataAsync()
    {
        await LoadKhuListAsync();
        await LoadDataAsync();
    }

    private async Task LoadKhuListAsync()
    {
        try
        {
            var currentKhu = cboLocKhu.SelectedItem?.ToString();
            var khuList = await _storageService.GetDistinctKhuListAsync();

            _isLoading = true;
            cboLocKhu.Items.Clear();
            cboLocKhu.Items.Add("-- Tất cả các Khu --");
            foreach (var k in khuList)
            {
                cboLocKhu.Items.Add(k);
            }

            if (!string.IsNullOrEmpty(currentKhu) && cboLocKhu.Items.Contains(currentKhu))
            {
                cboLocKhu.SelectedItem = currentKhu;
            }
            else
            {
                cboLocKhu.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi nạp danh sách Khu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadDataAsync()
    {
        if (_isLoading)
        {
            _hasPendingSearch = true;
            return;
        }

        _isLoading = true;
        _hasPendingSearch = false;

        try
        {
            var filter = new StorageLocationFilterRequest
            {
                Keyword = txtTimKiem.Text.Trim(),
                Khu = cboLocKhu.SelectedIndex > 0 ? cboLocKhu.SelectedItem?.ToString() : null,
                PageSize = 500
            };

            var result = await _storageService.GetPagedListAsync(filter);
            dgvLocations.DataSource = result.Items.ToList();
            lblTongSo.Text = $"Tổng số: {result.TotalCount} ngăn lưu trữ trong danh mục";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi nạp dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoading = false;
            if (_hasPendingSearch)
            {
                _hasPendingSearch = false;
                _ = LoadDataAsync();
            }
        }
    }

    private StorageLocationDto? GetSelectedLocation()
    {
        if (dgvLocations.CurrentRow?.DataBoundItem is StorageLocationDto dto)
            return dto;

        MessageBox.Show("Vui lòng bấm chọn một dòng vị trí lưu trữ từ bảng danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    private void ShowAddEditDialog(StorageLocationDto? location)
    {
        var isEdit = location != null;
        using var dlg = new Form
        {
            Text = isEdit ? $"Chỉnh sửa Vị trí #{location!.MaViTri}" : "Thêm mới Vị trí Lưu trữ",
            ClientSize = new Size(400, 290),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };

        var lblKhu = new Label { Text = "Khu vực:", Location = new Point(25, 25), AutoSize = true, Font = UITheme.FontBold(9) };
        var txtKhu = new TextBox { Text = location?.Khu ?? "", Location = new Point(130, 22), Width = 230, Font = UITheme.FontRegular(9.5f) };

        var lblTu = new Label { Text = "Tủ lưu trữ:", Location = new Point(25, 65), AutoSize = true, Font = UITheme.FontBold(9) };
        var txtTu = new TextBox { Text = location?.Tu ?? "", Location = new Point(130, 62), Width = 230, Font = UITheme.FontRegular(9.5f) };

        var lblNgan = new Label { Text = "Ngăn chứa:", Location = new Point(25, 105), AutoSize = true, Font = UITheme.FontBold(9) };
        var txtNgan = new TextBox { Text = location?.Ngan ?? "", Location = new Point(130, 102), Width = 230, Font = UITheme.FontRegular(9.5f) };

        var lblSucChua = new Label { Text = "Sức chứa:", Location = new Point(25, 145), AutoSize = true, Font = UITheme.FontBold(9) };
        var numSucChua = new NumericUpDown { Value = location?.SucChua ?? 1, Minimum = 1, Maximum = 100, Location = new Point(130, 142), Width = 100, Font = UITheme.FontRegular(9.5f) };

        var btnLuu = new Button
        {
            Text = isEdit ? "LƯU CẬP NHẬT" : "TẠO VỊ TRÍ",
            Location = new Point(130, 200),
            Size = new Size(130, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Navy,
            ForeColor = Color.White,
            Font = UITheme.FontBold(9),
            Cursor = Cursors.Hand
        };
        btnLuu.FlatAppearance.BorderSize = 0;

        var btnHuy = new Button
        {
            Text = "HỦY",
            Location = new Point(275, 200),
            Size = new Size(85, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.WhiteSmoke,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(9),
            Cursor = Cursors.Hand
        };
        btnHuy.FlatAppearance.BorderColor = UITheme.Line;
        btnHuy.Click += (s, e) => dlg.Close();

        btnLuu.Click += async (s, e) =>
        {
            var k = txtKhu.Text.Trim();
            var t = txtTu.Text.Trim();
            var n = txtNgan.Text.Trim();
            var sc = (int)numSucChua.Value;

            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(n))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Khu vực, Tủ và Ngăn lưu trữ.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (isEdit)
                {
                    await _storageService.UpdateAsync(location!.MaViTri, new UpdateStorageLocationRequest { Khu = k, Tu = t, Ngan = n, SucChua = sc });
                    MessageBox.Show("Cập nhật vị trí lưu trữ thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    await _storageService.CreateAsync(new CreateStorageLocationRequest { Khu = k, Tu = t, Ngan = n, SucChua = sc });
                    MessageBox.Show("Thêm mới vị trí lưu trữ thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                dlg.Close();
                await LoadKhuListAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        dlg.Controls.AddRange(new Control[] { lblKhu, txtKhu, lblTu, txtTu, lblNgan, txtNgan, lblSucChua, numSucChua, btnLuu, btnHuy });
        dlg.ShowDialog(this);
    }

    private async void HandleDelete(StorageLocationDto location)
    {
        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa vị trí lưu trữ [{location.Khu} - {location.Tu} - {location.Ngan}] (Mã #{location.MaViTri})?",
            "Xác nhận xóa vị trí",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _storageService.DeleteAsync(location.MaViTri);
            MessageBox.Show("Đã xóa vị trí lưu trữ thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadKhuListAsync();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
