
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models.ViewModels
{
    public class StudentCreateReflectionViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề phản ánh.")]
        [StringLength(
            200,
            MinimumLength = 1,
            ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Vui lòng chọn danh mục.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung phản ánh.")]
        [StringLength(
            4000,
            MinimumLength = 1,
            ErrorMessage = "Nội dung không được vượt quá 4000 ký tự.")]
        public string Content { get; set; } = string.Empty;

        public List<StudentCreateCategoryViewModel> Categories
            { get; set; } = new();
    }

    public class StudentCreateCategoryViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
