
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Contracts;

// ============================
// CATEGORY REQUESTS
// ============================

public sealed class CategoryRequest
{
    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

// ============================
// REFLECTION REQUESTS
// ============================

// Student tạo phản ánh.
// UserId được xác định từ tài khoản đăng nhập.
public sealed class ReflectionCreateRequest
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue,
        ErrorMessage = "Danh mục không hợp lệ")]
    public int CategoryId { get; set; }
}

// Student cập nhật phản ánh.
// Status được quản lý ở phía server.
public sealed class ReflectionUpdateRequest
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue,
        ErrorMessage = "Danh mục không hợp lệ")]
    public int CategoryId { get; set; }
}

// ============================
// RESPONSE REQUESTS
// ============================


public sealed class ResponseCreateRequest
{
    [Required(ErrorMessage = "Nội dung phản hồi không được để trống")]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ReflectionId { get; set; }
}


public sealed class ResponseUpdateRequest
{
    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
}
