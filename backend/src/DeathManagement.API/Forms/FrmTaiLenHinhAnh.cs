using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Helpers;
using DeathManagement.API.Security;
using DeathManagement.API.Services;

namespace DeathManagement.API.Forms;

public class FrmTaiLenHinhAnh : Form
{
    private readonly TextBox txtDuongDanTep = new() { Font = UITheme.FontRegular(9.5f), ReadOnly = true, PlaceholderText = "Chọn tệp ảnh từ máy hoặc kéo thả vào đây..." };
    private readonly ComboBox cboLoaiHinhAnh = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly NumericUpDown numMaThiThe = new() { Font = UITheme.FontRegular(9.5f), Minimum = 0, Maximum = 1000000, Value = 0 };
    private readonly NumericUpDown numMaMatTich = new() { Font = UITheme.FontRegular(9.5f), Minimum = 0, Maximum = 1000000, Value = 0 };
    private readonly NumericUpDown numMaTinBao = new() { Font = UITheme.FontRegular(9.5f), Minimum = 0, Maximum = 1000000, Value = 0 };
    private readonly NumericUpDown numMaXacMinh = new() { Font = UITheme.FontRegular(9.5f), Minimum = 0, Maximum = 1000000, Value = 0 };
    private readonly CheckBox chkNhayCam = new() { Text = "Ảnh nhạy cảm pháp y (Chỉ cán bộ & Admin có quyền xem - NFR1.2)", Font = UITheme.FontBold(8.5f), ForeColor = UITheme.Red, AutoSize = true };
    private readonly TextBox txtMoTa = new() { Font = UITheme.FontRegular(9.5f), Multiline = true, Height = 60, PlaceholderText = "Nhập ghi chú hoặc mô tả đặc điểm nhận dạng trong ảnh..." };

    // Giới hạn số ảnh được chọn trong một lần thao tác.
    private const int MaxSoAnh = 5;

    // Danh sách các ảnh được chọn để tải lên.
    private readonly List<string> _danhSachDuongDanAnh = new();

    private readonly PictureBox picPreview = new()
    {
        Size = new Size(160, 160),
        SizeMode = PictureBoxSizeMode.Zoom,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = ColorTranslator.FromHtml("#f8f9fa")
    };

    private readonly ProgressBar prgTienTrinh = new() { Height = 20, Visible = false };
    private readonly Label lblTrangThai = new() { Text = "", Font = UITheme.FontRegular(8.5f), ForeColor = UITheme.Navy, AutoSize = true, Visible = false };

    private readonly Button btnChonFile = new()
    {
        Text = "DUYỆT FILE...",
        Size = new Size(110, 32),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Blue,
        ForeColor = Color.White,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };

    private readonly Button btnTaiLen = new()
    {
        Text = "BẮT ĐẦU TẢI LÊN",
        Size = new Size(160, 38),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Navy,
        ForeColor = Color.White,
        Font = UITheme.FontBold(9),
        Cursor = Cursors.Hand
    };

    private readonly Button btnHuy = new()
    {
        Text = "ĐÓNG",
        Size = new Size(100, 38),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = UITheme.Navy,
        Font = UITheme.FontBold(9),
        Cursor = Cursors.Hand
    };

    public ImageDto? ResultImage { get; private set; }

    public FrmTaiLenHinhAnh(long? defaultMaThiThe = null, long? defaultMaMatTich = null,
                            int? defaultMaTinBao = null, long? defaultMaXacMinh = null)
    {
        Text = "Tải hình ảnh & Tệp đính kèm mới vào hệ thống";
        ClientSize = new Size(680, 660);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);
        AllowDrop = true; // Kéo thả file

        if (defaultMaThiThe.HasValue) numMaThiThe.Value = defaultMaThiThe.Value;
        if (defaultMaMatTich.HasValue) numMaMatTich.Value = defaultMaMatTich.Value;
        if (defaultMaTinBao.HasValue) numMaTinBao.Value = defaultMaTinBao.Value;
        if (defaultMaXacMinh.HasValue) numMaXacMinh.Value = defaultMaXacMinh.Value;

        InitControls();
        BuildUI();
    }

    private void InitControls()
    {
        btnChonFile.FlatAppearance.BorderSize = 0;
        btnTaiLen.FlatAppearance.BorderSize = 0;
        btnHuy.FlatAppearance.BorderColor = UITheme.Line;

        cboLoaiHinhAnh.Items.AddRange(new object[]
        {
            "ThiThe_KhamNghiem",
            "ThiThe_KhuonMat",
            "ThiThe_ToanThan",
            "MatTich_ChanDung",
            "MatTich_DacDiem",
            "HienTruong_Xe",
            "HienTruong_VatDung",
            "MinhChung_ADN"
        });
        cboLoaiHinhAnh.SelectedIndex = 0;

        cboLoaiHinhAnh.SelectedIndexChanged += (s, e) =>
        {
            var loai = cboLoaiHinhAnh.SelectedItem?.ToString() ?? "";
            if (loai.StartsWith("ThiThe_"))
            {
                chkNhayCam.Checked = true;
            }
        };

        btnChonFile.Click += (s, e) => ChonTepDialog();
        btnHuy.Click += (s, e) => Close();
        btnTaiLen.Click += async (s, e) => await ThucHienTaiLenAsync();

        BatKeoTha(this);
    }

    private void BuildUI()
    {
        // 1. Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.Navy };
        var lblTitle = new Label
        {
            Text = "TẢI HÌNH ẢNH & ĐA PHƯƠNG TIỆN MỚI",
            Font = UITheme.FontBold(11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblSub = new Label
        {
            Text = "Hỗ trợ JPG, JPEG, PNG, BMP | Tối đa 10 MB/ảnh | Tối đa 5 ảnh/lần",
            Font = UITheme.FontRegular(8),
            ForeColor = UITheme.Gold,
            AutoSize = true,
            Location = new Point(20, 34)
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // 2. Body Card
        var pnlCard = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(640, 560),
            BackColor = Color.White,
            Padding = new Padding(24)
        };

        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        BatKeoTha(pnlCard);
        BatKeoTha(picPreview);
        BatKeoTha(txtDuongDanTep);

        // Dòng chọn file
        var lblFile = CreateLabel("TỆP NGUỒN CẦN TẢI LÊN:", 24, 20);
        txtDuongDanTep.Location = new Point(24, 42);
        txtDuongDanTep.Size = new Size(460, 32);
        btnChonFile.Location = new Point(494, 40);

        // Preview + phân loại
        picPreview.Location = new Point(444, 90);

        var lblLoai = CreateLabel("PHÂN LOẠI MỤC ĐÍCH ẢNH:", 24, 90);
        cboLoaiHinhAnh.Location = new Point(24, 112);
        cboLoaiHinhAnh.Size = new Size(400, 30);

        var lblThiThe = CreateLabel("MÃ HỒ SƠ TỬ THI (NẾU CÓ):", 24, 155);
        numMaThiThe.Location = new Point(24, 177);
        numMaThiThe.Size = new Size(190, 30);

        var lblMatTich = CreateLabel("MÃ HỒ SƠ MẤT TÍCH (NẾU CÓ):", 234, 155);
        numMaMatTich.Location = new Point(234, 177);
        numMaMatTich.Size = new Size(190, 30);

        var lblTinBao = CreateLabel("MÃ TIN BÁO (NẾU CÓ):", 24, 220);
        numMaTinBao.Location = new Point(24, 242);
        numMaTinBao.Size = new Size(190, 30);

        var lblXacMinh = CreateLabel("MÃ XÁC MINH (NẾU CÓ):", 234, 220);
        numMaXacMinh.Location = new Point(234, 242);
        numMaXacMinh.Size = new Size(190, 30);

        chkNhayCam.Location = new Point(24, 282);

        var lblMoTa = CreateLabel("MÔ TẢ ĐẶC ĐIỂM HOẶC GHI CHÚ:", 24, 312);
        txtMoTa.Location = new Point(24, 334);
        txtMoTa.Size = new Size(580, 60);

        // Progress bar
        prgTienTrinh.Location = new Point(24, 410);
        prgTienTrinh.Size = new Size(580, 16);
        lblTrangThai.Location = new Point(24, 432);

        // Buttons
        btnTaiLen.Location = new Point(344, 470);
        btnHuy.Location = new Point(514, 470);

        pnlCard.Controls.AddRange(new Control[]
        {
            lblFile, txtDuongDanTep, btnChonFile,
            lblLoai, cboLoaiHinhAnh,
            lblThiThe, numMaThiThe,
            lblMatTich, numMaMatTich,
            lblTinBao, numMaTinBao,
            lblXacMinh, numMaXacMinh,
            chkNhayCam,
            lblMoTa, txtMoTa,
            picPreview,
            prgTienTrinh, lblTrangThai,
            btnTaiLen, btnHuy
        });

        Controls.Add(pnlCard);
        Controls.Add(pnlHeader);
    }

    // Bật kéo-thả cho một control (gọi cho form, panel nền, ô xem trước...)
    private void BatKeoTha(Control c)
    {
        c.AllowDrop = true;
        c.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy : DragDropEffects.None;
        c.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
            if (files.Length > MaxSoAnh)
            {
                MessageBox.Show($"Chỉ được kéo-thả tối đa {MaxSoAnh} ảnh mỗi lần.", "Vượt quá số lượng cho phép",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            XuLyDanhSachAnhKeoTha(files);
        };
    }

    private static Label CreateLabel(string text, int x, int y) => new()
    {
        Text = text,
        Font = UITheme.FontBold(8),
        ForeColor = UITheme.Navy,
        Location = new Point(x, y),
        AutoSize = true
    };

    // Bật kéo-thả file ảnh cho một control
    private void BatKeoTha(Control c)
    {
        c.AllowDrop = true;
        c.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy : DragDropEffects.None;
        c.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
            if (files.Length > MaxSoAnh)
            {
                MessageBox.Show($"Chỉ được kéo-thả tối đa {MaxSoAnh} ảnh mỗi lần.", "Vượt quá số lượng cho phép",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            XuLyDanhSachAnhKeoTha(files);
        };
    }

    private void ChonTepDialog()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Chọn ảnh cần tải lên",
            Filter = "Ảnh được hỗ trợ (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = true,
            CheckFileExists = true
        };

        if (ofd.ShowDialog() != DialogResult.OK)
            return;

        if (ofd.FileNames.Length > MaxSoAnh)
        {
            MessageBox.Show(
                $"Bạn chỉ được chọn tối đa {MaxSoAnh} ảnh mỗi lần.",
                "Vượt quá số lượng cho phép",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Kiểm tra toàn bộ danh sách trước khi chấp nhận.
        foreach (string path in ofd.FileNames)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(
                    $"Không tìm thấy tệp:\n{path}",
                    "Lỗi tệp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var info = new FileInfo(path);

            if (!FileHelper.IsValidImageExtension(path))
            {
                MessageBox.Show(
                    $"Tệp '{info.Name}' không phải định dạng ảnh được hỗ trợ.",
                    "Định dạng không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!FileHelper.IsValidFileSize(info.Length))
            {
                MessageBox.Show(
                    $"Ảnh '{info.Name}' phải lớn hơn 0 byte và không vượt quá 10 MB.",
                    "Dung lượng không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!FileHelper.IsValidImageContent(path))
            {
                MessageBox.Show(
                    $"Tệp '{info.Name}' không phải ảnh hợp lệ hoặc đã bị hỏng.",
                    "Ảnh không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }

        if (!ThemVaoDanhSach(ofd.FileNames)) return;

        // Giữ tương thích với TextBox và PictureBox hiện có.
        txtDuongDanTep.Text = _danhSachDuongDanAnh.Count == 1
            ? _danhSachDuongDanAnh[0]
            : $"Đã chọn {_danhSachDuongDanAnh.Count}/{MaxSoAnh} ảnh";

        try
        {
            using var stream = File.OpenRead(_danhSachDuongDanAnh[0]);
            using var temp = System.Drawing.Image.FromStream(stream);

            var anhCu = picPreview.Image;
            picPreview.Image = new Bitmap(temp);
            anhCu?.Dispose();

            lblTrangThai.Visible = true;
            lblTrangThai.Text =
                $"Đã chọn {_danhSachDuongDanAnh.Count}/{MaxSoAnh} ảnh hợp lệ.";
        }
        catch (Exception ex)
        {
            _danhSachDuongDanAnh.Clear();
            txtDuongDanTep.Clear();

            MessageBox.Show(
                "Không thể xem trước ảnh: " + ex.Message,
                "Lỗi ảnh",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
    private void NapTepDuocChon(string path)
    {
        txtDuongDanTep.Text = path;
        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            using var temp = System.Drawing.Image.FromStream(stream);
            picPreview.Image = new Bitmap(temp);
        }
        catch
        {
            picPreview.Image = null;
        }
    }


    private async Task ThucHienTaiLenAsync()
    {
        if (_danhSachDuongDanAnh.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng chọn hoặc kéo-thả ít nhất một ảnh.",
                "Chưa chọn ảnh",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_danhSachDuongDanAnh.Count > MaxSoAnh)
        {
            MessageBox.Show(
                $"Chỉ được tải tối đa {MaxSoAnh} ảnh mỗi lần.",
                "Vượt quá số lượng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Kiểm tra lại toàn bộ ảnh ngay trước khi lưu.
        foreach (string path in _danhSachDuongDanAnh)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(
                    $"Tệp không còn tồn tại:\n{path}",
                    "Không tìm thấy tệp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var info = new FileInfo(path);

            if (!FileHelper.IsValidImageExtension(path)
                || !FileHelper.IsValidFileSize(info.Length)
                || !FileHelper.IsValidImageContent(path))
            {
                MessageBox.Show(
                    $"Ảnh '{info.Name}' không hợp lệ.\n" +
                    "Kiểm tra định dạng, nội dung và dung lượng tối đa 10 MB.",
                    "Ảnh không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }

        var maThiThe = numMaThiThe.Value > 0
            ? (long?)numMaThiThe.Value
            : null;

        var maMatTich = numMaMatTich.Value > 0
            ? (long?)numMaMatTich.Value
            : null;

        var maTinBao = numMaTinBao.Value > 0 ? (int?)numMaTinBao.Value : null;
        var maXacMinh = numMaXacMinh.Value > 0 ? (long?)numMaXacMinh.Value : null;

        if (maThiThe == null && maMatTich == null && maTinBao == null && maXacMinh == null)
        {
            MessageBox.Show("Phải nhập ít nhất một mã hồ sơ: tin báo, thi thể, mất tích hoặc xác minh.",
                "Thiếu liên kết hồ sơ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnTaiLen.Enabled = false;
        btnChonFile.Enabled = false;
        btnHuy.Enabled = false;

        prgTienTrinh.Visible = true;
        lblTrangThai.Visible = true;
        prgTienTrinh.Minimum = 0;
        prgTienTrinh.Maximum = 100;
        prgTienTrinh.Value = 0;

        int tongSoAnh = _danhSachDuongDanAnh.Count;
        int soAnhThanhCong = 0;
        var danhSachLoi = new List<string>();
        var ketQua = new List<ImageDto>();

        try
        {
            using var db = Program.NewDb();
            var service = new ImageService(db);

            for (int i = 0; i < tongSoAnh; i++)
            {
                string path = _danhSachDuongDanAnh[i];
                string tenAnh = Path.GetFileName(path);

                lblTrangThai.Text =
                    $"Đang lưu ảnh {i + 1}/{tongSoAnh}: {tenAnh}";

                try
                {
                    // Kiểm tra lại trước từng lần lưu để phát hiện
                    // tệp đã bị thay đổi sau khi người dùng chọn.
                    var info = new FileInfo(path);

                    if (!FileHelper.IsValidImageExtension(path)
                        || !FileHelper.IsValidFileSize(info.Length)
                        || !FileHelper.IsValidImageContent(path))
                    {
                        throw new InvalidOperationException(
                            "Tệp đã thay đổi hoặc không còn hợp lệ.");
                    }

                    var req = new UploadImageRequest
                    {
                        FilePath = path,
                        MaTinBao = maTinBao,
                        MaXacMinh = maXacMinh,
                        LoaiHinhAnh =
                            cboLoaiHinhAnh.SelectedItem?.ToString()
                            ?? "ThiThe_KhamNghiem",
                        MoTa = txtMoTa.Text.Trim(),
                        LaNhayCam = chkNhayCam.Checked,
                        MaHoSoThiThe = maThiThe,
                        MaHoSoMatTich = maMatTich,
                        MaTaiKhoanTaiLen = 0
                    };

                    int phanTramAnhHienTai = 0;

                    var progress = new Progress<int>(pct =>
                    {
                        phanTramAnhHienTai = Math.Clamp(pct, 0, 100);

                        int tongPhanTram =
                            (i * 100 + phanTramAnhHienTai) / tongSoAnh;

                        prgTienTrinh.Value =
                            Math.Clamp(tongPhanTram, 0, 100);

                        lblTrangThai.Text =
                            $"Ảnh {i + 1}/{tongSoAnh}: " +
                            $"{phanTramAnhHienTai}%";
                    });

                    ImageDto ketQuaAnh =
                        await service.TaiLenHinhAnhAsync(req, progress);

                    ketQua.Add(ketQuaAnh);
                    soAnhThanhCong++;

                    prgTienTrinh.Value =
                        Math.Clamp(soAnhThanhCong * 100 / tongSoAnh, 0, 100);
                }
                catch (Exception ex)
                {
                    danhSachLoi.Add($"{tenAnh}: {ex.Message}");
                }
            }

            ResultImage = ketQua.LastOrDefault();

            if (soAnhThanhCong == tongSoAnh)
            {
                lblTrangThai.Text =
                    $"Hoàn tất: {soAnhThanhCong}/{tongSoAnh} ảnh.";

                MessageBox.Show(
                    $"Đã tải thành công {soAnhThanhCong} ảnh.",
                    "Upload thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            lblTrangThai.Text =
                $"Đã lưu {soAnhThanhCong}/{tongSoAnh} ảnh.";

            string thongBao =
                $"Đã lưu thành công {soAnhThanhCong}/{tongSoAnh} ảnh.";

            if (danhSachLoi.Count > 0)
            {
                thongBao += "\n\nCác ảnh không tải được:\n"
                    + string.Join("\n", danhSachLoi);
            }

            MessageBox.Show(
                thongBao,
                soAnhThanhCong > 0 ? "Upload chưa hoàn tất" : "Upload thất bại",
                MessageBoxButtons.OK,
                soAnhThanhCong > 0
                    ? MessageBoxIcon.Warning
                    : MessageBoxIcon.Error);

            // Chỉ đóng form khi tất cả ảnh đã được lưu.
            if (soAnhThanhCong == tongSoAnh)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể thực hiện upload.\n" +
                "Hãy kiểm tra kết nối SQL Server, quyền truy cập và thử lại.\n\n" +
                ex.Message,
                "Lỗi hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnTaiLen.Enabled = true;
            btnChonFile.Enabled = true;
            btnHuy.Enabled = true;
        }
    }

    private void XuLyDanhSachAnhKeoTha(string[] files)
    {
        if (files.Length == 0) return;

        foreach (string path in files)
        {
            if (!File.Exists(path)
                || !FileHelper.IsValidImageExtension(path)
                || !FileHelper.IsValidFileSize(new FileInfo(path).Length)
                || !FileHelper.IsValidImageContent(path))
            {
                MessageBox.Show(
                    $"Tệp '{Path.GetFileName(path)}' không hợp lệ.\n" +
                    "Chỉ chấp nhận JPG, JPEG, PNG hoặc BMP, tối đa 10 MB mỗi ảnh.",
                    "Không thể thêm ảnh",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }

        if (!ThemVaoDanhSach(files)) return;

        txtDuongDanTep.Text = _danhSachDuongDanAnh.Count == 1
            ? _danhSachDuongDanAnh[0]
            : $"Đã chọn {_danhSachDuongDanAnh.Count}/{MaxSoAnh} ảnh";

        try
        {
            using var stream = File.OpenRead(_danhSachDuongDanAnh[0]);
            using var temp = System.Drawing.Image.FromStream(stream);

            var anhCu = picPreview.Image;
            picPreview.Image = new Bitmap(temp);
            anhCu?.Dispose();

            lblTrangThai.Visible = true;
            lblTrangThai.Text = $"Đã chọn {_danhSachDuongDanAnh.Count}/{MaxSoAnh} ảnh hợp lệ.";
        }
        catch (Exception ex)
        {
            _danhSachDuongDanAnh.Clear();
            txtDuongDanTep.Clear();

            MessageBox.Show(
                "Không thể hiển thị ảnh xem trước: " + ex.Message,
                "Lỗi ảnh",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private bool ThemVaoDanhSach(IEnumerable<string> files)
    {
        var moi = _danhSachDuongDanAnh.ToList();
        foreach (var p in files)
            if (!moi.Contains(p, StringComparer.OrdinalIgnoreCase)) moi.Add(p);
        if (moi.Count > MaxSoAnh)
        {
            MessageBox.Show($"Danh sách chỉ được tối đa {MaxSoAnh} ảnh.", "Vượt quá số lượng cho phép",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        _danhSachDuongDanAnh.Clear();
        _danhSachDuongDanAnh.AddRange(moi);
        return true;
    }
}