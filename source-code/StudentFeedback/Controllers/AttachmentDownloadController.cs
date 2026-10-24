
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;
using StudentFeedback.Services;

namespace StudentFeedback.Controllers;

[Authorize(Roles = "Student,Admin")]
[Route("Attachments")]
public class AttachmentDownloadController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly MinioStorageService _storage;
    private readonly ILogger<AttachmentDownloadController> _logger;

    public AttachmentDownloadController(
        ApplicationDbContext db,
        MinioStorageService storage,
        ILogger<AttachmentDownloadController> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    // GET: /Attachments/Download/1
    [HttpGet("Download/{id:int}")]
    public async Task<IActionResult> Download(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId))
        {
            return Forbid();
        }

        var role = User.IsInRole("Admin")
            ? "Admin"
            : "Student";

        var accountIsActive = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == userId &&
                u.Role == role &&
                u.IsActive,
                cancellationToken);

        if (!accountIsActive)
        {
            return Forbid();
        }

        // Truy vấn có kiểm tra quyền sở hữu ngay tại database.
        var attachment = await _db.Attachments
            .AsNoTracking()
            .Where(a =>
                a.Id == id &&
                a.Reflection != null &&
                (role == "Admin" ||
                 a.Reflection.UserId == userId))
            .Select(a => new
            {
                a.Id,
                a.FileName,
                a.ObjectKey,
                a.ContentType,
                a.FileSize
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (attachment == null)
        {
            return NotFound();
        }

        // Giới hạn bộ nhớ sử dụng khi tải tệp.
        const long maxDownloadSize = 10 * 1024 * 1024;

        if (attachment.FileSize <= 0 ||
            attachment.FileSize > maxDownloadSize)
        {
            return StatusCode(
                422,
                "Kích thước tệp không hợp lệ.");
        }

        try
        {
            using var stream = new MemoryStream();

            await _storage.DownloadAsync(
                attachment.ObjectKey,
                stream,
                cancellationToken);

            if (stream.Length != attachment.FileSize)
            {
                _logger.LogWarning(
                    "Kích thước tệp MinIO không khớp. AttachmentId={Id}",
                    id);

                return StatusCode(
                    502,
                    "Dữ liệu tệp không khớp.");
            }

            // Không sử dụng đường dẫn từ tên tệp người dùng.
            var safeFileName = Path.GetFileName(
                attachment.FileName.Replace('\\', '/'));

            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = $"attachment-{id}";
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store";

            return File(
                stream.ToArray(),
                "application/octet-stream",
                safeFileName);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Không thể tải tệp MinIO. AttachmentId={Id}",
                id);

            return StatusCode(
                502,
                "Không thể tải tệp từ hệ thống lưu trữ.");
        }
    }
}
