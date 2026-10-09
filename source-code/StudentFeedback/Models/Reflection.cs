using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models;

public class Reflection
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    [RegularExpression("^(Pending|InProgress|Resolved|Closed)$")]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }

    public Category? Category { get; set; }
}