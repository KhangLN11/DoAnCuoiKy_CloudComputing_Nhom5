
using System;
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models.ViewModels
{
    public class StudentProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string StudentCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string EditFullName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }
    }
}
