
using System;
using System.Collections.Generic;

namespace StudentFeedback.Models.ViewModels
{
    public class StudentReflectionListViewModel
    {
        public string Search { get; set; } = string.Empty;

        public string Status { get; set; } = "all";

        public int? CategoryId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalItems { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling(
                TotalItems / (double)PageSize);

        public List<StudentReflectionListItemViewModel> Items
            { get; set; } = new();

        public List<StudentReflectionCategoryViewModel> Categories
            { get; set; } = new();
    }

    public class StudentReflectionListItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public class StudentReflectionCategoryViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
