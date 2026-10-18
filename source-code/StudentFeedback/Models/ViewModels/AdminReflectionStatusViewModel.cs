
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models.ViewModels;

public class AdminReflectionStatusViewModel
{
    [Range(1, int.MaxValue)]
    public int ReflectionId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [RegularExpression(
        "^(Pending|InProgress|Resolved|Closed)$",
        ErrorMessage = "Trạng thái không hợp lệ.")]
    public string Status { get; set; } = string.Empty;
}
