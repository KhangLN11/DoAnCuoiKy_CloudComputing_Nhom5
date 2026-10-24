
using System;
using System.Collections.Generic;

namespace StudentFeedback.Models.ViewModels
{
    public class StudentReflectionDetailsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<StudentReflectionResponseViewModel> Responses
            { get; set; } = new();
    }

    public class StudentReflectionResponseViewModel
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string AdminName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
