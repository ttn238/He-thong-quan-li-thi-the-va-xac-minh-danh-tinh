using System.Drawing.Drawing2D;
using System.Globalization;
using DeathManagement.API.DTOs.Ai;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using static DeathManagement.API.Security.AuthorizationPolicies;

namespace DeathManagement.API.Forms;

/// <summary>Màn "Phân tích AI & Đối chiếu": hiển thị kết quả AI theo thiết kế Stitch (6 trạng thái).</summary>
public class FrmKetQuaAI : Form
{
    // ===== Bảng màu Stitch "Administrative Precision" =====
    static readonly Color CSurface = ColorTranslator.FromHtml("#f8f9ff");
    static readonly Color CHead = ColorTranslator.FromHtml("#eff4ff");
    static readonly Color CChip = ColorTranslator.FromHtml("#dce9ff");
    static readonly Color CNavy = ColorTranslator.FromHtml("#0f2a4a");
    static readonly Color COk = ColorTranslator.FromHtml("#95f8a7");
    static readonly Color COkText = ColorTranslator.FromHtml("#00210a");
    static readonly Color CErr = ColorTranslator.FromHtml("#ba1a1a");
    static readonly Color CErrBg = ColorTranslator.FromHtml("#ffdad6");
    static readonly Color CErrText = ColorTranslator.FromHtml("#93000a");
    static readonly Color CText = ColorTranslator.FromHtml("#0d1c2e");
    static readonly Color CSub = ColorTranslator.FromHtml("#43474e");
    static readonly Color CLine = ColorTranslator.FromHtml("#c4c6cf");
    static readonly Color CWarnBg = ColorTranslator.FromHtml("#fff1c2");
    static readonly Color CSlate = ColorTranslator.FromHtml("#46617c");
    // màu khung nhận diện trên ảnh
    static readonly Color COcr = ColorTranslator.FromHtml("#fbbf24");
    static readonly Color CPlate = ColorTranslator.FromHtml("#34d399");
    static readonly Color CVeh = ColorTranslator.FromHtml("#fb923c");
    static readonly Color CItem = ColorTranslator.FromHtml("#a5b4fc");

    static readonly Font F8 = UITheme.FontRegular(8.5f), F9 = UITheme.FontRegular(9f);
    static readonly Font B8 = UITheme.FontBold(8f), B9 = UITheme.FontBold(9f), B10 = UITheme.FontBold(10f), B12 = UITheme.FontBold(12f);
    static readonly Font Mono = new("Consolas", 8.5f);
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    const double LowConf = 0.8;   // dưới ngưỡng này thì cảnh báo "độ tin cậy thấp"

    // ===== Trạng thái =====
    private AiResultDto? _data;
    private AiServiceException? _ex;
    private bool _busy, _demo = true, _hienBox = true, _anhMau;
    private AiMau _mau = AiMau.CoKetQua;
    private string? _filePath, _idCanMo;
    private Bitmap? _anh;
    private AiLocationItem? _diaDiemChon;
    private CancellationTokenSource? _cts;
    private int _anim;
    private bool _laying;
    private Row? _barRow, _anhRow;

    private readonly List<Control> _dyn = new();
    private readonly List<Card> _full = new(), _left = new(), _right = new();
    private Card? _review;
    private CheckedListBox? _clb;
    private RadioButton[] _rdoKl = Array.Empty<RadioButton>();
    private TextBox? _txtGhiChu;
    private Button? _btnLuu;

    /// <summary>Phát ra khi cán bộ bấm "Áp dụng vào hồ sơ" ở một địa điểm gợi ý (để form khác đăng ký nhận).</summary>
    public event Action<AiLocationItem>? LocationApplied;

    // ===== Điều khiển =====
    private readonly Panel pnlBody = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = CSurface };
    private readonly Label lblCrumb = new() { AutoSize = true, Font = B9, ForeColor = CText, Location = new Point(16, 21) };
    private readonly Label lblNguon = new() { AutoSize = true, Font = F8, ForeColor = CSub, Margin = new Padding(14, 9, 0, 0) };
    private readonly Label lblKetNoi = new() { Dock = DockStyle.Right, AutoSize = true, Font = B8, ForeColor = Color.Gold, Padding = new Padding(0, 15, 16, 0), Text = "● AI-SERVICE: ĐANG KIỂM TRA..." };
    private readonly FlowLayoutPanel pnlSwitch = new() { BackColor = CChip, Height = 84, Padding = new Padding(10, 8, 10, 4), WrapContents = true };
    private readonly RadioButton[] rdoMau = new RadioButton[6];
    private readonly Button btnLamMoi = NutHead("⟳  Làm mới (F5)", 130, true);
    private readonly Button btnIn = NutHead("🖨  In kết quả", 120, false);
    private readonly Button btnDong = NutHead("←  Đóng", 90, false);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 80 };
    private Row _banner = null!;

    public FrmKetQuaAI()
    {
        Text = "Phân tích AI & Đối chiếu - Hiển thị kết quả AI";
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(1000, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CSurface;
        Font = UITheme.FontRegular(9.5f);
        KeyPreview = true;

        BuildUI();

        Load += (s, e) =>
        {
            if (!AccessControl.Can(ResourceAiResult, ActionView)) { BeginInvoke(new Action(Close)); return; }
            Chay(KiemTraKetNoiAsync);
            if (_idCanMo != null) Chay(() => MoTheoMaAsync(_idCanMo)); else HienMau(AiMau.CoKetQua);
        };
    }

    /// <summary>Mở thẳng một kết quả đã có trên ai-service theo mã yêu cầu (AI-REQ-...).</summary>
    public FrmKetQuaAI(string requestId) : this() { _idCanMo = requestId; }

    // =====================================================================
    //  XÂY GIAO DIỆN KHUNG
    // =====================================================================
    private static Button NutHead(string text, int w, bool primary)
    {
        var b = new Button
        {
            Text = text,
            Size = new Size(w, 34),
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? CNavy : Color.White,
            ForeColor = primary ? Color.White : CNavy,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = CLine;
        return b;
    }

    private void BuildUI()
    {
        // Thanh trên cùng (giống tông FrmDashboard)
        var pnlNav = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = CNavy };
        pnlNav.Controls.Add(lblKetNoi);
        pnlNav.Controls.Add(new Label
        {
            Text = "QLHC & PHÁP Y QUỐC GIA   |   PHÂN TÍCH AI & ĐỐI CHIẾU",
            ForeColor = Color.White,
            Font = UITheme.FontBold(10.5f),
            AutoSize = true,
            Location = new Point(16, 12)
        });

        // Thanh breadcrumb + nút thao tác
        var pnlHead = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.White };
        var flowBtn = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(0, 14, 16, 0),
            BackColor = Color.Transparent
        };
        flowBtn.Controls.AddRange(new Control[] { btnDong, btnIn, btnLamMoi });
        pnlHead.Controls.Add(flowBtn);
        pnlHead.Controls.Add(lblCrumb);
        pnlHead.Paint += (s, e) => { using var p = new Pen(CLine); e.Graphics.DrawLine(p, 0, pnlHead.Height - 1, pnlHead.Width, pnlHead.Height - 1); };

        btnIn.Enabled = false;
        new ToolTip().SetToolTip(btnIn, "Chưa hỗ trợ in kết quả");
        btnDong.Click += (s, e) => Close();
        btnLamMoi.Click += (s, e) => Chay(LamMoiAsync);

        // Banner khuyến cáo pháp lý
        _banner = new Row(96, (row, g, r) =>
        {
            using (var b = new SolidBrush(CNavy)) g.FillRectangle(b, r);
            const string chip = "ĐIỀU 88 BỘ LUẬT TỐ TỤNG HÌNH SỰ";
            int cw = MeasureChip(g, chip);
            Chip(g, chip, r.Width - cw - 14, 12, CChip, CNavy);
            Txt(g, "⚖  KHUYẾN CÁO PHÁP LÝ & QUY TRÌNH NGHIỆP VỤ BẮT BUỘC", B10, Color.White, new Rectangle(14, 11, r.Width - cw - 40, 20), single: true);
            Txt(g, "Kết quả phân tích trí tuệ nhân tạo (AI) chỉ mang tính chất tham khảo kỹ thuật ban đầu, phục vụ sàng lọc dấu vết và định hướng đối soát. " +
                   "Dữ liệu này HOÀN TOÀN KHÔNG THAY THẾ kết luận giám định chính thức của Giám định viên tư pháp hoặc Hội đồng chuyên môn nghiệp vụ có thẩm quyền. " +
                   "Cán bộ thụ lý chịu trách nhiệm thẩm định thực địa trước khi áp dụng vào hồ sơ tố tụng.",
                F9, Color.FromArgb(220, 232, 255), new Rectangle(14, 38, r.Width - 28, 54));
        })
        { BackColor = CNavy };

        // Thanh chuyển 6 trạng thái (dữ liệu mẫu) + nút phân tích ảnh thật
        pnlSwitch.Controls.Add(new Label
        {
            Text = "TRÌNH CHUYỂN ĐỔI 6 TRẠNG THÁI UI:",
            Font = B8,
            ForeColor = CNavy,
            AutoSize = true,
            Margin = new Padding(0, 10, 10, 0)
        });
        string[] ten = { "1. Đang xử lý", "2. Hoàn thành có kết quả", "3. Không có kết quả", "4. Lỗi dịch vụ 504", "5. Dữ liệu khuyết thiếu", "6. Đang thử lại" };
        var thuTu = new[] { AiMau.DangXuLy, AiMau.CoKetQua, AiMau.KhongCoKetQua, AiMau.Loi504, AiMau.KhuyetThieu, AiMau.DangThuLai };
        for (int i = 0; i < 6; i++)
        {
            var rb = new RadioButton
            {
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                Text = ten[i],
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(i == 1 ? 175 : 140, 30),
                Margin = new Padding(0, 4, 6, 0),
                Font = UITheme.FontBold(8.5f),
                BackColor = Color.White,
                ForeColor = CNavy,
                Tag = thuTu[i],
                Cursor = Cursors.Hand
            };
            rb.FlatAppearance.BorderColor = CLine;
            rb.FlatAppearance.CheckedBackColor = CNavy;
            rb.CheckedChanged += (s, e) => { var r2 = (RadioButton)s!; r2.ForeColor = r2.Checked ? Color.White : CNavy; };
            rb.Click += (s, e) => HienMau((AiMau)((RadioButton)s!).Tag!);
            rdoMau[i] = rb;
            pnlSwitch.Controls.Add(rb);
        }
        var btnThat = new Button
        {
            Text = "📂  Chọn ảnh & phân tích bằng dịch vụ AI thật",
            Size = new Size(300, 30),
            Margin = new Padding(0, 4, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.Blue,
            ForeColor = Color.White,
            Font = UITheme.FontBold(8.5f),
            Cursor = Cursors.Hand
        };
        btnThat.FlatAppearance.BorderSize = 0;
        btnThat.Click += (s, e) => ChonAnh();
        pnlSwitch.Controls.Add(btnThat);
        pnlSwitch.Controls.Add(lblNguon);

        pnlBody.Controls.Add(_banner);
        pnlBody.Controls.Add(pnlSwitch);
        pnlBody.ClientSizeChanged += (s, e) => LayoutPage();

        _timer.Tick += (s, e) => { _anim += 12; _barRow?.Invalidate(); };

        Controls.Add(pnlBody);
        Controls.Add(pnlHead);
        Controls.Add(pnlNav);
    }

    // =====================================================================
    //  CÁC LỚP VẼ: Row (một dòng tự vẽ) và Card (thẻ có tiêu đề)
    // =====================================================================
    private sealed class Row : Control
    {
        public readonly List<(Rectangle R, Action A)> Hits = new();
        public Func<int, int>? HeightFor;
        private readonly Action<Row, Graphics, Rectangle> _draw;

        public Row(int height, Action<Row, Graphics, Rectangle> draw)
        {
            _draw = draw;
            Height = height;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Hits.Clear();
            e.Graphics.Clear(BackColor);
            _draw(this, e.Graphics, ClientRectangle);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = Hits.Any(h => h.R.Contains(e.Location)) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            foreach (var h in Hits.ToList())
                if (h.R.Contains(e.Location)) { BeginInvoke(h.A); break; }
        }

        /// <summary>Vẽ nút bấm và đăng ký vùng click.</summary>
        public void Btn(Graphics g, Rectangle r, string text, bool primary, Action act, bool enabled = true)
        {
            using (var back = new SolidBrush(!enabled ? CHead : primary ? CNavy : Color.White)) g.FillRectangle(back, r);
            using (var pen = new Pen(primary && enabled ? CNavy : CLine)) g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
            TextRenderer.DrawText(g, text, B8, r, !enabled ? CSub : primary ? Color.White : CNavy,
                TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (enabled) Hits.Add((r, act));
        }
    }

    private sealed class Card : Panel
    {
        public const int HeadH = 34;
        private readonly List<Control> _rows = new();
        private Rectangle _act;
        public string Title;
        public string? RightChip;
        public Color RightBack = CChip, RightFore = CNavy, Border = CLine, HeadBack = CHead;
        public string? ActionText;
        public Action? OnAction;

        public Card(string title)
        {
            Title = title;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
        }

        public void AddRow(Control c) { _rows.Add(c); Controls.Add(c); }

        /// <summary>Xếp các dòng theo bề rộng được cấp, trả về chiều cao thẻ.</summary>
        public int Arrange(int width)
        {
            int y = HeadH;
            foreach (var c in _rows)
            {
                if (c is Row r && r.HeightFor != null) r.Height = r.HeightFor(width - 2);
                c.SetBounds(1, y, width - 2, c.Height);
                y += c.Height;
            }
            return y + 1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var b = new SolidBrush(HeadBack)) g.FillRectangle(b, 0, 0, Width, HeadH);
            using (var p = new Pen(Border))
            {
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                g.DrawLine(p, 0, HeadH - 1, Width, HeadH - 1);
            }
            int right = Width - 10;
            _act = Rectangle.Empty;
            if (!string.IsNullOrEmpty(ActionText))
            {
                int w = TextRenderer.MeasureText(g, ActionText, B8, new Size(600, 20), TextFormatFlags.NoPadding).Width + 6;
                _act = new Rectangle(right - w, 8, w, 18);
                TextRenderer.DrawText(g, ActionText, B8, _act, UITheme.Blue, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                right = _act.X - 10;
            }
            if (!string.IsNullOrEmpty(RightChip))
            {
                int w = MeasureChip(g, RightChip);
                Chip(g, RightChip, right - w, 8, RightBack, RightFore);
                right -= w + 10;
            }
            TextRenderer.DrawText(g, Title, B9, new Rectangle(10, 0, Math.Max(40, right - 10), HeadH), CText,
                TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = OnAction != null && _act.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (OnAction != null && _act.Contains(e.Location)) OnAction();
        }
    }

    // ===== Hàm vẽ / định dạng dùng chung =====
    static void Txt(Graphics g, string s, Font f, Color c, Rectangle r, TextFormatFlags al = TextFormatFlags.Left, bool single = false)
    {
        var fl = TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | al;
        fl |= single ? TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter : TextFormatFlags.WordBreak | TextFormatFlags.Top;
        TextRenderer.DrawText(g, s, f, r, c, fl);
    }

    static int MeasureChip(Graphics g, string s, Font? f = null)
        => TextRenderer.MeasureText(g, s, f ?? B8, new Size(1200, 24), TextFormatFlags.NoPadding).Width + 12;

    static int Chip(Graphics g, string s, int x, int y, Color back, Color fore, Font? f = null)
    {
        int w = MeasureChip(g, s, f);
        var r = new Rectangle(x, y, w, 18);
        using (var b = new SolidBrush(back)) g.FillRectangle(b, r);
        TextRenderer.DrawText(g, s, f ?? B8, r, fore, TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return w;
    }

    static string Pct(double c) => (c * 100).ToString("0.0", Inv) + "%";
    static string Dash(string? s) => string.IsNullOrWhiteSpace(s) ? "Chưa có dữ liệu" : s;
    static string Bbox(int[]? b) => b is { Length: 4 } ? "[" + string.Join(", ", b) + "]" : "Không có tọa độ";
    static string Dt(string? iso) => DateTimeOffset.TryParse(iso, out var d) ? d.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") : "Chưa có dữ liệu";
    static string Toa(double la, double ln) => la.ToString("0.000000", Inv) + ", " + ln.ToString("0.000000", Inv);
    static void Copy(string s) { try { Clipboard.SetText(s); } catch { } }

    static void MoBanDo(double la, double ln)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                $"https://www.google.com/maps?q={la.ToString(Inv)},{ln.ToString(Inv)}")
            { UseShellExecute = true });
        }
        catch { }
    }

    static Row Ghi(string text) => new(46, (row, g, r) => Txt(g, text, F9, CSub, new Rectangle(12, 8, r.Width - 24, r.Height - 12)));

    static Row HangNut(params (string Text, bool Primary, Action Act)[] nuts) => new(56, (row, g, r) =>
    {
        var ws = nuts.Select(n => TextRenderer.MeasureText(g, n.Text, B8, new Size(900, 20), TextFormatFlags.NoPadding).Width + 36).ToArray();
        int x = (r.Width - ws.Sum() - 10 * (nuts.Length - 1)) / 2;
        for (int i = 0; i < nuts.Length; i++)
        {
            row.Btn(g, new Rectangle(x, 12, ws[i], 32), nuts[i].Text, nuts[i].Primary, nuts[i].Act);
            x += ws[i] + 10;
        }
    });

    // =====================================================================
    //  BỐ CỤC TRANG (tính tay, cuộn dọc)
    // =====================================================================
    private Card Dat(List<Card>? list, Card c)
    {
        pnlBody.Controls.Add(c);
        _dyn.Add(c);
        list?.Add(c);
        return c;
    }

    private void LayoutPage()
    {
        if (_laying) return;
        _laying = true;
        try
        {
            int off = pnlBody.AutoScrollPosition.Y;   // <= 0 khi đang cuộn
            int x = 16, gap = 14;
            int W = Math.Max(600, pnlBody.ClientSize.Width - 2 * x);
            int y = 16 + off;

            void Full(Control c, int h) { c.SetBounds(x, y, W, h); y += h + gap; }

            Full(_banner, _banner.Height);
            Full(pnlSwitch, pnlSwitch.Height);
            foreach (var c in _full) Full(c, c.Arrange(W));

            if (_left.Count + _right.Count > 0)
            {
                int wl = (int)((W - gap) * 0.42), wr = W - gap - wl;
                int yl = y, yr = y;
                foreach (var c in _left) { int h = c.Arrange(wl); c.SetBounds(x, yl, wl, h); yl += h + gap; }
                foreach (var c in _right) { int h = c.Arrange(wr); c.SetBounds(x + wl + gap, yr, wr, h); yr += h + gap; }
                y = Math.Max(yl, yr);
            }
            if (_review != null) Full(_review, _review.Arrange(W));

            pnlBody.AutoScrollMinSize = new Size(0, y - off + 8);
        }
        finally { _laying = false; }
    }

    // =====================================================================
    //  VẼ THEO TRẠNG THÁI
    // =====================================================================
    private void Render()
    {
        if (IsDisposed) return;
        foreach (var c in _dyn) { pnlBody.Controls.Remove(c); c.Dispose(); }
        _dyn.Clear(); _full.Clear(); _left.Clear(); _right.Clear();
        _review = null; _barRow = null; _anhRow = null; _clb = null; _txtGhiChu = null; _btnLuu = null;
        _timer.Stop();

        var view = AiViewHelper.Get(_data, _busy, _ex != null);
        var d = _data;
        switch (view)
        {
            case AiViewState.Loading:
            case AiViewState.Processing:
                Dat(_full, TheXuLy(view == AiViewState.Loading ? null : d)); break;
            case AiViewState.Retrying:
                Dat(_full, TheThuLai(d!)); break;
            case AiViewState.Error:
                Dat(_full, TheLoi()); break;
            case AiViewState.Empty:
                Dat(_full, TheMeta(d!));
                Dat(_full, TheRong(d!)); break;
            default:   // Success | Partial
                if (view == AiViewState.Partial) Dat(_full, TheKhuyet(d!));
                Dat(_left, TheMeta(d!));
                Dat(_left, TheAnh(d!));
                Dat(_right, TheOcr(d!));
                Dat(_right, TheDoiTuong(d!));
                Dat(_right, TheDiaDiem(d!));
                _review = Dat(null, TheThamDinh(d!));
                break;
        }

        // thanh breadcrumb + nhãn nguồn dữ liệu + trạng thái nút chọn
        lblCrumb.Text = "Hệ Thống DVI   ›   Phân Tích AI & Đối Chiếu   ›   #" + (string.IsNullOrEmpty(_data?.RequestId) ? "—" : _data!.RequestId) + "      CẤP ĐỘ: NGHIỆP VỤ ĐIỀU TRA";
        lblNguon.Text = _demo ? "(Dữ liệu mẫu kiểm thử UI/UX - Mockup Test Data)" : "(Kết quả thật từ dịch vụ AI)";
        for (int i = 0; i < rdoMau.Length; i++)
            rdoMau[i].Checked = _demo && (AiMau)rdoMau[i].Tag! == _mau;

        pnlBody.AutoScrollPosition = new Point(0, 0);
        LayoutPage();
        pnlBody.Invalidate(true);
    }

    // ---------- Thẻ trạng thái: đang xử lý / đang phân tích ----------
    private Card TheXuLy(AiResultDto? d)
    {
        int? pct = d?.Progress;
        var c = new Card("Đang phân tích hình ảnh chứng cứ");
        c.AddRow(new Row(120, (row, g, r) =>
        {
            Txt(g, "Đang phân tích hình ảnh chứng cứ", B12, CText, new Rectangle(0, 14, r.Width, 24), TextFormatFlags.HorizontalCenter, true);
            Txt(g, "Động cơ: " + (d == null ? "dịch vụ AI (ai-service)" : Dash(d.Engine)), F9, CSub, new Rectangle(0, 42, r.Width, 18), TextFormatFlags.HorizontalCenter, true);
            Txt(g, "Hệ thống đang bóc tách điểm ảnh, nhận dạng chữ (OCR) và truy vấn định danh đồ vật. Quá trình thường diễn ra trong vài giây. Giao diện sẽ tự cập nhật khi có kết quả.",
                F9, CSub, new Rectangle(r.Width / 2 - 320, 68, 640, 44), TextFormatFlags.HorizontalCenter);
        }));
        _barRow = ThanhTienDo(pct);
        c.AddRow(_barRow);
        if (pct is int p)
            c.AddRow(new Row(100, (row, g, r) =>
            {
                int x = r.Width / 2 - 200;
                Txt(g, "TIẾN TRÌNH XỬ LÝ ĐA TẦNG:", B8, CSub, new Rectangle(x, 8, 400, 16), single: true);
                string[] bc = { "1. Khử nhiễu & Cân bằng", "2. Bóc tách OCR", "3. Định danh phương tiện..." };
                for (int i = 0; i < 3; i++)
                {
                    bool xong = p >= (i + 1) * 33 && i < 2 || p >= 100;
                    bool dang = !xong && (i == 0 || p >= i * 33);
                    Txt(g, (xong ? "✔  " : dang ? "▶  " : "○  ") + bc[i], F9, xong ? UITheme.Green : dang ? CNavy : CSub,
                        new Rectangle(x, 30 + i * 22, 400, 20), single: true);
                }
            }));
        if (pct == null) _timer.Start();
        return c;
    }

    private Row ThanhTienDo(int? pct) => new(54, (row, g, r) =>
    {
        var bar = new Rectangle(r.Width / 2 - 260, 12, 520, 10);
        using (var b = new SolidBrush(CChip)) g.FillRectangle(b, bar);
        using (var b = new SolidBrush(CNavy))
        {
            if (pct is int p) g.FillRectangle(b, bar.X, bar.Y, bar.Width * Math.Clamp(p, 0, 100) / 100, bar.Height);
            else
            {
                int seg = bar.Width / 3;
                var rc = Rectangle.Intersect(new Rectangle(bar.X + _anim % (bar.Width + seg) - seg, bar.Y, seg, bar.Height), bar);
                if (!rc.IsEmpty) g.FillRectangle(b, rc);
            }
        }
        Txt(g, pct is int q ? q + "% hoàn tất" : "Đang chờ phản hồi từ dịch vụ AI...", F8, CSub,
            new Rectangle(0, 30, r.Width, 16), TextFormatFlags.HorizontalCenter, true);
    });

    private Card TheThuLai(AiResultDto d)
    {
        var c = new Card("Đang tái gửi yêu cầu phân tích");
        c.AddRow(new Row(120, (row, g, r) =>
        {
            Txt(g, "Đang tái gửi yêu cầu phân tích", B12, CText, new Rectangle(0, 14, r.Width, 24), TextFormatFlags.HorizontalCenter, true);
            Txt(g, $"LẦN THỬ: {(d.Attempt ?? 1):00}/03 (KÊNH DỰ PHÒNG)", B8, CNavy, new Rectangle(0, 42, r.Width, 16), TextFormatFlags.HorizontalCenter, true);
            Txt(g, "Hệ thống đang tự động định tuyến lại gói tin phân tích qua kênh dự phòng. Vui lòng không đóng cửa sổ trong khi gửi dữ liệu.",
                F9, CSub, new Rectangle(r.Width / 2 - 320, 66, 640, 44), TextFormatFlags.HorizontalCenter);
        }));
        _barRow = ThanhTienDo(null);
        c.AddRow(_barRow);
        _timer.Start();
        return c;
    }

    // ---------- Thẻ trạng thái: không có kết quả ----------
    private Card TheRong(AiResultDto d)
    {
        var c = new Card("Không tìm thấy đặc điểm nhận dạng phù hợp");
        c.AddRow(new Row(132, (row, g, r) =>
        {
            Txt(g, "Không tìm thấy đặc điểm nhận dạng phù hợp", B12, CText, new Rectangle(0, 14, r.Width, 24), TextFormatFlags.HorizontalCenter, true);
            Txt(g, $"MÃ TRUY VẤN: #{d.RequestId} (RETURN_EMPTY_FEATURES)", Mono, CSub, new Rectangle(0, 42, r.Width, 16), TextFormatFlags.HorizontalCenter, true);
            Txt(g, "Động cơ AI đã quét toàn bộ khung hình nhưng không nhận ra văn bản, đối tượng hay địa điểm nào đủ độ tin cậy. Có thể do góc chụp quá hẹp, ngược sáng mạnh hoặc bề mặt phương tiện bị che lấp.",
                F9, CSub, new Rectangle(r.Width / 2 - 340, 68, 680, 56), TextFormatFlags.HorizontalCenter);
        }));
        c.AddRow(new Row(110, (row, g, r) =>
        {
            int x = r.Width / 2 - 300;
            Txt(g, "Khuyến nghị cho cán bộ điều tra hiện trường:", B9, CText, new Rectangle(x, 4, 600, 18), single: true);
            string[] ds =
            {
                "Chụp bổ sung ảnh trực diện biển kiểm soát, cự ly 1.5m - 2.5m, vuông góc với bề mặt phản quang.",
                "Làm sạch sơ bộ bùn đất bám trên số khung, số máy hoặc lô-gô nhận diện nhãn hiệu.",
                "Dùng đèn bù sáng chuyên dụng trong điều kiện ban đêm hoặc lóa chói ban ngày."
            };
            for (int i = 0; i < ds.Length; i++) Txt(g, "•  " + ds[i], F9, CSub, new Rectangle(x, 28 + i * 24, 600, 22), single: true);
        }));
        c.AddRow(HangNut(("⟳  Phân tích lại", true, () => Chay(LamMoiAsync))));
        return c;
    }

    // ---------- Thẻ trạng thái: lỗi dịch vụ ----------
    private static string Ly(int? h) => h switch
    {
        504 => "GATEWAY TIMEOUT",
        503 => "SERVICE UNAVAILABLE",
        400 => "BAD REQUEST",
        404 => "NOT FOUND",
        413 => "PAYLOAD TOO LARGE",
        409 => "CONFLICT",
        _ => "ERROR"
    };

    private Card TheLoi()
    {
        string code, msg, trace, when, endpoint; int? http;
        if (_ex != null)
        {
            code = _ex.Code; msg = _ex.Message; trace = _ex.TraceId; endpoint = _ex.Endpoint; http = _ex.HttpStatus;
            when = _ex.OccurredAt.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz");
        }
        else
        {
            var e = _data?.Error;
            code = e?.Code ?? "ERR_AI_UNKNOWN"; msg = e?.Message ?? "Không thể lấy kết quả phân tích.";
            trace = e?.TraceId ?? "—"; when = e?.OccurredAt ?? "—"; endpoint = e?.Endpoint ?? AiResultClient.BaseUrl; http = e?.HttpStatus;
        }
        var c = new Card("Lỗi kết nối Dịch vụ Trí tuệ nhân tạo")
        {
            Border = CErr,
            HeadBack = CErrBg,
            RightBack = CErr,
            RightFore = Color.White,
            RightChip = http != null ? $"HTTP {http} {Ly(http)}" : null
        };
        c.AddRow(new Row(64, (row, g, r) => Txt(g, msg, F9, CText, new Rectangle(14, 10, r.Width - 28, 48))));
        c.AddRow(new Row(112, (row, g, r) =>
        {
            Txt(g, "THÔNG TIN LỖI HỆ THỐNG (STACK TRACE):", B8, CSub, new Rectangle(14, 0, r.Width - 28, 16), single: true);
            var box = new Rectangle(14, 20, r.Width - 28, 88);
            using (var b = new SolidBrush(CHead)) g.FillRectangle(b, box);
            using (var p = new Pen(CLine)) g.DrawRectangle(p, box.X, box.Y, box.Width - 1, box.Height - 1);
            Txt(g, $"Timestamp: {when}\nEndpoint: {endpoint}\nError Code: {code}\nTrace ID: {trace}", Mono, CText,
                new Rectangle(box.X + 8, box.Y + 6, box.Width - 16, box.Height - 10));
        }));
        c.AddRow(HangNut(
            ("⟳  Thử kết nối lại ngay (Retry Request)", true, () => Chay(LamMoiAsync)),
            ("⧉  Sao chép thông tin lỗi", false, () => Copy($"{code} | HTTP {http} | {endpoint} | {trace} | {when}\n{msg}"))));
        return c;
    }

    // ---------- Thẻ dữ liệu khuyết thiếu ----------
    private static (int X, int W)[] Cols(int w)
    {
        int a = (int)(w * .28), b = (int)(w * .16), c = (int)(w * .28);
        return new[] { (0, a), (a, b), (a + b, c), (a + b + c, w - a - b - c) };
    }

    private Card TheKhuyet(AiResultDto d)
    {
        int thieu = d.MissingFields.Count(f => f.Status != "done");
        var c = new Card("⚠  Cảnh báo: Dữ liệu khuyết thiếu một phần (Partial AI Data)")
        {
            HeadBack = CWarnBg,
            RightBack = CErrBg,
            RightFore = CErrText,
            RightChip = $"{thieu}/{d.MissingFields.Count} TRƯỜNG DỮ LIỆU CHƯA THỂ SUY LUẬN"
        };
        c.AddRow(new Row(42, (row, g, r) => Txt(g,
            "Một số trường quan trọng chưa thể trích xuất do chất lượng ảnh nguồn hoặc do dịch vụ AI chưa hỗ trợ. Hệ thống hiển thị trạng thái mặc định an toàn thay vì giá trị rỗng/lỗi.",
            F8, CSub, new Rectangle(12, 6, r.Width - 24, 32))));
        c.AddRow(new Row(26, (row, g, r) =>
        {
            using (var b = new SolidBrush(CHead)) g.FillRectangle(b, r);
            string[] h = { "PHÂN HỆ NGHIỆP VỤ", "TRẠNG THÁI DỮ LIỆU", "GIÁ TRỊ TRÍCH XUẤT", "KHUYẾN NGHỊ XỬ LÝ" };
            var cl = Cols(r.Width);
            for (int i = 0; i < 4; i++) Txt(g, h[i], B8, CSub, new Rectangle(cl[i].X + 10, 4, cl[i].W - 14, 18), single: true);
        }));
        foreach (var f in d.MissingFields)
        {
            var (st, back, fore) = f.Status switch
            {
                "done" => ("HOÀN TẤT", COk, COkText),
                "missing" => ("KHÔNG CÓ DỮ LIỆU", CErrBg, CErrText),
                "out_of_scope" => ("NGOÀI PHẠM VI ẢNH", CChip, CNavy),
                _ => ("CHƯA HỖ TRỢ", CChip, CNavy)
            };
            c.AddRow(new Row(46, (row, g, r) =>
            {
                var cl = Cols(r.Width);
                Txt(g, f.Field, B9, CText, new Rectangle(cl[0].X + 10, 6, cl[0].W - 14, 36));
                Chip(g, st, cl[1].X + 10, 10, back, fore);
                Txt(g, Dash(f.Value), F8, CText, new Rectangle(cl[2].X + 10, 6, cl[2].W - 14, 36));
                Txt(g, Dash(f.Recommendation), F8, CSub, new Rectangle(cl[3].X + 10, 6, cl[3].W - 14, 36));
                using var p = new Pen(CHead);
                g.DrawLine(p, 0, r.Height - 1, r.Width, r.Height - 1);
            }));
        }
        return c;
    }

    // ---------- Thẻ thông tin yêu cầu ----------
    private Card TheMeta(AiResultDto d)
    {
        bool ok = d.Status == "completed";
        var c = new Card("Thông tin yêu cầu phân tích")
        {
            RightChip = ok ? "HOÀN TẤT XỬ LÝ (THAM KHẢO)" : d.Status == "failed" ? "XỬ LÝ THẤT BẠI" : "ĐANG XỬ LÝ",
            RightBack = ok ? COk : CErrBg,
            RightFore = ok ? COkText : CErrText
        };
        c.AddRow(new Row(200, (row, g, r) =>
        {
            int half = r.Width / 2;
            void KV(string k, string v, int x, int y, int w, Font? vf = null)
            {
                Txt(g, k, B8, CSub, new Rectangle(x, y, w, 14), single: true);
                Txt(g, v, vf ?? B9, CText, new Rectangle(x, y + 16, w, 32));
            }
            KV("MÃ YÊU CẦU AI", "#" + d.RequestId, 12, 10, half - 16);
            KV("HỒ SƠ LIÊN KẾT", Dash(d.LinkedRecordId), half, 10, half - 16);
            KV("THỜI ĐIỂM GỬI", Dt(d.SubmittedAt), 12, 58, half - 16);
            KV("HOÀN TẤT SUY LUẬN", Dt(d.CompletedAt) + (d.DurationMs is int ms ? $" ({(ms / 1000.0).ToString("0.00", Inv)}s)" : ""), half, 58, half - 16);
            KV("ĐỘNG CƠ XỬ LÝ (MODEL ENGINE)", Dash(d.Engine), 12, 106, r.Width - 24);
            KV("MÃ TOÀN VẸN CHỨNG CỨ SỐ (SHA-256)", Dash(d.ImageHash), 12, 150, r.Width - 24, Mono);
        }));
        return c;
    }

    // ---------- Thẻ ảnh đầu vào + khung nhận diện ----------
    private int ImgH(AiResultDto d, int w)
    {
        double iw = d.Image?.Width ?? _anh?.Width ?? 0, ih = d.Image?.Height ?? _anh?.Height ?? 0;
        int h = iw > 0 && ih > 0 ? (int)(w * ih / iw) : 240;
        return Math.Clamp(h, 180, 460);
    }

    private Card TheAnh(AiResultDto d)
    {
        var im = d.Image;
        var c = new Card("Hình ảnh đầu vào (Input Evidence Image)")
        {
            RightChip = im == null ? null : $"{((im.SizeBytes ?? 0) / 1024).ToString("N0", Inv)} KB · {Dash(im.Format)}"
        };
        c.AddRow(new Row(38, (row, g, r) =>
        {
            Txt(g, (im?.FileName ?? "KHÔNG CÓ TÊN TỆP").ToUpper(), B8, CText, new Rectangle(12, 0, r.Width - 190, r.Height), single: true);
            row.Btn(g, new Rectangle(r.Width - 168, 6, 156, 26), _hienBox ? "▣  Ẩn Bounding Box" : "▢  Hiện Bounding Box", true,
                () => { _hienBox = !_hienBox; row.Invalidate(); _anhRow?.Invalidate(); });
        }));
        _anhRow = new Row(240, (row, g, r) => VeAnh(g, r, d)) { BackColor = Color.FromArgb(32, 36, 44) };
        _anhRow.HeightFor = w => ImgH(d, w);
        c.AddRow(_anhRow);
        c.AddRow(new Row(30, (row, g, r) =>
        {
            int x = 12;
            foreach (var (col, t) in new[] { (COcr, "Vùng văn bản OCR"), (CPlate, "Biển số xe"), (CVeh, "Phương tiện"), (CItem, "Đồ vật / Dấu vết") })
            {
                using (var b = new SolidBrush(col)) g.FillRectangle(b, x, 10, 10, 10);
                Txt(g, t, F8, CSub, new Rectangle(x + 14, 7, 130, 16), single: true);
                x += 14 + TextRenderer.MeasureText(g, t, F8, new Size(400, 20), TextFormatFlags.NoPadding).Width + 14;
            }
        }));
        return c;
    }

    private void VeAnh(Graphics g, Rectangle r, AiResultDto d)
    {
        if (_anh == null)
        {
            Txt(g, "Không tải được ảnh đầu vào.", F9, Color.White, r, TextFormatFlags.HorizontalCenter, true);
            return;
        }
        double sw = d.Image?.Width ?? _anh.Width, sh = d.Image?.Height ?? _anh.Height;
        double k = Math.Min(r.Width / sw, r.Height / sh);
        int dw = (int)(sw * k), dh = (int)(sh * k), ox = r.X + (r.Width - dw) / 2, oy = r.Y + (r.Height - dh) / 2;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(_anh, new Rectangle(ox, oy, dw, dh));
        if (!_hienBox) return;

        int n = 0;
        void Box(int[]? bb, string label, Color col)
        {
            if (bb is not { Length: 4 }) return;
            n++;
            var rc = new Rectangle(ox + (int)(bb[0] * k), oy + (int)(bb[1] * k), Math.Max(2, (int)(bb[2] * k)), Math.Max(2, (int)(bb[3] * k)));
            using (var p = new Pen(col, 2)) g.DrawRectangle(p, rc);
            var sz = TextRenderer.MeasureText(g, label, B8, new Size(600, 20), TextFormatFlags.NoPadding);
            var tag = new Rectangle(rc.X, Math.Max(oy, rc.Y - sz.Height - 4), sz.Width + 8, sz.Height + 4);
            using (var b2 = new SolidBrush(col)) g.FillRectangle(b2, tag);
            TextRenderer.DrawText(g, label, B8, tag, Color.Black, TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        foreach (var o in d.Ocr)
            Box(o.Bbox, (o.Kind == "license_plate" ? "LP: " : "OCR: ") + o.Text + " " + Pct(o.Confidence), o.Kind == "license_plate" ? CPlate : COcr);
        foreach (var o in d.Objects)
            Box(o.Bbox, o.Label + " " + Pct(o.Confidence), o.Label.Contains("Phương tiện", StringComparison.OrdinalIgnoreCase) ? CVeh : CItem);

        var info = $"X: {sw:0} | Y: {sh:0} | Zoom: {(k * 100):0}% | BBox Count: {n}";
        var ir = new Rectangle(ox + 6, oy + dh - 22, 300, 18);
        using (var b3 = new SolidBrush(Color.FromArgb(170, 0, 0, 0))) g.FillRectangle(b3, ir);
        Txt(g, info, B8, Color.White, new Rectangle(ir.X + 6, ir.Y, ir.Width, ir.Height), single: true);
    }

    // ---------- 1. OCR ----------
    private Card TheOcr(AiResultDto d)
    {
        var c = new Card("1. Văn bản trích xuất quang học (OCR Module)")
        {
            ActionText = "⧉  Sao chép toàn bộ văn bản",
            OnAction = () => Copy(string.Join(Environment.NewLine, d.Ocr.Select(o => o.Text)))
        };
        if (d.Ocr.Count == 0) c.AddRow(Ghi("Không nhận dạng được văn bản nào trong ảnh."));
        foreach (var o in d.Ocr)
            c.AddRow(new Row(62, (row, g, r) =>
            {
                const int btnW = 96;
                bool low = o.Confidence < LowConf;
                Txt(g, o.Text, B10, CText, new Rectangle(12, 6, r.Width - btnW - 36, 20), single: true);
                var kind = o.Kind switch { "street_sign" => "BIỂN TÊN ĐƯỜNG", "license_plate" => "BIỂN SỐ XE", _ => "VĂN BẢN KHÁC" };
                int x = 12 + Chip(g, kind, 12, 34, CChip, CNavy) + 8;
                var tail = !string.IsNullOrWhiteSpace(o.Note) ? o.Note : "Tọa độ BBox: " + Bbox(o.Bbox);
                Txt(g, (low ? "Độ tin cậy thấp: " : "Độ tin cậy: ") + Pct(o.Confidence) + "  ·  " + tail, F8, low ? CErr : CSub,
                    new Rectangle(x, 34, Math.Max(40, r.Width - x - btnW - 24), 18), single: true);
                row.Btn(g, new Rectangle(r.Width - btnW - 10, 17, btnW, 26), "⧉  Sao chép", false, () => Copy(o.Text));
                using var p = new Pen(CHead);
                g.DrawLine(p, 0, r.Height - 1, r.Width, r.Height - 1);
            }));
        return c;
    }

    // ---------- 2. Đối tượng ----------
    private static void VeDoiTuong(Graphics g, AiObjectItem o, Rectangle r)
    {
        using (var bg = new SolidBrush(CSurface)) g.FillRectangle(bg, r);
        using (var p = new Pen(CLine)) g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
        bool low = o.Confidence < LowConf;
        var pc = Pct(o.Confidence);
        int w = MeasureChip(g, pc);
        Chip(g, pc, r.Right - w - 8, r.Y + 8, low ? CErrBg : COk, low ? CErrText : COkText);
        Txt(g, o.Label, B9, CText, new Rectangle(r.X + 8, r.Y + 6, r.Width - w - 24, 22), single: true);
        Txt(g, Dash(o.Description), F8, CSub, new Rectangle(r.X + 8, r.Y + 32, r.Width - 16, 48));
        Txt(g, o.Bbox is { Length: 4 } ? "BBox: " + Bbox(o.Bbox) : "Không có tọa độ", F8, CSub, new Rectangle(r.X + 8, r.Bottom - 22, r.Width / 2, 16), single: true);
        if (!string.IsNullOrWhiteSpace(o.Tag))
            Txt(g, o.Tag, B8, CNavy, new Rectangle(r.X + r.Width / 2, r.Bottom - 22, r.Width / 2 - 8, 16), TextFormatFlags.Right, true);
    }

    private Card TheDoiTuong(AiResultDto d)
    {
        var c = new Card("2. Đối tượng & Đặc điểm nhận diện (Detected Objects)") { RightChip = $"{d.Objects.Count} ĐỐI TƯỢNG PHÁT HIỆN" };
        if (d.Objects.Count == 0)
            c.AddRow(Ghi("Chưa phát hiện đối tượng/đặc điểm nào (mô hình nhận diện đối tượng chưa được tích hợp trong ai-service, hoặc ảnh không có đối tượng đủ độ tin cậy)."));
        for (int i = 0; i < d.Objects.Count; i += 2)
        {
            var a = d.Objects[i];
            var b = i + 1 < d.Objects.Count ? d.Objects[i + 1] : null;
            c.AddRow(new Row(122, (row, g, r) =>
            {
                int cw = (r.Width - 30) / 2;
                VeDoiTuong(g, a, new Rectangle(10, 8, cw, 106));
                if (b != null) VeDoiTuong(g, b, new Rectangle(20 + cw, 8, cw, 106));
            }));
        }
        return c;
    }

    // ---------- 3. Địa điểm suy luận ----------
    private Card TheDiaDiem(AiResultDto d)
    {
        var c = new Card("3. Địa điểm suy luận (Suggested Locations)")
        {
            RightChip = "AI GỢI Ý — CHƯA PHẢI TỌA ĐỘ XÁC MINH CHÍNH THỨC",
            RightBack = CSlate,
            RightFore = Color.White
        };
        var ds = d.Locations.OrderBy(x => x.Rank == 0 ? int.MaxValue : x.Rank).ToList();
        if (ds.Count == 0)
            c.AddRow(Ghi("Chưa có địa điểm suy luận (ai-service chưa tích hợp dữ liệu bản đồ). Cán bộ xác định địa điểm thủ công."));
        for (int i = 0; i < ds.Count; i++)
        {
            var l = ds[i];
            bool big = i == 0;
            int rank = l.Rank > 0 ? l.Rank : i + 1;
            c.AddRow(new Row(big ? 138 : 66, (row, g, r) =>
            {
                bool ok = l.Confidence >= LowConf;
                bool applied = ReferenceEquals(_diaDiemChon, l);
                int cw = Chip(g, "ƯU TIÊN " + rank, 12, 10, big ? CNavy : CChip, big ? Color.White : CNavy);
                Txt(g, l.Name, big ? B10 : B9, CText, new Rectangle(12 + cw + 8, 8, r.Width - cw - 40 - (big ? 0 : 130), 22), single: true);
                if (!big) Txt(g, "Độ tin cậy: " + Pct(l.Confidence), F8, CSub, new Rectangle(r.Width - 140, 8, 128, 22), TextFormatFlags.Right, true);

                string vung = Dash(l.AdminArea) + (l.Lat is double a && l.Lng is double b ? "  ·  Tọa độ: " + Toa(a, b) : "");
                if (big)
                {
                    int x = 12;
                    x += Chip(g, "Độ tin cậy: " + Pct(l.Confidence), x, 41, ok ? COk : CErrBg, ok ? COkText : CErrText) + 10;
                    if (l.Lat is double la && l.Lng is double ln)
                    {
                        row.Btn(g, new Rectangle(x, 38, 120, 26), "🗺  Xem bản đồ", false, () => MoBanDo(la, ln));
                        x += 130;
                    }
                    row.Btn(g, new Rectangle(x, 38, 160, 26), applied ? "✓  Đã áp dụng" : "✓  Áp dụng vào hồ sơ", !applied, () => ApDungDiaDiem(l));

                    int w3 = (r.Width - 24) / 3;
                    string[] k3 = { "ĐỊA BÀN HÀNH CHÍNH", "TỌA ĐỘ VỆ TINH (WGS-84)", "CƠ SỞ SUY LUẬN" };
                    string[] v3 = { Dash(l.AdminArea), l.Lat is double p1 && l.Lng is double p2 ? Toa(p1, p2) : "Chưa có dữ liệu", Dash(l.Basis) };
                    for (int j = 0; j < 3; j++)
                    {
                        Txt(g, k3[j], B8, CSub, new Rectangle(12 + j * w3, 76, w3 - 8, 14), single: true);
                        Txt(g, v3[j], F9, CText, new Rectangle(12 + j * w3, 92, w3 - 8, 40));
                    }
                }
                else
                {
                    Txt(g, vung, F8, CSub, new Rectangle(12, 38, r.Width - 190, 20), single: true);
                    row.Btn(g, new Rectangle(r.Width - 160, 34, 148, 26), applied ? "✓  Đã áp dụng" : "Chọn phương án " + rank, false, () => ApDungDiaDiem(l));
                }
                using var p = new Pen(CHead);
                g.DrawLine(p, 0, r.Height - 1, r.Width, r.Height - 1);
            }));
        }
        return c;
    }

    private void ApDungDiaDiem(AiLocationItem l)
    {
        _diaDiemChon = l;
        LocationApplied?.Invoke(l);
        pnlBody.Invalidate(true);
    }

    // ---------- Biên bản thẩm định của cán bộ ----------
    private sealed class KiemItem(string id, string label)
    {
        public string Id { get; } = id;
        public override string ToString() => label;
    }

    private Card TheThamDinh(AiResultDto d)
    {
        var tk = Session.TaiKhoan;
        var c = new Card("Biên bản đối chiếu & Thẩm định nghiệp vụ của Cán bộ thụ lý")
        {
            RightChip = "CÁN BỘ ĐIỀU TRA: " + (tk?.HoTen ?? "—") + (tk != null ? $" ({tk.TenVaiTro})" : "")
        };
        bool duocLuu = AccessControl.Can(ResourceAiResult, ActionConfirm) && d.Status == "completed";

        var host = new Panel { Height = 332, BackColor = Color.White };
        var lblCl = new Label { Text = "Danh mục đối soát dấu vết (Verification Checklist):", Font = B8, ForeColor = CSub, AutoSize = false };
        _clb = new CheckedListBox { CheckOnClick = true, Font = F9, BorderStyle = BorderStyle.FixedSingle, HorizontalScrollbar = true };
        var items = d.Ocr.Select((o, i) => ("ocr-" + i, "OCR: " + o.Text))
            .Concat(d.Objects.Select((o, i) => ("obj-" + i, "Đối tượng: " + o.Label)))
            .Concat(d.Locations.Select((l, i) => ("loc-" + i, "Địa điểm: " + l.Name)));
        foreach (var (id, label) in items) _clb.Items.Add(new KiemItem(id, label), d.Review?.CheckedItems.Contains(id) == true);

        var lblKl = new Label { Text = "Kết luận thẩm định của Cán bộ phụ trách:", Font = B8, ForeColor = CSub, AutoSize = false };
        string[] kl = { "XÁC NHẬN KẾT QUẢ", "YÊU CẦU BỔ SUNG", "BÁC BỎ KẾT QUẢ" };
        string[] mt = { "Có giá trị tham khảo và chuyển hồ sơ tiếp theo", "Thu thập thêm ảnh cự ly gần chất lượng cao", "Nhận diện sai lệch / Hiện trường bị biến dạng" };
        string cur = d.Review?.Decision ?? "confirm";
        _rdoKl = new RadioButton[3];
        var moTa = new Label[3];
        for (int i = 0; i < 3; i++)
        {
            _rdoKl[i] = new RadioButton
            {
                Text = kl[i],
                Font = B9,
                AutoSize = false,
                ForeColor = i == 2 ? CErr : CText,
                Checked = i == 0 ? cur == "confirm" || (cur != "need_more" && cur != "reject") : i == 1 ? cur == "need_more" : cur == "reject"
            };
            moTa[i] = new Label { Text = mt[i], Font = F8, ForeColor = CSub, AutoSize = false };
        }
        var lblGc = new Label { Text = "Ghi chú thẩm định & Đề xuất nghiệp vụ:", Font = B8, ForeColor = CSub, AutoSize = false };
        _txtGhiChu = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Font = F9, Text = d.Review?.Note ?? "", MaxLength = 1000 };
        var lblTb = new Label { Font = F8, ForeColor = CSub, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
        lblTb.Text = !duocLuu
            ? (d.Status != "completed" ? "Chỉ thẩm định được kết quả đã phân tích xong." : "Tài khoản chỉ có quyền XEM kết quả AI (không có quyền xác nhận).")
            : d.Review != null ? "Đã thẩm định lúc " + Dt(d.Review.ReviewedAt) + (string.IsNullOrEmpty(d.Review.Reviewer) ? "" : " bởi " + d.Review.Reviewer) : "Chưa có biên bản thẩm định.";

        var btnLai = new Button { Text = "⟳  Phân tích lại (AI Re-analyze)", FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = CNavy, Font = UITheme.FontBold(8.5f), Cursor = Cursors.Hand };
        btnLai.FlatAppearance.BorderColor = CLine;
        btnLai.Click += (s, e) => Chay(PhanTichLaiAsync);
        _btnLuu = new Button { Text = "💾  Lưu Biên Bản Thẩm Định (Ctrl+S)", FlatStyle = FlatStyle.Flat, BackColor = CNavy, ForeColor = Color.White, Font = UITheme.FontBold(8.5f), Cursor = Cursors.Hand };
        _btnLuu.FlatAppearance.BorderSize = 0;
        _btnLuu.Click += (s, e) => Chay(LuuThamDinhAsync);

        if (!duocLuu)
        {
            _clb.Enabled = false; _txtGhiChu.Enabled = false; _btnLuu.Enabled = false;
            foreach (var rb in _rdoKl) rb.Enabled = false;
        }

        host.Controls.AddRange(new Control[] { lblCl, _clb, lblKl, lblGc, _txtGhiChu, lblTb, btnLai, _btnLuu });
        host.Controls.AddRange(_rdoKl);
        host.Controls.AddRange(moTa);

        void Bo()
        {
            int W = host.ClientSize.Width, lw = (int)(W * 0.42);
            lblCl.SetBounds(12, 10, lw, 18);
            _clb!.SetBounds(12, 32, lw, 236);
            int rx = lw + 36, rw = Math.Max(120, W - rx - 12);
            lblKl.SetBounds(rx, 10, rw, 18);
            for (int i = 0; i < 3; i++)
            {
                _rdoKl[i].SetBounds(rx, 32 + i * 44, rw, 22);
                moTa[i].SetBounds(rx + 22, 54 + i * 44, rw - 22, 16);
            }
            lblGc.SetBounds(rx, 168, rw, 18);
            _txtGhiChu!.SetBounds(rx, 188, rw, 80);
            _btnLuu!.SetBounds(W - 12 - 270, 284, 270, 36);
            btnLai.SetBounds(_btnLuu.Left - 10 - 220, 284, 220, 36);
            lblTb.SetBounds(12, 290, Math.Max(100, btnLai.Left - 24), 24);
        }
        host.Resize += (s, e) => Bo();
        Bo();

        c.AddRow(host);
        return c;
    }

    // =====================================================================
    //  XỬ LÝ NGHIỆP VỤ
    // =====================================================================
    private async void Chay(Func<Task> viec)
    {
        try { await viec(); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!IsDisposed) MessageBox.Show(this, ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task KiemTraKetNoiAsync()
    {
        bool ok = await AiResultClient.PingAsync();
        if (IsDisposed) return;
        lblKetNoi.Text = ok ? "● AI-SERVICE: ĐÃ KẾT NỐI" : "● AI-SERVICE: KHÔNG KẾT NỐI ĐƯỢC";
        lblKetNoi.ForeColor = ok ? Color.LightGreen : Color.Salmon;
    }

    private void DatAnh(Bitmap? b, bool mau) { _anh?.Dispose(); _anh = b; _anhMau = mau; }

    private void HienMau(AiMau m)
    {
        _cts?.Cancel();
        _demo = true; _mau = m; _busy = false; _ex = null; _filePath = null; _diaDiemChon = null;
        _data = AiResultSamples.Tao(m);
        if (!_anhMau || _anh == null) DatAnh(TaoAnhMau(), true);
        Render();
    }

    private static Bitmap TaoAnhMau()
    {
        var bmp = new Bitmap(1920, 1080);
        using var g = Graphics.FromImage(bmp);
        using (var br = new LinearGradientBrush(new Rectangle(0, 0, 1920, 1080), Color.FromArgb(78, 88, 104), Color.FromArgb(28, 32, 40), 90f))
            g.FillRectangle(br, 0, 0, 1920, 1080);
        using (var br = new SolidBrush(Color.FromArgb(50, 255, 255, 255))) g.FillRectangle(br, 0, 760, 1920, 320);
        using var f = new Font("Segoe UI", 28, FontStyle.Bold);
        TextRenderer.DrawText(g, "ẢNH MẪU (placeholder) - HIENTRUONG_XUANLA_01.JPG", f, new Rectangle(0, 20, 1920, 60),
            Color.FromArgb(210, 255, 255, 255), TextFormatFlags.HorizontalCenter);
        return bmp;
    }

    private void ChonAnh()
    {
        if (!PermissionUi.Check(this, ResourceAiResult, ActionView)) return;
        using var dlg = new OpenFileDialog
        {
            Title = "Chọn ảnh hiện trường để phân tích",
            Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var path = dlg.FileName;
        Chay(() => PhanTichAsync(path));
    }

    private async Task PhanTichAsync(string path)
    {
        var fi = new FileInfo(path);
        var ext = fi.Extension.ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp"))
        { MessageBox.Show(this, "Chỉ hỗ trợ ảnh JPG, PNG, WEBP, BMP.", "Sai định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (fi.Length > 10 * 1024 * 1024)
        { MessageBox.Show(this, "Ảnh vượt quá 10 MB.", "Tệp quá lớn", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try { using var tmp = new Bitmap(path); DatAnh(new Bitmap(tmp), false); }
        catch { DatAnh(null, false); }

        _demo = false; _filePath = path; _busy = true; _ex = null; _data = null; _diaDiemChon = null;
        Render();
        try
        {
            var kq = await AiResultClient.AnalyzeAsync(path, null, token);
            if (token.IsCancellationRequested || IsDisposed) return;
            _data = kq;
        }
        catch (OperationCanceledException) { return; }
        catch (AiServiceException ex) { _ex = ex; }
        _busy = false;
        Render();
    }

    private async Task MoTheoMaAsync(string id)
    {
        _demo = false; _busy = true; _ex = null; _data = null;
        Render();
        try
        {
            _data = await AiResultClient.GetAsync(id);
            if (_data.Image?.Url is string u)
            {
                try
                {
                    var bytes = await AiResultClient.DownloadAsync(u);
                    using var ms = new MemoryStream(bytes);
                    using var tmp = new Bitmap(ms);
                    DatAnh(new Bitmap(tmp), false);
                }
                catch { DatAnh(null, false); }
            }
        }
        catch (AiServiceException ex) { _ex = ex; }
        _busy = false;
        Render();
    }

    private async Task LamMoiAsync()
    {
        if (_demo) { HienMau(_mau); return; }
        if (_data == null || string.IsNullOrEmpty(_data.RequestId))
        {
            if (_filePath != null) await PhanTichAsync(_filePath);
            return;
        }
        var id = _data.RequestId;
        _busy = true; Render();
        try { _data = await AiResultClient.GetAsync(id); _ex = null; }
        catch (AiServiceException ex) { _ex = ex; }
        _busy = false;
        Render();
    }

    private async Task PhanTichLaiAsync()
    {
        if (_demo) { HienMau(_mau); return; }
        if (_filePath == null) { MessageBox.Show(this, "Không còn ảnh gốc trên máy để phân tích lại. Hãy chọn ảnh mới.", "Phân tích lại"); return; }
        await PhanTichAsync(_filePath);
    }

    private async Task LuuThamDinhAsync()
    {
        if (_data == null || _clb == null || _txtGhiChu == null || _btnLuu == null || !_btnLuu.Enabled) return;
        if (!PermissionUi.Check(this, ResourceAiResult, ActionConfirm)) return;

        string qd = _rdoKl[1].Checked ? "need_more" : _rdoKl[2].Checked ? "reject" : "confirm";
        var note = _txtGhiChu.Text.Trim();
        if (qd != "confirm" && note.Length == 0)
        {
            MessageBox.Show(this, "Cần ghi chú lý do khi không xác nhận kết quả.", "Thiếu ghi chú", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtGhiChu.Focus();
            return;
        }
        var rv = new AiReviewDto
        {
            Decision = qd,
            Note = note,
            Reviewer = Session.TaiKhoan?.HoTen,
            CheckedItems = _clb.CheckedItems.OfType<KiemItem>().Select(x => x.Id).ToList()
        };
        if (_demo)
        {
            MessageBox.Show(this, "Đang xem DỮ LIỆU MẪU nên biên bản không được gửi lên dịch vụ AI.\nHãy bấm \"Chọn ảnh & phân tích bằng dịch vụ AI thật\" để lưu biên bản thật.",
                "Dữ liệu mẫu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var btn = _btnLuu;
        btn.Enabled = false;
        try
        {
            var kq = await AiResultClient.SaveReviewAsync(_data.RequestId, rv);
            _data.Review = kq;
            if (!IsDisposed)
                MessageBox.Show(this, "Đã lưu biên bản thẩm định lúc " + Dt(kq.ReviewedAt) + ".", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (AiServiceException ex)
        {
            if (!IsDisposed) MessageBox.Show(this, ex.Message, "Không lưu được biên bản", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { if (!btn.IsDisposed) btn.Enabled = true; }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5) { Chay(LamMoiAsync); return true; }
        if (keyData == (Keys.Control | Keys.S)) { Chay(LuuThamDinhAsync); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _cts?.Cancel();
        _timer.Dispose();
        _anh?.Dispose();
        base.OnFormClosed(e);
    }
}