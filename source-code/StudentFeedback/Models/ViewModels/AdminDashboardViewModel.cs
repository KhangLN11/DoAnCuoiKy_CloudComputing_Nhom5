
namespace StudentFeedback.Models.ViewModels;

public class AdminDashboardViewModel
{
    // Thống kê tổng quan
    public int TotalReflections { get; set; }

    public int PendingCount { get; set; }

    public int InProgressCount { get; set; }

    public int ResolvedCount { get; set; }

    public int ClosedCount { get; set; }

    // Thống kê theo danh mục
    public List<AdminDashboardCategoryItemViewModel>
        CategoryStatistics { get; set; } = new();

    // Danh sách phản ánh gần đây
    public List<AdminDashboardRecentReflectionViewModel>
        RecentReflections { get; set; } = new();
}

public class AdminDashboardCategoryItemViewModel
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int ReflectionCount { get; set; }
}

public class AdminDashboardRecentReflectionViewModel
{
    public int Id { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
