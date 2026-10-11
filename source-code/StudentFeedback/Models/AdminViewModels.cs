
namespace StudentFeedback.Models
{
    public class AdminReflectionVM
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Content { get; set; } = "";
        public string Status { get; set; } = "Mới tạo";
        public DateTime CreatedAt { get; set; }
        public string AdminResponse { get; set; } = "";
    }

    public class AdminCategoryVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int ReflectionCount { get; set; }
    }

    public class AdminUserVM
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "Sinh viên";
        public string Status { get; set; } = "Hoạt động";
    }

    public class AdminDashboardVM
    {
        public int Total { get; set; }
        public int NewCount { get; set; }
        public int ProcessingCount { get; set; }
        public int ResolvedCount { get; set; }

        public List<AdminReflectionVM> RecentReflections { get; set; }
            = new();
    }
}
