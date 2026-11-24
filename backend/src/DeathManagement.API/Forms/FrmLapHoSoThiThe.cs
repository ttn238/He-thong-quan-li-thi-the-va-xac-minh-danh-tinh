using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Repositories;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Forms;

/// <summary>
/// Form WinForms lập hồ sơ thi thể mới (Task 6.1 — EPIC 6).
/// Cán bộ nhập đầy đủ thông tin rồi bấm [Lập hồ sơ] để gọi sp_LapHoSoThiThe.
/// </summary>
public sealed class FrmLapHoSoThiThe : Form
{
    // ───── Kết quả trả về ─────
    /// <summary>Mã hồ sơ vừa tạo thành công (null nếu hủy).</summary>
    public long? MaHoSoVuaTao { get; private set; }

    // ───── Controls ─────
    private readonly ErrorProvider _err = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };

    // Thời gian phát hiện
    private readonly DateTimePicker dtpThoiGian = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
        Font = UITheme.FontRegular(9.5f),
        Value = DateTime.Now,
        Dock = DockStyle.Fill
    };

    // Địa điểm
    private readonly TextBox txtDiaDiem = new()
    {
        MaxLength = 255,
        Font = UITheme.FontRegular(9.5f),
        PlaceholderText = "Vd: Số 10 Đường Trần Hưng Đạo, Quận 1, TP.HCM",
        Dock = DockStyle.Fill
    };

    // Giới tính
    private readonly ComboBox cboGioiTinh = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = UITheme.FontRegular(9.5f),
        Dock = DockStyle.Fill
    };

    // Tuổi ước lượng
    private readonly NumericUpDown nudTuoi = new()
    {
        Minimum = 0,
        Maximum = 130,
        Font = UITheme.FontRegular(9.5f),
        Dock = DockStyle.Fill,
        DecimalPlaces = 0
    };
    private readonly CheckBox chkTuoiKhongXacDinh = new()
    {
        Text = "Không xác định",
        Font = UITheme.FontRegular(9),
        AutoSize = true,
        Checked = true
    };

    // Chiều cao
    private readonly NumericUpDown nudChieuCao = new()
    {
        Minimum = 1,
        Maximum = 300,
        Value = 165,
        DecimalPlaces = 1,
        Font = UITheme.FontRegular(9.5f),
        Dock = DockStyle.Fill
    };
    private readonly CheckBox chkChieuCaoKhongXacDinh = new()
    {
        Text = "Không xác định",
        Font = UITheme.FontRegular(9),
        AutoSize = true,
        Checked = true
    };

    // Đặc điểm cơ thể
    private readonly TextBox txtDacDiem = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Font = UITheme.FontRegular(9.5f),
        Dock = DockStyle.Fill,
        PlaceholderText = "Vết sẹo, hình xăm, dị tật, trang phục lúc phát hiện…",
        MaxLength = 4000
    };

    // Tin báo liên kết
    private readonly ComboBox cboTinBao = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = UITheme.FontRegular(9.5f),
        Dock = DockStyle.Fill
    };

    // Thanh dưới
    private readonly Label lblTrangThai = new()
    {
        AutoSize = true,
        Font = UITheme.FontRegular(8.5f),
        ForeColor = UITheme.Muted
    };
    private readonly Button btnHuy  = TaoNut("HỦY BỎ", 100, Color.White, UITheme.Red);
    private readonly Button btnLap  = TaoNut("LẬP HỒ SƠ", 150, UITheme.Navy, Color.White);

    // ───────────── Constructor ─────────────
    public FrmLapHoSoThiThe()
    {
        KhoiTaoForm();
        GanSuKien();
    }

    // ───────────── Khởi tạo giao diện ─────────────
    private void KhoiTaoForm()
    {
        Text = "Lập hồ sơ thi thể";
        MinimumSize = new Size(680, 580);
        Size        = new Size(760, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor     = UITheme.Bg;
        Font = UITheme.FontRegular();

        // Giới tính
        cboGioiTinh.Items.AddRange(new object[] { "KhongXacDinh", "Nam", "Nu", "Khac" });
        cboGioiTinh.SelectedIndex = 0;

        // ── Layout chính (TableLayoutPanel 2 cột) ──
        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(16),
            AutoScroll = true
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Tiêu đề
        var lblTitle = new Label
        {
            Text = "🗂 LẬP HỒ SƠ THI THỂ",
            Font = UITheme.FontHeading(13),
            ForeColor = UITheme.Navy,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 10)
        };
        tbl.SetColumnSpan(lblTitle, 2);
        tbl.Controls.Add(lblTitle);

        // Dòng: Thời gian phát hiện
        ThemDong(tbl, "Thời gian phát hiện (*)", dtpThoiGian);

        // Dòng: Địa điểm
        var pnlDiaDiem = new Panel { Dock = DockStyle.Fill, Height = 28 };
        pnlDiaDiem.Controls.Add(txtDiaDiem);
        ThemDong(tbl, "Địa điểm phát hiện (*)", pnlDiaDiem);

        // Dòng: Giới tính
        ThemDong(tbl, "Giới tính ước tính", cboGioiTinh);

        // Dòng: Tuổi
        var pnlTuoi = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        nudTuoi.Width = 100;
        nudTuoi.Enabled = false;
        pnlTuoi.Controls.Add(chkTuoiKhongXacDinh);
        pnlTuoi.Controls.Add(nudTuoi);
        ThemDong(tbl, "Tuổi ước lượng (0–130)", pnlTuoi);

        // Dòng: Chiều cao
        var pnlChieuCao = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        nudChieuCao.Width = 100;
        nudChieuCao.Enabled = false;
        var lblCm = new Label { Text = "cm", Font = UITheme.FontRegular(9), AutoSize = true, Margin = new Padding(2, 6, 0, 0) };
        pnlChieuCao.Controls.Add(chkChieuCaoKhongXacDinh);
        pnlChieuCao.Controls.Add(nudChieuCao);
        pnlChieuCao.Controls.Add(lblCm);
        ThemDong(tbl, "Chiều cao", pnlChieuCao);

        // Dòng: Đặc điểm cơ thể
        txtDacDiem.Height = 90;
        ThemDong(tbl, "Đặc điểm cơ thể", txtDacDiem);

        // Dòng: Mã tin báo
        ThemDong(tbl, "Liên kết Tin báo", cboTinBao);

        Controls.Add(tbl);

        // ── Thanh dưới ──
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            Padding = new Padding(16, 8, 16, 8),
            BackColor = UITheme.Soft
        };
        var flowBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        flowBottom.Controls.Add(btnLap);
        flowBottom.Controls.Add(btnHuy);
        flowBottom.Controls.Add(lblTrangThai);
        pnlBottom.Controls.Add(flowBottom);
        Controls.Add(pnlBottom);
    }

    // ───────────── Gắn sự kiện ─────────────
    private void GanSuKien()
    {
        chkTuoiKhongXacDinh.CheckedChanged += (_, _) => nudTuoi.Enabled = !chkTuoiKhongXacDinh.Checked;
        chkChieuCaoKhongXacDinh.CheckedChanged += (_, _) => nudChieuCao.Enabled = !chkChieuCaoKhongXacDinh.Checked;
        btnHuy.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnLap.Click += async (_, _) => await XuLyLapHoSoAsync();

        Load += (_, _) =>
        {
            if (!AccessControl.Can("BODY", "CREATE"))
            {
                btnLap.Enabled = false;
                lblTrangThai.Text = "⛔ Bạn không có quyền lập hồ sơ thi thể.";
                lblTrangThai.ForeColor = UITheme.Red;
            }
        };
        Load += async (_, _) => await TaiDanhSachTinBaoAsync();
    }

    private async Task TaiDanhSachTinBaoAsync()
    {
        try
        {
            using var db = Program.NewDb();
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT MaTinBao, MaTin, TieuDe FROM dbo.TinBao ORDER BY NgayTiepNhan DESC";
            using var reader = await command.ExecuteReaderAsync();

            var options = new List<TinBaoOption> { new(null, "Không liên kết tin báo") };
            while (await reader.ReadAsync())
                options.Add(new TinBaoOption(reader.GetInt32(0), $"{reader.GetString(1)} — {reader.GetString(2)}"));

            cboTinBao.DisplayMember = nameof(TinBaoOption.TenHienThi);
            cboTinBao.ValueMember = nameof(TinBaoOption.MaTinBao);
            cboTinBao.DataSource = options;
            if (options.Count == 1) cboTinBao.Enabled = false;
        }
        catch (Exception ex)
        {
            cboTinBao.Enabled = false;
            lblTrangThai.Text = "Không tải được danh sách tin báo: " + ex.Message;
            lblTrangThai.ForeColor = UITheme.Red;
        }
    }

    // ───────────── Lập hồ sơ ─────────────
    private async Task XuLyLapHoSoAsync()
    {
        _err.Clear();
        lblTrangThai.Text = "";
        btnLap.Enabled = false;

        try
        {
            // Validate giao diện
            var diaDiem = txtDiaDiem.Text.Trim();
            if (string.IsNullOrEmpty(diaDiem))
            {
                _err.SetError(txtDiaDiem, "Vui lòng nhập địa điểm phát hiện.");
                lblTrangThai.Text = "⚠ Vui lòng kiểm tra lại thông tin.";
                lblTrangThai.ForeColor = UITheme.Red;
                return;
            }

            if (dtpThoiGian.Value > DateTime.Now)
            {
                _err.SetError(dtpThoiGian, "Thời gian không được ở tương lai.");
                lblTrangThai.Text = "⚠ Thời gian phát hiện không hợp lệ.";
                lblTrangThai.ForeColor = UITheme.Red;
                return;
            }

            var request = new CreateBodyRequest
            {
                ThoiGianPhatHien = dtpThoiGian.Value,
                DiaDiem          = diaDiem,
                GioiTinh         = cboGioiTinh.SelectedItem?.ToString() ?? "KhongXacDinh",
                TuoiUocLuong     = chkTuoiKhongXacDinh.Checked ? null : (short)nudTuoi.Value,
                ChieuCao         = chkChieuCaoKhongXacDinh.Checked ? null : nudChieuCao.Value,
                DacDiemCoThe     = string.IsNullOrWhiteSpace(txtDacDiem.Text) ? null : txtDacDiem.Text.Trim(),
                MaTinBao         = (cboTinBao.SelectedItem as TinBaoOption)?.MaTinBao
            };

            lblTrangThai.Text = "⏳ Đang lập hồ sơ…";
            lblTrangThai.ForeColor = UITheme.Blue;

            using var db = Program.NewDb();
            var service = new BodyService(new BodyRepository(db));
            var maHoSo = await service.TaoHoSoAsync(request);

            MaHoSoVuaTao = maHoSo;
            lblTrangThai.Text = $"✅ Đã lập hồ sơ thi thể #{maHoSo} thành công!";
            lblTrangThai.ForeColor = UITheme.Green;

            MessageBox.Show(
                $"Hồ sơ thi thể đã được lập thành công.\n\nMã hồ sơ: #{maHoSo}",
                "Lập hồ sơ thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (UnauthenticatedException ex)
        {
            lblTrangThai.Text = "⛔ " + ex.Message;
            lblTrangThai.ForeColor = UITheme.Red;
        }
        catch (ForbiddenException ex)
        {
            lblTrangThai.Text = "⛔ " + ex.Message;
            lblTrangThai.ForeColor = UITheme.Red;
        }
        catch (ArgumentException ex)
        {
            lblTrangThai.Text = "⚠ " + ex.Message;
            lblTrangThai.ForeColor = UITheme.Red;
        }
        catch (Exception ex)
        {
            lblTrangThai.Text = "❌ Lỗi: " + ex.Message;
            lblTrangThai.ForeColor = UITheme.Red;
        }
        finally
        {
            btnLap.Enabled = true;
        }
    }

    // ───────────── Tiện ích ─────────────
    private static void ThemDong(TableLayoutPanel tbl, string nhan, Control control)
    {
        var lbl = new Label
        {
            Text = nhan,
            Font = UITheme.FontBold(9),
            ForeColor = UITheme.DarkText,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 8, 0)
        };
        tbl.Controls.Add(lbl);
        tbl.Controls.Add(control);
    }

    private static Button TaoNut(string text, int width, Color back, Color fore) => new()
    {
        Text = text,
        Width = width,
        Height = 34,
        BackColor = back,
        ForeColor = fore,
        FlatStyle = FlatStyle.Flat,
        Font = UITheme.FontBold(9),
        Cursor = Cursors.Hand,
        Margin = new Padding(4, 4, 4, 4)
    };

    private sealed record TinBaoOption(int? MaTinBao, string TenHienThi);
}
