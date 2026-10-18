
namespace StudentFeedback.Models.ViewModels;

public class StudentDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string StudentCode { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public int TotalReflections { get; set; }

    public int PendingCount { get; set; }

    public int InProgressCount { get; set; }

    public int ResolvedCount { get; set; }

    public int ClosedCount { get; set; }

    public int CompletedCount => ResolvedCount + ClosedCount;
}
