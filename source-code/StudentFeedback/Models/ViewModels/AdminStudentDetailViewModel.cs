
using StudentFeedback.Models;

namespace StudentFeedback.Models.ViewModels;

public class AdminStudentDetailViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? StudentCode { get; set; }

    public string? ClassName { get; set; }

    public List<Reflection> Reflections { get; set; } = new();

    public int TotalReflections { get; set; }

    public int PendingCount { get; set; }

    public int InProgressCount { get; set; }

    public int ResolvedCount { get; set; }

    public int ClosedCount { get; set; }
}
