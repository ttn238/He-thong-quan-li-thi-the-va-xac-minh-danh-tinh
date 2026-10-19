using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Security;
using DeathManagement.API.Services;

namespace DeathManagement.API.Forms;

public class FrmTaiLenHinhAnh : Form
{
    private readonly TextBox txtDuongDanTep = new() { Font = UITheme.FontRegular(9.5f), ReadOnly = true, PlaceholderText = "Chọn tệp ảnh từ máy hoặc kéo thả vào đây..." };
    private readonly ComboBox cboLoaiHinhAnh = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly NumericUpDown numMaThiThe = new() { Font = UITheme.FontRegular(9.5f), Minimum = 0, Maximum = 1000000, Value = 0 };
    private readonly NumericUpDown numMaMatTich = new() { Font = UITheme.FontRegular(9.5f), Minimum = 0, Maximum = 1000000, Value = 0 };
    private readonly CheckBox chkNhayCam = new() { Text = "Ảnh nhạy cảm pháp y (Chỉ cán bộ & Admin có quyền xem - NFR1.2)", Font = UITheme.FontBold(8.5f), ForeColor = UITheme.Red, AutoSize = true };
    private readonly TextBox txtMoTa = new() { Font = UITheme.FontRegular(9.5f), Multiline = true, Height = 60, PlaceholderText = "Nhập ghi chú hoặc mô tả đặc điểm nhận dạng trong ảnh..." };

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

    public FrmTaiLenHinhAnh(long? defaultMaThiThe = null, long? defaultMaMatTich = null)
    {
        Text = "Tải hình ảnh & Tệp đính kèm mới vào hệ thống";
        ClientSize = new Size(680, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);
        AllowDrop = true; // Kéo thả file

        if (defaultMaThiThe.HasValue) numMaThiThe.Value = defaultMaThiThe.Value;
        if (defaultMaMatTich.HasValue) numMaMatTich.Value = defaultMaMatTich.Value;

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

        // Xử lý kéo thả (Drag and drop)
        DragEnter += (s, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        DragDrop += (s, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                NapTepDuocChon(files[0]);
            }
        };
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
            Text = "Hỗ trợ định dạng JPG, PNG, WEBP, BMP. Tự động mã hóa tên tệp và băm MD5 toàn vẹn.",
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
            Size = new Size(640, 520),
            BackColor = Color.White,
            Padding = new Padding(24)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

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

        chkNhayCam.Location = new Point(24, 220);

        var lblMoTa = CreateLabel("MÔ TẢ ĐẶC ĐIỂM HOẶC GHI CHÚ:", 24, 252);
        txtMoTa.Location = new Point(24, 274);
        txtMoTa.Size = new Size(580, 60);

        // Progress bar
        prgTienTrinh.Location = new Point(24, 350);
        prgTienTrinh.Size = new Size(580, 16);
        lblTrangThai.Location = new Point(24, 372);

        // Buttons
        btnTaiLen.Location = new Point(344, 410);
        btnHuy.Location = new Point(514, 410);

        pnlCard.Controls.AddRange(new Control[]
        {
            lblFile, txtDuongDanTep, btnChonFile,
            lblLoai, cboLoaiHinhAnh,
            lblThiThe, numMaThiThe,
            lblMatTich, numMaMatTich,
            chkNhayCam,
            lblMoTa, txtMoTa,
            picPreview,
            prgTienTrinh, lblTrangThai,
            btnTaiLen, btnHuy
        });

        Controls.Add(pnlCard);
        Controls.Add(pnlHeader);
    }

    private static Label CreateLabel(string text, int x, int y) => new()
    {
        Text = text,
        Font = UITheme.FontBold(8),
        ForeColor = UITheme.Navy,
        Location = new Point(x, y),
        AutoSize = true
    };

    private void ChonTepDialog()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Chọn tệp hình ảnh để tải lên",
            Filter = "Tệp hình ảnh (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp|Tất cả tệp (*.*)|*.*",
            Multiselect = false
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            NapTepDuocChon(ofd.FileName);
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
        var path = txtDuongDanTep.Text.Trim();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            MessageBox.Show("Vui lòng chọn một tệp hình ảnh hợp lệ.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var maThiThe = numMaThiThe.Value > 0 ? (long?)numMaThiThe.Value : null;
        var maMatTich = numMaMatTich.Value > 0 ? (long?)numMaMatTich.Value : null;

        if (maThiThe == null && maMatTich == null)
        {
            var hoi = MessageBox.Show(
                "Ảnh này hiện chưa liên kết với Hồ sơ thi thể hoặc Người mất tích cụ thể nào.\nBạn có chắc muốn lưu ảnh độc lập?",
                "Xác nhận liên kết",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (hoi != DialogResult.Yes) return;
        }

        btnTaiLen.Enabled = false;
        btnChonFile.Enabled = false;
        btnHuy.Enabled = false;
        prgTienTrinh.Visible = true;
        lblTrangThai.Visible = true;
        prgTienTrinh.Value = 0;
        lblTrangThai.Text = "Đang đọc và sao chép tệp...";

        try
        {
            var req = new UploadImageRequest
            {
                FilePath = path,
                LoaiHinhAnh = cboLoaiHinhAnh.SelectedItem?.ToString() ?? "ThiThe_KhamNghiem",
                MoTa = txtMoTa.Text.Trim(),
                LaNhayCam = chkNhayCam.Checked,
                MaHoSoThiThe = maThiThe,
                MaHoSoMatTich = maMatTich,
                MaTaiKhoanTaiLen = 0   // ImageService tự gán từ JWT của người đăng nhập
            };

            var progress = new Progress<int>(pct =>
            {
                prgTienTrinh.Value = Math.Clamp(pct, 0, 100);
                lblTrangThai.Text = $"Đang tải lên và tính mã băm MD5: {pct}%";
            });

            using var db = Program.NewDb();
            var service = new ImageService(db);
            ResultImage = await service.TaiLenHinhAnhAsync(req, progress);

            lblTrangThai.Text = "Tải lên thành công 100%!";
            MessageBox.Show(
                $"Đã lưu thành công tệp: {ResultImage.TenGoc}\nMã ảnh: #{ResultImage.MaHinhAnh}\nMã băm MD5: {ResultImage.HashMD5}",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi tải ảnh: " + ex.Message, "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTaiLen.Enabled = true;
            btnChonFile.Enabled = true;
            btnHuy.Enabled = true;
        }
    }
}
