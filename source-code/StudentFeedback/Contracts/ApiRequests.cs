using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Contracts;

public sealed class CategoryRequest
{
    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed class ReflectionCreateRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public sealed class ReflectionUpdateRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    [RegularExpression("^(Pending|InProgress|Resolved|Closed)$", ErrorMessage = "Trạng thái không hợp lệ")]
    public string Status { get; set; } = "Pending";
}

public sealed class ResponseCreateRequest
{
    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ReflectionId { get; set; }

    [Range(1, int.MaxValue)]
    public int UserId { get; set; }
}

public sealed class ResponseUpdateRequest
{
    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
}
