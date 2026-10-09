using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models;

public class Attachment
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int ReflectionId { get; set; }

    [Required(ErrorMessage = "Tên tệp không được để trống")]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(1024)]
    public string ObjectKey { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Reflection? Reflection { get; set; }
}