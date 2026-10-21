using System.Drawing.Drawing2D;
using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Security;
using DeathManagement.API.Services;

namespace DeathManagement.API.Forms;

public class FrmQuanLyHinhAnh : Form
{
    private readonly TextBox txtTimKiem = new() { Font = UITheme.FontRegular(10), PlaceholderText = "Tìm theo tên file, mô tả, người tải lên..." };
    private readonly ComboBox cboLoaiHinhAnh = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly ComboBox cboLocDoiTuong = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontRegular(9.5f) };
    private readonly CheckBox chkChiNhayCam = new() { Text = "Chỉ ảnh nhạy cảm pháp y", Font = UITheme.FontBold(8.5f), ForeColor = UITheme.Red, AutoSize = true };

    private readonly Button btnCheDoXem = new()
    {
        Text = "📋 XEM BẢNG DỮ LIỆU",
        Size = new Size(160, 32),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = UITheme.Navy,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };

    private readonly Button btnTaiAnhMoi = new()
    {
        Text = "+ TẢI ẢNH MỚI LÊN",
        Size = new Size(150, 36),
        FlatStyle = FlatStyle.Flat,
        BackColor = UITheme.Blue,
        ForeColor = Color.White,
        Font = UITheme.FontBold(8.5f),
        Cursor = Cursors.Hand
    };

    private readonly FlowLayoutPanel flowGallery = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        Padding = new Padding(16),
        BackColor = UITheme.Bg
    };

    private readonly DataGridView dgvList = new()
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
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        Visible = false
    };

    private readonly Label lblTongSo = new() { AutoSize = true, Font = UITheme.FontBold(9), ForeColor = UITheme.Navy };
    private readonly Label lblDungLuong = new() { AutoSize = true, Font = UITheme.FontRegular(8.5f), ForeColor = UITheme.Muted };

    private List<ImageDto> _currentImages = new();
    private bool _isGridView = false;
    private readonly bool _coQuyenXemNhayCam;

    public FrmQuanLyHinhAnh()
    {
        Text = "Quản lý Kho Hình ảnh & Tệp đính kèm - Phân hệ Pháp y & DVI";
        ClientSize = new Size(1200, 750);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UITheme.Bg;
        Font = UITheme.FontRegular(9.5f);

        // Quyền cán bộ / Admin / Nhà xác: mặc định cho phép trừ khi là vai trò NGUOI_DAN
        AccessControl.Require(AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionView);
        // Chỉ để ẩn/hiện bộ lọc; ImageService tự kiểm tra lại quyền xem ảnh nhạy cảm từ token
        _coQuyenXemNhayCam = AccessControl.CanPolicy(AuthorizationPolicies.RequireSensitiveDataViewer);

        InitControls();
        BuildUI();
        LoadData();
    }

    private void InitControls()
    {
        btnTaiAnhMoi.FlatAppearance.BorderSize = 0;
        btnCheDoXem.FlatAppearance.BorderColor = UITheme.Line;

        cboLoaiHinhAnh.Items.AddRange(new object[]
        {
            "Tất cả loại ảnh",
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

        cboLocDoiTuong.Items.AddRange(new object[]
        {
            "Tất cả hồ sơ",
            "Chỉ hồ sơ tử thi",
            "Chỉ hồ sơ người mất tích"
        });
        cboLocDoiTuong.SelectedIndex = 0;

        txtTimKiem.TextChanged += (s, e) => FilterAndRender();
        cboLoaiHinhAnh.SelectedIndexChanged += (s, e) => LoadData();
        cboLocDoiTuong.SelectedIndexChanged += (s, e) => LoadData();
        chkChiNhayCam.CheckedChanged += (s, e) => LoadData();

        btnCheDoXem.Click += (s, e) =>
        {
            _isGridView = !_isGridView;
            btnCheDoXem.Text = _isGridView ? "🖼️ XEM DẠNG THẺ (GALLERY)" : "📋 XEM BẢNG DỮ LIỆU";
            flowGallery.Visible = !_isGridView;
            dgvList.Visible = _isGridView;
            if (_isGridView) RenderGrid();
        };

        btnTaiAnhMoi.Click += (s, e) =>
        {
            if (!PermissionUi.Check(this, AuthorizationPolicies.ResourceFile, AuthorizationPolicies.ActionCreate)) return;   // <-- THÊM
            using var f = new FrmTaiLenHinhAnh();
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                LoadData();
            }
        };

        SetupDataGrid();
    }

    private void BuildUI()
    {
        // 1. Header Banner
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UITheme.Navy };
        var lblTitle = new Label
        {
            Text = "KHO LƯU TRỮ HÌNH ẢNH & TỆP ĐA PHƯƠNG TIỆN ",
            Font = UITheme.FontBold(11.5f),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 12)
        };
        var lblSub = new Label
        {
            Text = "Quản lý hình ảnh khám nghiệm tử thi, chân dung người mất tích, phương tiện hiện trường và chứng từ pháp y",
            Font = UITheme.FontRegular(8),
            ForeColor = UITheme.Gold,
            AutoSize = true,
            Location = new Point(20, 36)
        };

        btnTaiAnhMoi.Location = new Point(pnlHeader.Width - 170, 14);
        btnTaiAnhMoi.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, btnTaiAnhMoi });

        // 2. Toolbar & Filter Panel
        var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(20, 12, 20, 12) };
        pnlFilter.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, pnlFilter.Height - 1, pnlFilter.Width, pnlFilter.Height - 1);
        };

        txtTimKiem.Location = new Point(20, 12);
        txtTimKiem.Size = new Size(300, 32);

        cboLoaiHinhAnh.Location = new Point(330, 12);
        cboLoaiHinhAnh.Size = new Size(180, 32);

        cboLocDoiTuong.Location = new Point(520, 12);
        cboLocDoiTuong.Size = new Size(180, 32);

        chkChiNhayCam.Location = new Point(715, 17);
        chkChiNhayCam.Visible = _coQuyenXemNhayCam; // Chỉ hiện lọc nhạy cảm cho người có quyền

        btnCheDoXem.Location = new Point(pnlFilter.Width - 180, 12);
        btnCheDoXem.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        pnlFilter.Controls.AddRange(new Control[] { txtTimKiem, cboLoaiHinhAnh, cboLocDoiTuong, chkChiNhayCam, btnCheDoXem });

        // 3. Bottom Status Bar
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = Color.White };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        lblTongSo.Location = new Point(20, 12);
        lblDungLuong.Location = new Point(250, 13);
        pnlBottom.Controls.AddRange(new Control[] { lblTongSo, lblDungLuong });

        // 4. Center Area (Chứa flowGallery và dgvList)
        var pnlCenter = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Bg };
        pnlCenter.Controls.Add(flowGallery);
        pnlCenter.Controls.Add(dgvList);

        // Add All
        Controls.Add(pnlCenter);
        Controls.Add(pnlBottom);
        Controls.Add(pnlFilter);
        Controls.Add(pnlHeader);
    }

    private void SetupDataGrid()
    {
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaHinhAnh", HeaderText = "ID", FillWeight = 30 });
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenGoc", HeaderText = "Tên tệp gốc", FillWeight = 110 });
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoaiHinhAnh", HeaderText = "Phân loại", FillWeight = 80 });
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "DungLuong", HeaderText = "Dung lượng", FillWeight = 50 });
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "KichThuoc", HeaderText = "Độ phân giải", FillWeight = 55 });
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "NhayCam", HeaderText = "Độ nhạy cảm", FillWeight = 60 });
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "NguoiTai", HeaderText = "Người tải lên", FillWeight = 75 });
        dgvList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThoiGian", HeaderText = "Thời điểm tải", FillWeight = 70 });

        dgvList.CellFormatting += (s, e) =>
        {
            if (dgvList.Columns[e.ColumnIndex].Name == "NhayCam" && e.Value != null)
            {
                var val = e.Value.ToString();
                if (val!.Contains("Nhạy cảm"))
                {
                    e.CellStyle!.ForeColor = UITheme.Red;
                    e.CellStyle.Font = UITheme.FontBold(8.5f);
                }
                else
                {
                    e.CellStyle!.ForeColor = UITheme.Green;
                }
            }
        };

        dgvList.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && dgvList.Rows[e.RowIndex].Tag is ImageDto dto)
            {
                XemChiTiet(dto);
            }
        };
    }

    private async void LoadData()
    {
        try
        {
            var filter = new ImageFilterRequest
            {
                Keyword = txtTimKiem.Text.Trim(),
                LoaiHinhAnh = cboLoaiHinhAnh.SelectedIndex > 0 ? cboLoaiHinhAnh.SelectedItem?.ToString() : null,
                ChiHienNhayCam = chkChiNhayCam.Checked ? true : null
            };

            var doiTuong = cboLocDoiTuong.SelectedIndex;
            if (doiTuong == 1) filter.ChiHoSoThiThe = true; // Chỉ hồ sơ tử thi
            else if (doiTuong == 2) filter.ChiHoSoMatTich = true; // Chỉ hồ sơ người mất tích

            using var db = Program.NewDb();
            var service = new ImageService(db);
            _currentImages = await service.LayDanhSachHinhAnhAsync(filter, _coQuyenXemNhayCam);

            FilterAndRender();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi tải danh sách hình ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FilterAndRender()
    {
        var kw = txtTimKiem.Text.Trim().ToLower();
        var filtered = _currentImages;
        if (!string.IsNullOrEmpty(kw))
        {
            filtered = filtered.Where(x => x.TenGoc.ToLower().Contains(kw) ||
                                           (x.MoTa != null && x.MoTa.ToLower().Contains(kw)) ||
                                           x.NguoiTaiLen.ToLower().Contains(kw)).ToList();
        }

        long totalBytes = filtered.Sum(x => x.KichThuoc);
        int sensitiveCount = filtered.Count(x => x.LaNhayCam);

        lblTongSo.Text = $"Tổng số: {filtered.Count} hình ảnh | Đang hiển thị: {filtered.Count} tệp";
        lblDungLuong.Text = $"Dung lượng: {totalBytes / (1024.0 * 1024.0):F2} MB (Trong đó {sensitiveCount} ảnh nhạy cảm)";

        if (_isGridView)
            RenderGrid(filtered);
        else
            RenderGallery(filtered);
    }

    private void RenderGallery(List<ImageDto>? images = null)
    {
        var list = images ?? _currentImages;
        flowGallery.SuspendLayout();
        flowGallery.Controls.Clear();

        if (list.Count == 0)
        {
            var pnlEmpty = new Panel { Size = new Size(600, 120), Margin = new Padding(200, 100, 0, 0), BackColor = Color.Transparent };
            var lblEmpty = new Label
            {
                Text = "Không tìm thấy hình ảnh nào phù hợp với bộ lọc hiện tại.",
                Font = UITheme.FontBold(11),
                ForeColor = UITheme.Muted,
                AutoSize = true,
                Location = new Point(20, 40)
            };
            pnlEmpty.Controls.Add(lblEmpty);
            flowGallery.Controls.Add(pnlEmpty);
            flowGallery.ResumeLayout();
            return;
        }

        foreach (var img in list)
        {
            flowGallery.Controls.Add(CreateImageCard(img));
        }

        flowGallery.ResumeLayout();
    }

    private Panel CreateImageCard(ImageDto img)
    {
        var card = new Panel
        {
            Size = new Size(220, 275),
            Margin = new Padding(10, 10, 12, 12),
            BackColor = Color.White
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            if (img.LaNhayCam)
            {
                using var redBrush = new SolidBrush(UITheme.Red);
                e.Graphics.FillRectangle(redBrush, 0, 0, card.Width, 3);
            }
        };

        // Thumbnail PictureBox
        var pic = new PictureBox
        {
            Location = new Point(10, 10),
            Size = new Size(200, 140),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = ColorTranslator.FromHtml("#222222"),
            Cursor = Cursors.Hand
        };

        // Load ảnh thumbnail nhẹ nhàng
        LoadThumbnail(pic, img);
        pic.Click += (s, e) => XemChiTiet(img);

        // Badge nhạy cảm
        if (img.LaNhayCam)
        {
            var badge = new Label
            {
                Text = "⚠️ NHẠY CẢM",
                BackColor = UITheme.Red,
                ForeColor = Color.White,
                Font = UITheme.FontBold(7),
                Padding = new Padding(4, 2, 4, 2),
                AutoSize = true,
                Location = new Point(12, 12)
            };
            card.Controls.Add(badge);
            badge.BringToFront();
        }

        // Tên file gốc
        var lblName = new Label
        {
            Text = img.TenGoc,
            Font = UITheme.FontBold(8.5f),
            ForeColor = UITheme.Navy,
            Size = new Size(200, 36),
            Location = new Point(10, 155),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        lblName.Click += (s, e) => XemChiTiet(img);

        // Metadata ngắn gọn
        var lblType = new Label
        {
            Text = $"🏷️ {img.LoaiHinhAnh}",
            Font = UITheme.FontRegular(7.5f),
            ForeColor = UITheme.Blue,
            Size = new Size(200, 16),
            Location = new Point(10, 192),
            AutoEllipsis = true
        };

        var lblSize = new Label
        {
            Text = $"📦 {img.KichThuoc / 1024.0:F1} KB | {img.ThoiGianTaiLen:dd/MM/yyyy}",
            Font = UITheme.FontRegular(7.5f),
            ForeColor = UITheme.Muted,
            Size = new Size(200, 16),
            Location = new Point(10, 210)
        };

        // Nút xem chi tiết & xóa
        var btnView = new Button
        {
            Text = "XEM ẢNH",
            Location = new Point(10, 234),
            Size = new Size(130, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Soft,
            ForeColor = UITheme.Navy,
            Font = UITheme.FontBold(7.5f),
            Cursor = Cursors.Hand
        };
        btnView.FlatAppearance.BorderColor = UITheme.Line;
        btnView.Click += (s, e) => XemChiTiet(img);

        var btnDel = new Button
        {
            Text = "🗑️",
            Location = new Point(148, 234),
            Size = new Size(62, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = UITheme.Red,
            Font = UITheme.FontBold(8),
            Cursor = Cursors.Hand
        };
        btnDel.FlatAppearance.BorderColor = UITheme.Line;
        btnDel.Click += async (s, e) => await XoaAnhAsync(img);

        card.Controls.AddRange(new Control[] { pic, lblName, lblType, lblSize, btnView, btnDel });
        return card;
    }

    private void LoadThumbnail(PictureBox pic, ImageDto img)
    {
        try
        {
            var service = new ImageService(Program.NewDb());
            var physicalPath = service.LayDuongDanVatLy(img.DuongDan);

            if (File.Exists(physicalPath))
            {
                using var stream = new MemoryStream(File.ReadAllBytes(physicalPath));
                using var temp = System.Drawing.Image.FromStream(stream);
                pic.Image = new Bitmap(temp);
            }
            else
            {
                pic.Image = TaoThumbPlaceholder(img.LoaiHinhAnh, img.LaNhayCam);
            }
        }
        catch
        {
            pic.Image = TaoThumbPlaceholder(img.LoaiHinhAnh, true);
        }
    }

    private static Bitmap TaoThumbPlaceholder(string text, bool isSensitive)
    {
        var bmp = new Bitmap(200, 140);
        using var g = Graphics.FromImage(bmp);
        g.Clear(isSensitive ? ColorTranslator.FromHtml("#2d1815") : ColorTranslator.FromHtml("#1f2937"));
        using var brush = new SolidBrush(isSensitive ? ColorTranslator.FromHtml("#e74c3c") : Color.LightGray);
        using var f = UITheme.FontBold(8);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(isSensitive ? "⚠️ ẢNH BẢO VỆ\n" + text : text, f, brush, new RectangleF(0, 0, 200, 140), sf);
        return bmp;
    }

    private void RenderGrid(List<ImageDto>? images = null)
    {
        var list = images ?? _currentImages;
        dgvList.Rows.Clear();
        foreach (var img in list)
        {
            var r = dgvList.Rows.Add(
                img.MaHinhAnh,
                img.TenGoc,
                img.LoaiHinhAnh,
                $"{img.KichThuoc / 1024.0:F1} KB",
                img.ChieuRong.HasValue ? $"{img.ChieuRong}×{img.ChieuCao}" : "-",
                img.LaNhayCam ? "⚠️ Nhạy cảm" : "Công khai",
                img.NguoiTaiLen,
                img.ThoiGianTaiLen.ToString("dd/MM/yyyy HH:mm")
            );
            dgvList.Rows[r].Tag = img;
        }
    }

    private void XemChiTiet(ImageDto img)
    {
        using var f = new FrmXemChiTietAnh(img);
        f.ShowDialog(this);
    }

    private async Task XoaAnhAsync(ImageDto img)
    {
        var xacNhan = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa hình ảnh:\n\"{img.TenGoc}\" (Mã #{img.MaHinhAnh}) khỏi hệ thống?",
            "Xác nhận xóa tệp",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (xacNhan != DialogResult.Yes) return;

        try
        {
            using var db = Program.NewDb();
            var service = new ImageService(db);
            var ok = await service.XoaHinhAnhAsync(img.MaHinhAnh, AccessControl.UserId ?? 0);
            if (ok)
            {
                MessageBox.Show("Đã xóa hình ảnh thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            else
            {
                MessageBox.Show("Không thể xóa hình ảnh.", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi xóa ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
