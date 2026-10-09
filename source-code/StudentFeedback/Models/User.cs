using System.ComponentModel.DataAnnotations;

namespace StudentFeedback.Models;

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
}