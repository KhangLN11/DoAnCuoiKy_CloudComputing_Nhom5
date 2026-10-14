
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models.ViewModels;

public class AdminCategoryViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
    public string? Description { get; set; }
}
