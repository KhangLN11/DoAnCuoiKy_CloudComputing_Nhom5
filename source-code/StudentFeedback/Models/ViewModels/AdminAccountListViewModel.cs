
using System.Collections.Generic;

namespace StudentFeedback.Models.ViewModels;

public class AdminAccountListViewModel
{
    public List<AdminAccountItemViewModel> Accounts { get; set; } = new();

    public string? Search { get; set; }

    public string? RoleFilter { get; set; }

    public int TotalAccounts { get; set; }

    public int TotalStudents { get; set; }

    public int TotalAdmins { get; set; }
}

public class AdminAccountItemViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? StudentCode { get; set; }

    public string? ClassName { get; set; }
}
