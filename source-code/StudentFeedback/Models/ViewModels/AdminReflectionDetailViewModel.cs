
using StudentFeedback.Models;

namespace StudentFeedback.Models.ViewModels;

public class AdminReflectionDetailViewModel
{
    public Reflection Reflection { get; set; } = null!;

    public List<Response> Responses { get; set; } = new();

    public List<Attachment> Attachments { get; set; } = new();
}
