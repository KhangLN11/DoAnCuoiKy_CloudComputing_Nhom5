
using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models;

public class PasswordSetupToken
{
    public int Id { get; set; }

    // Tài khoản sinh viên sở hữu token.
    [Required]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // Chỉ lưu SHA-256 của token, không lưu token gốc.
    [Required]
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    // Thời điểm tạo token (UTC).
    public DateTime CreatedAtUtc { get; set; }

    // Thời điểm token hết hạn (UTC).
    public DateTime ExpiresAtUtc { get; set; }

    // Đánh dấu token đã được sử dụng.
    public DateTime? UsedAtUtc { get; set; }
}
