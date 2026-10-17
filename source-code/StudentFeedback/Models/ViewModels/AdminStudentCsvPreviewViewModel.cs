
using System.Collections.Generic;

namespace StudentFeedback.Models.ViewModels;

public class AdminStudentCsvPreviewViewModel
{
    public string? FileName { get; set; }

    public int TotalRows { get; set; }

    public int ValidRows { get; set; }

    public int InvalidRows { get; set; }

    public List<AdminStudentCsvRowViewModel> Rows { get; set; }
        = new();

    public List<string> FileErrors { get; set; }
        = new();
}

public class AdminStudentCsvRowViewModel
{
    public int RowNumber { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ClassName { get; set; }

    public List<string> Errors { get; set; } = new();

    public bool IsValid => Errors.Count == 0;
}
