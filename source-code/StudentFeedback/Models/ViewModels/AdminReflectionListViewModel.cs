
using StudentFeedback.Models;

namespace StudentFeedback.Models.ViewModels;

public class AdminReflectionListViewModel
{
    // Danh sách phản ánh
    public List<Reflection> Reflections { get; set; } = new();

    // Danh mục dùng cho bộ lọc
    public List<Category> Categories { get; set; } = new();

    // Điều kiện tìm kiếm
    public string? Search { get; set; }

    // Điều kiện lọc
    public string? Status { get; set; }

    public int? CategoryId { get; set; }

    // Thống kê
    public int TotalCount { get; set; }

    public int PendingCount { get; set; }

    public int InProgressCount { get; set; }

    public int ResolvedCount { get; set; }

    public int ClosedCount { get; set; }
}
