using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StudentFeedback.Contracts;

public sealed class AttachmentUploadRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ReflectionId không hợp lệ.")]
    public int ReflectionId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn file.")]
    public IFormFile? File { get; set; }
}