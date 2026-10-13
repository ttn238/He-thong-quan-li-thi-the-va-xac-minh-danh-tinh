namespace DeathManagement.API.Forms;

public static class UITheme
{
    public static readonly Color Navy = ColorTranslator.FromHtml("#0f2b4d");
    public static readonly Color Blue = ColorTranslator.FromHtml("#1f5fae");
    public static readonly Color BlueHover = ColorTranslator.FromHtml("#174c8c");
    public static readonly Color Muted = ColorTranslator.FromHtml("#7a8699");
    public static readonly Color Line = ColorTranslator.FromHtml("#d9e0ea");
    public static readonly Color Bg = ColorTranslator.FromHtml("#eef2f7");
    public static readonly Color Soft = ColorTranslator.FromHtml("#f6f8fb");
    public static readonly Color Red = ColorTranslator.FromHtml("#c0392b");
    public static readonly Color Green = ColorTranslator.FromHtml("#1fa971");
    public static readonly Color Gold = ColorTranslator.FromHtml("#f2c94c");
    public static readonly Color DarkText = ColorTranslator.FromHtml("#1e293b");

    public static Font FontRegular(float size = 9.5f) => new("Segoe UI", size, FontStyle.Regular);
    public static Font FontBold(float size = 9.5f) => new("Segoe UI", size, FontStyle.Bold);
    public static Font FontSemiBold(float size = 9.5f) => new("Segoe UI", size, FontStyle.Bold);
    public static Font FontHeading(float size = 14f) => new("Segoe UI", size, FontStyle.Bold);
}
