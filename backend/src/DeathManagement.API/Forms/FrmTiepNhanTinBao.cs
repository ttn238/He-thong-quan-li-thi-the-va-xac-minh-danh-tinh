using DeathManagement.API.DTOs.Image;
using DeathManagement.API.DTOs.TinBao;
using DeathManagement.API.Helpers;
using DeathManagement.API.Services;
namespace DeathManagement.API.Forms;
using DeathManagement.API.Security;

public class FrmTiepNhanTinBao : Form
{
    private const int MaxMoTa = 2000;

    private readonly ErrorProvider _err = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private readonly ToolTip _tip = new();

    private static readonly string[] MucDoDb = { "KhanCap", "Cao", "BinhThuong", "Thap" };
    private int? _maTinBao;                       // có giá trị sau khi tạo tin báo thành công
    private readonly List<AnhChon> _dsAnh = new();
    private readonly ImageList _imgThumb = new() { ImageSize = new Size(40, 40), ColorDepth = ColorDepth.Color32 };
    private readonly ListView lvAnh = new()
    {
        Dock = DockStyle.Bottom,
        Height = 110,
        View = View.Details,
        FullRowSelect = true,
        HideSelection = false,
        MultiSelect = false,
        HeaderStyle = ColumnHeaderStyle.None,
        Font = UITheme.FontRegular(8.5f)
    };

    private sealed class AnhChon(string duongDan, string ten, long size)
    {
        public string DuongDan { get; } = duongDan;
        public string Ten { get; } = ten;
        public long Size { get; } = size;
        public string TrangThai { get; set; } = "Chờ gửi";
        public long? MaHinhAnh { get; set; }
    }

    private sealed class LoaiTinBaoItem { public int Ma { get; set; } public string Ten { get; set; } = ""; }

    // ---- Khu vực trái: ảnh + AI ----
    private readonly PictureBox picHienTruong = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(30, 30, 30) };
    private readonly Label lblTenAnh = new() { Dock = DockStyle.Bottom, Height = 22, Text = "Chưa chọn ảnh hiện trường", ForeColor = UITheme.Muted, Font = UITheme.FontRegular(8.5f), TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button btnChonAnh = TaoNut("Chọn / Tải ảnh lên", 130, UITheme.Navy, Color.White);
    private readonly Button btnWebcam = TaoNut("Chụp từ Webcam", 120, Color.White, UITheme.Navy);
    private readonly Button btnXoaAnh = TaoNut("Xóa ảnh", 80, Color.White, UITheme.Red);
    private readonly Label lblAiTrangThai = new() { Dock = DockStyle.Top, Height = 38, Text = "Chưa phân tích: dịch vụ AI/OCR chưa được tích hợp vào ứng dụng.", ForeColor = UITheme.Muted, Font = UITheme.FontRegular(8.5f) };
    private readonly TextBox txtOcr = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BackColor = UITheme.Soft, Font = UITheme.FontRegular(9) };
    private readonly Button btnApDung = TaoNut("Áp dụng vào biểu mẫu", 150, Color.White, UITheme.Blue);

    // ---- Nhóm sự việc ----
    private readonly DateTimePicker dtpThoiDiem = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm:ss", Font = UITheme.FontRegular(9.5f) };
    private readonly ComboBox cboLoai = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly TextBox txtDiaDiem = new() { Dock = DockStyle.Fill, MaxLength = 500, Font = UITheme.FontRegular(9.5f), PlaceholderText = "Số XX Đường XXX, Phường XXX, Quận XXX, Thành phố XXX" };
    private readonly Button btnBanDo = TaoNut("Bản đồ GIS", 105, Color.White, UITheme.Navy);
    private readonly TextBox txtMoTa = new() { Dock = DockStyle.Fill, Multiline = true, MaxLength = MaxMoTa, ScrollBars = ScrollBars.Vertical, Font = UITheme.FontRegular(9.5f), PlaceholderText = "Nêu rõ mô tả chi tiết" };
    private readonly Label lblDem = new() { AutoSize = true, ForeColor = UITheme.Muted, Font = UITheme.FontRegular(8.5f), Anchor = AnchorStyles.Right };

    // ---- Nhóm người báo ----
    private readonly CheckBox chkAnDanh = new() { AutoSize = true, Text = "Gửi ẩn danh (Bảo vệ danh tính người tố giác / cung cấp tin báo)", Font = UITheme.FontRegular(9) };
    private readonly TextBox txtHoTen = new() { Font = UITheme.FontRegular(9.5f), MaxLength = 150 };
    private readonly TextBox txtSdt = new() { Font = UITheme.FontRegular(9.5f), MaxLength = 20 };
    private readonly TextBox txtCccd = new() { Font = UITheme.FontRegular(9.5f), MaxLength = 20 };
    private readonly ComboBox cboKenh = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly ComboBox cboMucDo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };

    // ---- Thanh dưới ----
    private readonly Label lblTrangThai = new() { AutoSize = true, Font = UITheme.FontRegular(8.5f), ForeColor = UITheme.DarkText };
    private readonly Button btnHuy = TaoNut("HỦY BỎ", 100, Color.White, UITheme.Red);
    private readonly Button btnIn = TaoNut("In Phiếu (F11)", 110, Color.White, UITheme.Navy);
    private readonly Button btnGui = TaoNut("GỬI TIN BÁO", 150, UITheme.Navy, Color.White);

    public FrmTiepNhanTinBao()
    {
        Text = "Hệ Thống Quản Lý Thi Thể & Xác Minh Danh Tính — [Form Tiếp Nhận Tin Báo Sự Việc]";
        ClientSize = new Size(1020, 720);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        NapDuLieuTam();
        GanSuKien();
    }

    // ================= DỰNG GIAO DIỆN =================
    private void BuildUI()
    {
        var tk = Session.TaiKhoan;

        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = UITheme.Navy };
        pnlHeader.Controls.Add(new Label { Text = "TIẾP NHẬN TIN BÁO SỰ VIỆC", Font = UITheme.FontBold(11), ForeColor = Color.White, AutoSize = true, Location = new Point(16, 8) });
        pnlHeader.Controls.Add(new Label { Text = $"Cán bộ: {tk?.HoTen}   |   Đơn vị: {Session.DonVi}", Font = UITheme.FontRegular(8), ForeColor = UITheme.Gold, AutoSize = true, Location = new Point(16, 30) });

        // Thanh dưới
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White, Padding = new Padding(12, 8, 12, 8) };
        pnlBottom.Paint += (s, e) => { using var pen = new Pen(UITheme.Line); e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0); };
        lblTrangThai.Text = $"● Trạng thái: Sẵn sàng ghi nhận tin báo   |   Máy trạm: {Environment.MachineName}";
        lblTrangThai.Location = new Point(12, 20);
        var flowNut = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 400, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        flowNut.Controls.AddRange(new Control[] { btnGui, btnIn, btnHuy });
        pnlBottom.Controls.Add(flowNut);
        pnlBottom.Controls.Add(lblTrangThai);

        // Thân form
        var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

        var pnlRight = new Panel { Dock = DockStyle.Fill };
        var gbSuViec = TaoGroup("Chi Tiết Thông Tin Sự Việc");
        gbSuViec.Controls.Add(DungNhomSuViec());
        var gbNguoiBao = TaoGroup("Thông Tin Người Báo Cáo");
        gbNguoiBao.Dock = DockStyle.Bottom;
        gbNguoiBao.Height = 235;
        gbNguoiBao.Controls.Add(DungNhomNguoiBao());
        pnlRight.Controls.Add(gbSuViec);                                              // Fill
        pnlRight.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 8 });     // khoảng cách
        pnlRight.Controls.Add(gbNguoiBao);                                            // Bottom

        var pnlLeft = DungKhuVucTrai();

        pnlBody.Controls.Add(pnlRight);
        pnlBody.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 10 });
        pnlBody.Controls.Add(pnlLeft);

        Controls.Add(pnlBody);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);
    }

    private Panel DungKhuVucTrai()
    {
        var pnl = new Panel { Dock = DockStyle.Left, Width = 400 };

        var gbAnh = TaoGroup("Hình Ảnh Hiện Trường & Phân Tích AI");
        gbAnh.Dock = DockStyle.Top;
        gbAnh.Height = 420;
        var flowAnh = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, WrapContents = false };
        flowAnh.Controls.AddRange(new Control[] { btnChonAnh, btnWebcam, btnXoaAnh });
        gbAnh.Controls.Add(picHienTruong);   // Fill
        gbAnh.Controls.Add(lvAnh);           // Bottom
        gbAnh.Controls.Add(lblTenAnh);       // Bottom
        gbAnh.Controls.Add(flowAnh);         // Bottom, sát đáy

        var gbAi = TaoGroup("Kết Quả Phân Tích AI Tức Thời");
        gbAi.Controls.Add(txtOcr);
        gbAi.Controls.Add(btnApDung);
        btnApDung.Dock = DockStyle.Bottom;
        btnApDung.Height = 32;
        gbAi.Controls.Add(new Label { Dock = DockStyle.Top, Height = 20, Text = "Ký tự OCR nhận dạng được:", Font = UITheme.FontBold(8.5f) });
        gbAi.Controls.Add(lblAiTrangThai);

        pnl.Controls.Add(gbAi);                                                        // Fill
        pnl.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        pnl.Controls.Add(gbAnh);                                                       // Top
        return pnl;
    }

    private Control DungNhomSuViec()
    {
        var t = TaoLuoi(2);
        // Hàng 0-1: thời điểm | loại hình
        t.Controls.Add(Nhan("Thời Điểm Tiếp Nhận / Xảy Ra:"), 0, 0);
        t.Controls.Add(Nhan("Loại Hình Sự Việc / Phân Loại Ban Đầu:"), 1, 0);
        t.Controls.Add(Truong(dtpThoiDiem), 0, 1);
        t.Controls.Add(Truong(cboLoai), 1, 1);
        // Hàng 2-3: địa điểm + nút bản đồ
        var lblDd = Nhan("Địa Điểm Hiện Trường Cụ Thể (Số nhà, Tuyến đường, Tọa độ):");
        t.Controls.Add(lblDd, 0, 2); t.SetColumnSpan(lblDd, 2);
        var pnlDd = new Panel { Dock = DockStyle.Fill, Height = 30, Margin = new Padding(0, 0, 0, 6) };
        btnBanDo.Dock = DockStyle.Right;
        pnlDd.Controls.Add(txtDiaDiem);
        pnlDd.Controls.Add(btnBanDo);
        t.Controls.Add(pnlDd, 0, 3); t.SetColumnSpan(pnlDd, 2);
        // Hàng 4-6: mô tả
        var lblMt = Nhan("Mô Tả Chi Tiết Tình Trạng Hiện Trường & Ghi Chú Tiếp Nhận:");
        t.Controls.Add(lblMt, 0, 4); t.SetColumnSpan(lblMt, 2);
        txtMoTa.Margin = new Padding(0, 0, 0, 4);
        t.Controls.Add(txtMoTa, 0, 5); t.SetColumnSpan(txtMoTa, 2);
        t.Controls.Add(lblDem, 0, 6); t.SetColumnSpan(lblDem, 2);

        t.RowStyles.Clear();
        for (int i = 0; i < 7; i++)
            t.RowStyles.Add(i == 5 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
        return t;
    }

    private Control DungNhomNguoiBao()
    {
        var t = TaoLuoi(6);
        t.Controls.Add(chkAnDanh, 0, 0); t.SetColumnSpan(chkAnDanh, 6);

        var l1 = Nhan("Họ Và Tên Người Báo:"); t.Controls.Add(l1, 0, 1); t.SetColumnSpan(l1, 3);
        var l2 = Nhan("Số Điện Thoại Liên Hệ:"); t.Controls.Add(l2, 3, 1); t.SetColumnSpan(l2, 3);
        Truong(txtHoTen); t.Controls.Add(txtHoTen, 0, 2); t.SetColumnSpan(txtHoTen, 3);
        Truong(txtSdt); t.Controls.Add(txtSdt, 3, 2); t.SetColumnSpan(txtSdt, 3);

        var l3 = Nhan("CCCD / Định Danh Điện Tử:"); t.Controls.Add(l3, 0, 3); t.SetColumnSpan(l3, 2);
        var l4 = Nhan("Kênh Tiếp Nhận:"); t.Controls.Add(l4, 2, 3); t.SetColumnSpan(l4, 2);
        var l5 = Nhan("Mức Độ Khẩn Cấp:"); t.Controls.Add(l5, 4, 3); t.SetColumnSpan(l5, 2);
        Truong(txtCccd); t.Controls.Add(txtCccd, 0, 4); t.SetColumnSpan(txtCccd, 2);
        Truong(cboKenh); t.Controls.Add(cboKenh, 2, 4); t.SetColumnSpan(cboKenh, 2);
        Truong(cboMucDo); t.Controls.Add(cboMucDo, 4, 4); t.SetColumnSpan(cboMucDo, 2);

        t.RowStyles.Clear();
        for (int i = 0; i < 6; i++) t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return t;
    }

    // ================= DỮ LIỆU TẠM + SỰ KIỆN =================
    private void NapDuLieuTam()
    {
        using (var db = Program.NewDb())
        {
            var ds = db.Database.SqlQuery<LoaiTinBaoItem>(
                $"SELECT MaLoaiTinBao AS Ma, TenLoaiTinBao AS Ten FROM LoaiTinBao WHERE TrangThai = 1").ToList();
            cboLoai.DisplayMember = nameof(LoaiTinBaoItem.Ten);
            cboLoai.ValueMember = nameof(LoaiTinBaoItem.Ma);
            cboLoai.DataSource = ds;
        }
        lvAnh.SmallImageList = _imgThumb;
        lvAnh.Columns.Add("Tệp", 190);
        lvAnh.Columns.Add("MB", 55);
        lvAnh.Columns.Add("Trạng thái", 120);

        // TẠM: danh sách kênh chưa có trong tài liệu/CSDL, chờ bạn chốt
        cboKenh.Items.AddRange(new object[] { "Người dân trực tiếp", "Tổng đài 113 chuyển tiếp", "Cán bộ tại hiện trường" });
        cboKenh.SelectedIndex = 0;

        // 4 mức tương ứng CHECK của cột MucDoUuTien (KhanCap, Cao, BinhThuong, Thap)
        cboMucDo.Items.AddRange(new object[] { "CẤP ĐỘ 1 - KHẨN CẤP", "CẤP ĐỘ 2 - CAO", "CẤP ĐỘ 3 - BÌNH THƯỜNG", "CẤP ĐỘ 4 - THẤP" });
        cboMucDo.SelectedIndex = 2;   // mặc định BinhThuong như DEFAULT của cột
        dtpThoiDiem.Value = DateTime.Now;
        CapNhatBoDem();
        btnXoaAnh.Enabled = false;

        // Chưa có chức năng thật -> vô hiệu hóa, không để nút "trang trí"
        foreach (var b in new[] { btnWebcam, btnBanDo, btnIn, btnApDung }) b.Enabled = false;
        _tip.SetToolTip(btnWebcam, "Chưa hỗ trợ chụp webcam");
        _tip.SetToolTip(btnBanDo, "Chưa tích hợp bản đồ GIS");
        _tip.SetToolTip(btnIn, "Chưa hỗ trợ in phiếu");
        _tip.SetToolTip(btnApDung, "Cần dịch vụ AI/OCR (chưa tích hợp)");
    }

    private void GanSuKien()
    {
        txtMoTa.TextChanged += (s, e) => CapNhatBoDem();
        chkAnDanh.CheckedChanged += (s, e) =>
        {
            bool an = chkAnDanh.Checked;
            if (an) { txtHoTen.Clear(); txtSdt.Clear(); txtCccd.Clear(); _err.SetError(txtSdt, ""); _err.SetError(txtCccd, ""); }
            txtHoTen.Enabled = txtSdt.Enabled = txtCccd.Enabled = !an;
        };
        btnChonAnh.Click += (s, e) => ChonAnh();
        btnXoaAnh.Click += (s, e) => XoaAnh();
        btnHuy.Click += (s, e) => HuyBo();
        btnGui.Click += async (s, e) => await GuiTinBaoAsync();
        lvAnh.SelectedIndexChanged += (s, e) => XemTruocAnhDangChon();
        foreach (Control c in new Control[] { picHienTruong, lvAnh }) BatKeoTha(c);
    }

    private void CapNhatBoDem() => lblDem.Text = $"Ký tự: {txtMoTa.TextLength} / {MaxMoTa}";

    private void ChonAnh()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Chọn ảnh hiện trường",
            Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = true,
            CheckFileExists = true
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) ThemAnh(dlg.FileNames);
    }

    private void BatKeoTha(Control c)
    {
        c.AllowDrop = true;
        c.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy : DragDropEffects.None;
        c.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files) ThemAnh(files);
        };
    }

    private void ThemAnh(IEnumerable<string> paths)
    {
        var loi = new List<string>();
        foreach (var p in paths)
        {
            var fi = new FileInfo(p);
            if (_dsAnh.Count >= ImageHelper.MaxAnhMoiTinBao) { loi.Add($"{fi.Name}: đã đủ {ImageHelper.MaxAnhMoiTinBao} ảnh."); continue; }
            if (_dsAnh.Any(a => string.Equals(a.DuongDan, fi.FullName, StringComparison.OrdinalIgnoreCase))) { loi.Add($"{fi.Name}: đã có trong danh sách."); continue; }
            if (!FileHelper.IsValidImageExtension(p)) { loi.Add($"{fi.Name}: định dạng không được hỗ trợ."); continue; }
            if (!FileHelper.IsValidFileSize(fi.Length)) { loi.Add($"{fi.Name}: phải lớn hơn 0 và không quá 10 MB."); continue; }
            if (!FileHelper.IsValidImageContent(p)) { loi.Add($"{fi.Name}: không phải ảnh hợp lệ hoặc bị hỏng."); continue; }
            _dsAnh.Add(new AnhChon(fi.FullName, fi.Name, fi.Length));
        }
        VeLaiDanhSachAnh();
        if (loi.Count > 0)
            MessageBox.Show(string.Join("\n", loi), "Một số ảnh không được thêm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static Bitmap TaoThumb(string path)
    {
        using var fs = File.OpenRead(path);
        using var goc = System.Drawing.Image.FromStream(fs);
        var thumb = new Bitmap(40, 40);
        using var g = Graphics.FromImage(thumb);
        g.Clear(Color.White);
        float tl = Math.Min(40f / goc.Width, 40f / goc.Height);
        int w = (int)(goc.Width * tl), h = (int)(goc.Height * tl);
        g.DrawImage(goc, (40 - w) / 2, (40 - h) / 2, w, h);
        return thumb;
    }

    private void VeLaiDanhSachAnh()
    {
        lvAnh.BeginUpdate();
        lvAnh.Items.Clear();
        _imgThumb.Images.Clear();
        for (int i = 0; i < _dsAnh.Count; i++)
        {
            var a = _dsAnh[i];
            using (var t = TaoThumb(a.DuongDan)) _imgThumb.Images.Add(t);
            var it = new ListViewItem(a.Ten, i) { Tag = a };
            it.SubItems.Add($"{a.Size / 1048576.0:F1}");
            it.SubItems.Add(a.TrangThai);
            lvAnh.Items.Add(it);
        }
        lvAnh.EndUpdate();

        lblTenAnh.Text = _dsAnh.Count == 0
            ? "Chưa chọn ảnh hiện trường"
            : $"{_dsAnh.Count}/{ImageHelper.MaxAnhMoiTinBao} ảnh • tổng {_dsAnh.Sum(x => x.Size) / 1048576.0:F1} MB";
        btnXoaAnh.Enabled = lvAnh.SelectedItems.Count > 0;
        if (_dsAnh.Count == 0) { picHienTruong.Image?.Dispose(); picHienTruong.Image = null; }
    }

    private void XemTruocAnhDangChon()
    {
        btnXoaAnh.Enabled = lvAnh.SelectedItems.Count > 0;
        if (lvAnh.SelectedItems.Count == 0) return;
        var a = (AnhChon)lvAnh.SelectedItems[0].Tag!;
        using var fs = File.OpenRead(a.DuongDan);
        using var goc = System.Drawing.Image.FromStream(fs);
        var bmp = new Bitmap(goc);
        picHienTruong.Image?.Dispose();
        picHienTruong.Image = bmp;
    }

    private void XoaAnh()
    {
        if (lvAnh.SelectedItems.Count == 0) return;
        var a = (AnhChon)lvAnh.SelectedItems[0].Tag!;
        if (a.MaHinhAnh != null)
        {
            MessageBox.Show("Ảnh này đã lưu vào hệ thống nên không xóa ở đây.", "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _dsAnh.Remove(a);
        VeLaiDanhSachAnh();
    }

    private void HuyBo()
    {
        bool coDuLieu = txtMoTa.TextLength > 0 || txtDiaDiem.TextLength > 0 || _dsAnh.Count > 0;
        if (coDuLieu && MessageBox.Show("Hủy tin báo đang nhập? Dữ liệu chưa lưu sẽ mất.", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private async Task GuiTinBaoAsync()
    {
        if (!PermissionUi.Check(this, AuthorizationPolicies.ResourceIncident, AuthorizationPolicies.ActionCreate)) return;
        if (!KiemTraDauVao()) return;

        btnGui.Enabled = false;
        try
        {
            using var db = Program.NewDb();

            if (_maTinBao is null)   // bấm gửi lại để thử lại ảnh lỗi thì không tạo tin báo lần nữa
            {
                var diaDiem = txtDiaDiem.Text.Trim();
                var tieuDe = $"{cboLoai.Text} - {diaDiem}";
                _maTinBao = await new TinBaoService(db).GuiTinBaoAsync(new GuiTinBaoRequest
                {
                    MaLoaiTinBao = (int)cboLoai.SelectedValue!,
                    TieuDe = tieuDe.Length > 200 ? tieuDe[..200] : tieuDe,
                    NoiDung = txtMoTa.Text.Trim(),
                    ThoiGianXayRa = dtpThoiDiem.Value,
                    DiaDiem = diaDiem,
                    HoTenNguoiBao = chkAnDanh.Checked || txtHoTen.Text.Trim().Length == 0 ? null : txtHoTen.Text.Trim(),
                    SoDienThoaiNguoiBao = chkAnDanh.Checked || txtSdt.Text.Trim().Length == 0 ? null : txtSdt.Text.Trim(),
                    MucDoUuTien = MucDoDb[cboMucDo.SelectedIndex]
                });
            }

            var imgSvc = new ImageService(db);
            int soLoi = 0;
            foreach (var a in _dsAnh.Where(x => x.MaHinhAnh is null).ToList())
            {
                a.TrangThai = "Đang tải lên...";
                VeLaiDanhSachAnh();
                try
                {
                    var dto = await imgSvc.TaiLenHinhAnhAsync(new UploadImageRequest
                    {
                        FilePath = a.DuongDan,
                        LoaiHinhAnh = ImageHelper.TypeIncidentItem,   // tạm: mọi ảnh hiện trường
                        MaTinBao = _maTinBao
                    });
                    a.MaHinhAnh = dto.MaHinhAnh;
                    a.TrangThai = "Đã tải lên";
                }
                catch (Exception ex) when (ex is not (UnauthenticatedException or ForbiddenException))
                {
                    a.TrangThai = "Lỗi: " + ex.Message;
                    soLoi++;
                }
            }
            VeLaiDanhSachAnh();

            if (soLoi == 0)
            {
                MessageBox.Show($"Đã gửi tin báo (mã nội bộ {_maTinBao}).", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show($"Tin báo đã được tạo nhưng còn {soLoi} ảnh chưa tải được.\nBấm GỬI TIN BÁO để thử lại các ảnh lỗi.",
                    "Chưa hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally { btnGui.Enabled = true; }
    }

    // ================= KIỂM TRA DỮ LIỆU =================
    private bool KiemTraDauVao()
    {
        _err.Clear();
        Control? dauTien = null;
        void Loi(Control c, string msg) { _err.SetError(c, msg); dauTien ??= c; }

        if (dtpThoiDiem.Value > DateTime.Now.AddMinutes(5)) Loi(dtpThoiDiem, "Thời điểm không được ở tương lai.");
        if (cboLoai.SelectedIndex < 0) Loi(cboLoai, "Chọn loại hình sự việc.");

        var diaDiem = txtDiaDiem.Text.Trim();
        if (diaDiem.Length == 0) Loi(txtDiaDiem, "Nhập địa điểm hiện trường.");

        var moTa = txtMoTa.Text.Trim();
        if (moTa.Length == 0) Loi(txtMoTa, "Nhập mô tả sự việc.");

        if (cboMucDo.SelectedIndex < 0) Loi(cboMucDo, "Chọn mức độ khẩn cấp.");

        if (!chkAnDanh.Checked)
        {
            var sdt = txtSdt.Text.Trim();
            if (sdt.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^\+?\d{9,12}$"))
                Loi(txtSdt, "Số điện thoại gồm 9–12 chữ số.");        // TẠM: theo quy tắc của bảng tài khoản

            var cccd = txtCccd.Text.Trim();
            if (cccd.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(cccd, @"^\d{12}$"))
                Loi(txtCccd, "CCCD gồm đúng 12 chữ số.");
        }

        dauTien?.Focus();
        return dauTien == null;
    }

    // ================= HÀM DỰNG CONTROL DÙNG CHUNG =================
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        picHienTruong.Image?.Dispose();
        _tip.Dispose();
        _err.Dispose();
        base.OnFormClosed(e);
        _imgThumb.Dispose();
    }

    private static Button TaoNut(string text, int w, Color back, Color fore)
    {
        var b = new Button { Text = text, Size = new Size(w, 32), FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore, Font = UITheme.FontBold(8.5f), Cursor = Cursors.Hand, Margin = new Padding(4, 0, 0, 0) };
        b.FlatAppearance.BorderColor = back == Color.White ? UITheme.Line : back;
        return b;
    }

    private static GroupBox TaoGroup(string title) => new()
    {
        Text = title,
        Dock = DockStyle.Fill,
        Padding = new Padding(10, 6, 10, 8),
        Font = UITheme.FontBold(9),
        ForeColor = UITheme.Navy,
        BackColor = Color.White
    };

    private static TableLayoutPanel TaoLuoi(int cot)
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = cot, BackColor = Color.White, ForeColor = UITheme.DarkText };
        for (int i = 0; i < cot; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cot));
        return t;
    }

    private static Label Nhan(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = UITheme.FontBold(8.5f),
        ForeColor = UITheme.DarkText,
        Margin = new Padding(0, 4, 0, 2)
    };

    private static T Truong<T>(T c) where T : Control
    {
        c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        c.Margin = new Padding(0, 0, 8, 6);
        return c;
    }
}