using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFeedback.Services;

namespace StudentFeedback.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/MinioDiagnostics")]
public class MinioDiagnosticsController : Controller
{
    private readonly MinioStorageService _storage;
    private readonly ILogger<MinioDiagnosticsController> _logger;

    public MinioDiagnosticsController(
        MinioStorageService storage,
        ILogger<MinioDiagnosticsController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        try
        {
            await _storage.CheckConnectionAsync(
                cancellationToken);

            return Json(new
            {
                success = true,
                message = "ASP.NET Core đã kết nối MinIO thành công."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Không thể kết nối MinIO.");

            return StatusCode(503, new
            {
                success = false,
                message = "Không thể kết nối MinIO. Kiểm tra log server."
            });
        }
    }

    [HttpPost("EnsureBucket")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnsureBucket(
        CancellationToken cancellationToken)
    {
        try
        {
            await _storage.EnsureBucketExistsAsync(
                cancellationToken);

            return Json(new
            {
                success = true,
                message = "Bucket đã sẵn sàng."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Không thể tạo hoặc kiểm tra bucket MinIO.");

            return StatusCode(503, new
            {
                success = false,
                message = "Không thể chuẩn bị bucket MinIO."
            });
        }
    }
}