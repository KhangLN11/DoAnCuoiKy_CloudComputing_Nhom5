
using System;
using System.Collections.Generic;

namespace StudentFeedback.Models.ViewModels;

public class StudentNotificationViewModel
{
    public int TotalCount { get; set; }

    public int UnreadCount { get; set; }

    public List<StudentNotificationItemViewModel> Items { get; set; }
        = new();
}

public class StudentNotificationItemViewModel
{
    public int Id { get; set; }

    public int ReflectionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
