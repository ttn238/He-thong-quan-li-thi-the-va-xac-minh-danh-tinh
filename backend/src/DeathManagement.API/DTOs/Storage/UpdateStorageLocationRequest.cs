using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.Storage;

public class UpdateStorageLocationRequest
{
    [Required(ErrorMessage = "Khu vực lưu trữ không được để trống.")]
    [StringLength(50, ErrorMessage = "Tên khu tối đa 50 ký tự.")]
    public string Khu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tủ lưu trữ không được để trống.")]
    [StringLength(50, ErrorMessage = "Tên tủ tối đa 50 ký tự.")]
    public string Tu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngăn lưu trữ không được để trống.")]
    [StringLength(50, ErrorMessage = "Tên ngăn tối đa 50 ký tự.")]
    public string Ngan { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Sức chứa phải lớn hơn 0 (tối thiểu là 1 thi thể).")]
    public int SucChua { get; set; } = 1;
}
