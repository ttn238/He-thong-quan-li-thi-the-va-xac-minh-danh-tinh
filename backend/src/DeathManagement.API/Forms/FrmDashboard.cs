namespace DeathManagement.API.Forms;

public class FrmDashboard : Form
{
    public FrmDashboard()
    {
        var tk = Session.TaiKhoan!;

        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Trang chủ";
        ClientSize = new Size(520, 260);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 12f);

        var lbl = new Label
        {
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(30, 30),
            Text = $"Đăng nhập thành công\nXin chào: {tk.HoTen}\nVai trò: {tk.TenVaiTro}\nĐơn vị: {Session.DonVi}",
        };

        var btn = new Button { Text = "ĐĂNG XUẤT", Location = new Point(30, 170), Size = new Size(150, 40) };
        btn.Click += (_, _) =>
        {
            Session.Clear();
            DialogResult = DialogResult.Retry;
        };

        Controls.Add(lbl);
        Controls.Add(btn);
    }
}