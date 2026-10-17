
using System.Collections.Generic;

namespace StudentFeedback.Models.ViewModels;

public class AdminStudentListViewModel
{
    public List<AdminStudentItemViewModel> Students { get; set; }
        = new();

    public string? Search { get; set; }

    public int TotalStudents { get; set; }

    public int TotalReflections { get; set; }
}

public class AdminStudentItemViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? StudentCode { get; set; }

    public string? ClassName { get; set; }

    public int ReflectionCount { get; set; }
}
