using System.Drawing.Drawing2D;
using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Services;

namespace DeathManagement.API.Forms;

public class FrmXemChiTietAnh : Form
{
    private readonly ImageDto _imageDto;
    private readonly PictureBox picPreview = new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = ColorTranslator.FromHtml("#1a1a1a")
    };

    private System.Drawing.Image? _currentBitmap;
    private float _zoomFactor = 1.0f;

    public FrmXemChiTietAnh(ImageDto dto, System.Drawing.Image? loadedImage = null)
    {
        _imageDto = dto;
        Text = $"Chi tiết hình ảnh: {_imageDto.TenGoc} - [{_imageDto.LoaiHinhAnh}]";
        ClientSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        BuildUI();
        LoadImageData(loadedImage);
    }

    private void BuildUI()
    {
        // 1. Top Header Banner
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UITheme.Navy };
        var lblTitle = new Label
        {
            Text = $"🖼️ CHI TIẾT TỆP ĐA PHƯƠNG TIỆN: {_imageDto.TenGoc}",
            Font = UITheme.FontBold(10.5f),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 10)
        };
        var lblSub = new Label
        {
            Text = $"Loại ảnh: {_imageDto.LoaiHinhAnh} | Ngày tải: {_imageDto.ThoiGianTaiLen:dd/MM/yyyy HH:mm} | Người tải: {_imageDto.NguoiTaiLen}",
            Font = UITheme.FontRegular(8),
            ForeColor = UITheme.Gold,
            AutoSize = true,
            Location = new Point(20, 32)
        };

        if (_imageDto.LaNhayCam)
        {
            var badgeNhayCam = new Label
            {
                Text = "⚠️ ẢNH PHÁP Y NHẠY CẢM",
                BackColor = UITheme.Red,
                ForeColor = Color.White,
                Font = UITheme.FontBold(8),
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true,
                Location = new Point(880, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlHeader.Controls.Add(badgeNhayCam);
        }

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // 2. Toolbar thao tác GDI+ (Zoom, Xoay, Lật ảnh)
        var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.White };
        pnlToolbar.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1);
        };

        var btnZoomIn = CreateToolButton("🔍+ Phóng to", () => ZoomImage(1.2f));
        btnZoomIn.Location = new Point(15, 6);

        var btnZoomOut = CreateToolButton("🔍- Thu nhỏ", () => ZoomImage(0.8f));
        btnZoomOut.Location = new Point(130, 6);

        var btnRotateRight = CreateToolButton("↷ Xoay 90°", () => RotateImage(RotateFlipType.Rotate90FlipNone));
        btnRotateRight.Location = new Point(245, 6);

        var btnFlipX = CreateToolButton("⇄ Lật ngang", () => RotateImage(RotateFlipType.RotateNoneFlipX));
        btnFlipX.Location = new Point(355, 6);

        var btnReset = CreateToolButton("↺ Vừa khung", () => ResetZoom());
        btnReset.Location = new Point(465, 6);

        pnlToolbar.Controls.AddRange(new Control[] { btnZoomIn, btnZoomOut, btnRotateRight, btnFlipX, btnReset });

        // 3. Metadata Panel bên phải
        var pnlInfo = new Panel
        {
            Dock = DockStyle.Right,
            Width = 320,
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        pnlInfo.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, 0, 0, pnlInfo.Height);
        };

        var lblMetaTitle = new Label
        {
            Text = "THÔNG SỐ KỸ THUẬT & METADATA",
            Font = UITheme.FontBold(9.5f),
            ForeColor = UITheme.Navy,
            Location = new Point(16, 16),
            AutoSize = true
        };

        var flowMeta = new FlowLayoutPanel
        {
            Location = new Point(16, 45),
            Size = new Size(288, 580),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        void AddMeta(string label, string val, Color? color = null)
        {
            var p = new Panel { Size = new Size(280, 40), Margin = new Padding(0, 0, 0, 8) };
            var l1 = new Label { Text = label, Font = UITheme.FontBold(8), ForeColor = UITheme.Muted, AutoSize = true, Location = new Point(0, 0) };
            var l2 = new Label { Text = val, Font = UITheme.FontRegular(8.5f), ForeColor = color ?? UITheme.DarkText, AutoSize = true, Location = new Point(0, 18) };
            p.Controls.AddRange(new Control[] { l1, l2 });
            flowMeta.Controls.Add(p);
        }

        AddMeta("MÃ ĐỊNH DANH ẢNH (ID)", $"#{_imageDto.MaHinhAnh}");
        AddMeta("TÊN TỆP GỐC", _imageDto.TenGoc);
        AddMeta("TÊN LƯU TRỮ (SECURE GUID)", _imageDto.TenFile);
        AddMeta("PHÂN LOẠI", _imageDto.LoaiHinhAnh);
        AddMeta("ĐỊNH DẠNG MIME", _imageDto.DinhDang);
        AddMeta("DUNG LƯỢNG", $"{_imageDto.KichThuoc / 1024.0:F1} KB ({_imageDto.KichThuoc:N0} bytes)");
        AddMeta("ĐỘ PHÂN GIẢI", _imageDto.ChieuRong.HasValue ? $"{_imageDto.ChieuRong} × {_imageDto.ChieuCao} px" : "Chưa xác định");
        AddMeta("MÃ BĂM TOÀN VẸN (MD5)", _imageDto.HashMD5 ?? "Chưa băm", UITheme.Blue);
        AddMeta("ĐỘ NHẠY CẢM", _imageDto.LaNhayCam ? "⚠️ Nhạy cảm cao (Chỉ cán bộ xem)" : "Công khai / An toàn", _imageDto.LaNhayCam ? UITheme.Red : UITheme.Green);
        AddMeta("NGƯỜI TẢI LÊN", _imageDto.NguoiTaiLen);
        AddMeta("THỜI ĐIỂM TẢI LÊN", $"{_imageDto.ThoiGianTaiLen:dd/MM/yyyy HH:mm:ss}");
        AddMeta("GHI CHÚ / MÔ TẢ", string.IsNullOrWhiteSpace(_imageDto.MoTa) ? "(Không có ghi chú)" : _imageDto.MoTa);

        pnlInfo.Controls.AddRange(new Control[] { lblMetaTitle, flowMeta });

        // 4. Center Viewport (Chứa PictureBox)
        var pnlViewport = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#111111") };
        pnlViewport.Controls.Add(picPreview);

        picPreview.MouseWheel += (s, e) =>
        {
            if (e.Delta > 0) ZoomImage(1.15f);
            else if (e.Delta < 0) ZoomImage(0.85f);
        };

        // Add All
        Controls.Add(pnlViewport);
        Controls.Add(pnlInfo);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);
    }

    private Button CreateToolButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(105, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Soft,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(8),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = UITheme.Line;
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private void LoadImageData(System.Drawing.Image? loadedImage)
    {
        try
        {
            if (loadedImage != null)
            {
                _currentBitmap = new Bitmap(loadedImage);
                picPreview.Image = _currentBitmap;
                return;
            }

            var service = new ImageService(Program.NewDb());
            var physicalPath = service.LayDuongDanVatLy(_imageDto.DuongDan);

            if (File.Exists(physicalPath))
            {
                using var stream = new MemoryStream(File.ReadAllBytes(physicalPath));
                using var temp = System.Drawing.Image.FromStream(stream);
                _currentBitmap = new Bitmap(temp);
                picPreview.Image = _currentBitmap;
            }
            else
            {
                // Render ảnh giữ chỗ tượng trưng bằng GDI+
                _currentBitmap = TaoAnhPlaceholder(_imageDto.TenGoc, _imageDto.LaNhayCam);
                picPreview.Image = _currentBitmap;
            }
        }
        catch (Exception ex)
        {
            _currentBitmap = TaoAnhPlaceholder("Lỗi tải tệp: " + ex.Message, true);
            picPreview.Image = _currentBitmap;
        }
    }

    private void ZoomImage(float factor)
    {
        _zoomFactor *= factor;
        if (_zoomFactor < 0.2f) _zoomFactor = 0.2f;
        if (_zoomFactor > 5.0f) _zoomFactor = 5.0f;

        if (_zoomFactor == 1.0f)
        {
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
        }
        else
        {
            picPreview.SizeMode = PictureBoxSizeMode.CenterImage;
            if (_currentBitmap != null)
            {
                var newW = (int)(_currentBitmap.Width * _zoomFactor);
                var newH = (int)(_currentBitmap.Height * _zoomFactor);
                var bmp = new Bitmap(newW, newH);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(_currentBitmap, 0, 0, newW, newH);
                }
                picPreview.Image = bmp;
            }
        }
    }

    private void ResetZoom()
    {
        _zoomFactor = 1.0f;
        picPreview.SizeMode = PictureBoxSizeMode.Zoom;
        picPreview.Image = _currentBitmap;
    }

    private void RotateImage(RotateFlipType rotateType)
    {
        if (_currentBitmap == null) return;
        _currentBitmap.RotateFlip(rotateType);
        picPreview.Image = _currentBitmap;
        picPreview.Refresh();
    }

    private static Bitmap TaoAnhPlaceholder(string text, bool isSensitive)
    {
        var bmp = new Bitmap(800, 600);
        using var g = Graphics.FromImage(bmp);
        g.Clear(isSensitive ? ColorTranslator.FromHtml("#2c1b18") : ColorTranslator.FromHtml("#1f2937"));

        using var fontTitle = UITheme.FontHeading(14);
        using var fontSub = UITheme.FontRegular(10);
        using var brush = new SolidBrush(isSensitive ? ColorTranslator.FromHtml("#e74c3c") : Color.LightGray);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        var notice = isSensitive
            ? "⚠️ HÌNH ẢNH PHÁP Y ĐƯỢC BẢO VỆ\n\n" + text
            : "HÌNH ẢNH MINH HỌA HỆ THỐNG\n\n" + text;

        g.DrawString(notice, fontTitle, brush, new RectangleF(0, 0, 800, 600), format);
        return bmp;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentBitmap?.Dispose();
        }
        base.Dispose(disposing);
    }
}
