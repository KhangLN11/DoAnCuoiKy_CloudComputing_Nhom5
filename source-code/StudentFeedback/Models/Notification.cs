
using System;
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models;

public class Notification
{
    public int Id { get; set; }

    // Sinh viên nhận thông báo.
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    // Phản ánh liên quan.
    [Range(1, int.MaxValue)]
    public int ReflectionId { get; set; }

    [Required(ErrorMessage = "Tiêu đề thông báo không được để trống.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung thông báo không được để trống.")]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    // StatusChanged hoặc AdminReplied.
    [Required]
    [StringLength(30)]
    [RegularExpression("^(StatusChanged|AdminReplied)$")]
    public string Type { get; set; } = "StatusChanged";

    // Thông báo mặc định chưa đọc.
    public bool IsRead { get; set; } = false;

    // Lưu UTC, chuyển sang giờ Việt Nam khi hiển thị.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Navigation properties.
    public User? User { get; set; }

    public Reflection? Reflection { get; set; }
}
