using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models;

public class Response
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nội dung phản hồi không được để trống")]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ReflectionId { get; set; }

    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Reflection? Reflection { get; set; }

    public User? User { get; set; }
}