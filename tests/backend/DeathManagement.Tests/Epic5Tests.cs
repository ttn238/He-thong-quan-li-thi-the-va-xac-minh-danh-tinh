using DeathManagement.API.DTOs.Ai;
using DeathManagement.API.DTOs.TinBao;
using DeathManagement.API.Helpers;
using Xunit;

namespace DeathManagement.Tests;

public class Epic5Tests
{
    [Theory]
    [InlineData("test.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("picture.png", true)]
    [InlineData("scan.bmp", true)]
    [InlineData("document.webp", true)]
    [InlineData("document.WEBP", true)]
    [InlineData("virus.exe", false)]
    [InlineData("notes.txt", false)]
    [InlineData("image.pdf", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void FileHelper_IsValidImageExtension_ValidatesCorrectly(string? fileName, bool expected)
    {
        var result = FileHelper.IsValidImageExtension(fileName!);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(10 * 1024 * 1024, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(10 * 1024 * 1024 + 1, false)]
    public void FileHelper_IsValidFileSize_EnforcesLimit(long fileSize, bool expected)
    {
        var result = FileHelper.IsValidFileSize(fileSize);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FileHelper_GenerateSafeFileName_RetainsExtensionAndRandomizes()
    {
        var name = FileHelper.GenerateSafeFileName("sample_document.png");
        Assert.EndsWith(".png", name, StringComparison.OrdinalIgnoreCase);
        Assert.True(name.Length >= 36);
        Assert.DoesNotContain("sample_document", name);
    }

    [Fact]
    public void FileHelper_ComputeMd5Hash_ComputesExpectedHash()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("TestMD5Hash");
        var hash = FileHelper.ComputeMd5Hash(data);
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Equal(32, hash.Length);
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("diagram.png", "image/png")]
    [InlineData("bitmap.bmp", "image/bmp")]
    [InlineData("scene.webp", "image/webp")]
    public void ImageHelper_GetMimeType_ReturnsCorrectMime(string fileName, string expectedMime)
    {
        var mime = ImageHelper.GetMimeType(fileName);
        Assert.Equal(expectedMime, mime);
    }

    [Fact]
    public void ImageHelper_GetMimeType_ThrowsOnUnsupported()
    {
        Assert.Throws<InvalidOperationException>(() => ImageHelper.GetMimeType("document.pdf"));
    }

    [Theory]
    [InlineData("ThiThe_KhamNghiem", null, true)]
    [InlineData("ThiThe_KhuonMat", null, true)]
    [InlineData("ThiThe_ToanThan", null, true)]
    [InlineData("HienTruong_VatDung", 123L, true)]
    [InlineData("HienTruong_VatDung", null, false)]
    [InlineData("MatTich_ChanDung", null, false)]
    public void ImageHelper_IsSensitiveImage_ClassifiesSensitivity(string type, long? bodyId, bool expected)
    {
        var result = ImageHelper.IsSensitiveImage(type, bodyId);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AiResultDto_Normalize_HandlesNullCollections()
    {
        var dto = new AiResultDto
        {
            RequestId = null!,
            Status = null!,
            Ocr = null!,
            Objects = null!,
            Locations = null!,
            MissingFields = null!
        };

        dto.Normalize();

        Assert.NotNull(dto.RequestId);
        Assert.NotNull(dto.Status);
        Assert.NotNull(dto.Ocr);
        Assert.NotNull(dto.Objects);
        Assert.NotNull(dto.Locations);
        Assert.NotNull(dto.MissingFields);
    }

    [Fact]
    public void GuiTinBaoRequest_StoresFieldsProperly()
    {
        var now = DateTime.Now;
        var req = new GuiTinBaoRequest
        {
            MaLoaiTinBao = 1,
            TieuDe = "Phát hiện sự việc",
            NoiDung = "Nội dung chi tiết sự việc tại bờ sông",
            ThoiGianXayRa = now,
            DiaDiem = "Phường An Phú, TP Thủ Đức",
            HoTenNguoiBao = "Nguyễn Văn A",
            SoDienThoaiNguoiBao = "0901234567",
            MucDoUuTien = "BinhThuong"
        };

        Assert.Equal(1, req.MaLoaiTinBao);
        Assert.Equal("Phát hiện sự việc", req.TieuDe);
        Assert.Equal("BinhThuong", req.MucDoUuTien);
        Assert.Equal(now, req.ThoiGianXayRa);
    }
}
