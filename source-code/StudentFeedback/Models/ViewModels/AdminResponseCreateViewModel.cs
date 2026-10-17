
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models.ViewModels;

public class AdminResponseCreateViewModel
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Phản ánh không hợp lệ.")]
    public int ReflectionId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi.")]
    [StringLength(4000,
        ErrorMessage = "Nội dung không được vượt quá 4000 ký tự.")]
    public string Content { get; set; } = string.Empty;
}
