namespace DeathManagement.API.Entities;

/// <summary>
/// Đại diện cho Vị trí lưu trữ trong kho lạnh / nhà xác (Bảng ViTriLuuTru trong CSDL QL_XACMINHTHITHE)
/// Phân cấp 3 tầng vật lý: Khu -> Tủ -> Ngăn
/// </summary>
public class StorageLocation
{
    public int MaViTri { get; set; }
    public string Khu { get; set; } = string.Empty;
    public string Tu { get; set; } = string.Empty;
    public string Ngan { get; set; } = string.Empty;
    public int SucChua { get; set; } = 1;
}
