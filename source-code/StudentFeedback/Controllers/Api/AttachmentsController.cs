
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;
using StudentFeedback.Services;

namespace StudentFeedback.Controllers.Api;

[ApiController]
[Authorize(Roles = "Student,Admin")]
[Route("api/attachments")]
public sealed class AttachmentsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private const int MaxFilesPerReflection = 5;

    private readonly ApplicationDbContext _db;
    private readonly MinioStorageService _storage;
    private readonly IMinioClient _minio;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(
        ApplicationDbContext db,
        MinioStorageService storage,
        IMinioClient minio,
        IConfiguration configuration,
        ILogger<AttachmentsController> logger)
    {
        _db = db;
        _storage = storage;
        _minio = minio;
        _configuration = configuration;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        string? claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out int id) && id > 0
            ? id
            : null;
    }

    private async Task<bool> IsActiveUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        bool isAdmin = User.IsInRole("Admin");
        bool isStudent = User.IsInRole("Student");

        return await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == userId &&
                u.IsActive &&
                (
                    (isAdmin && u.Role == "Admin") ||
                    (isStudent && u.Role == "Student")
                ),
                cancellationToken);
    }

    // GET: /api/attachments
    // GET: /api/attachments?reflectionId=16
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? reflectionId,
        CancellationToken cancellationToken)
    {
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        if (!await IsActiveUserAsync(
            userId.Value, cancellationToken))
        {
            return Forbid();
        }

        if (reflectionId.HasValue &&
            reflectionId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "ReflectionId phải lớn hơn 0."
            });
        }

        var query = _db.Attachments
            .AsNoTracking()
            .AsQueryable();

        // Student chỉ được xem file thuộc phản ánh của mình.
        if (!User.IsInRole("Admin"))
        {
            query = query.Where(a =>
                a.Reflection != null &&
                a.Reflection.UserId == userId.Value);
        }

        if (reflectionId.HasValue)
        {
            query = query.Where(a =>
                a.ReflectionId == reflectionId.Value);
        }

        var items = await query
            .OrderByDescending(a => a.UploadedAt)
            .ThenByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.ReflectionId,
                a.FileName,
                a.ContentType,
                a.FileSize,
                a.UploadedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    // GET: /api/attachments/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        if (!await IsActiveUserAsync(
            userId.Value, cancellationToken))
        {
            return Forbid();
        }

        if (id <= 0)
            return NotFound();

        var query = _db.Attachments
            .AsNoTracking()
            .Where(a => a.Id == id);

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(a =>
                a.Reflection != null &&
                a.Reflection.UserId == userId.Value);
        }

        var item = await query
            .Select(a => new
            {
                a.Id,
                a.ReflectionId,
                a.FileName,
                a.ContentType,
                a.FileSize,
                a.UploadedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? NotFound()
            : Ok(item);
    }

    // GET: /api/attachments/5/download
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(
        int id,
        CancellationToken cancellationToken)
    {
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        if (!await IsActiveUserAsync(
            userId.Value, cancellationToken))
        {
            return Forbid();
        }

        if (id <= 0)
            return NotFound();

        var query = _db.Attachments
            .AsNoTracking()
            .Where(a => a.Id == id);

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(a =>
                a.Reflection != null &&
                a.Reflection.UserId == userId.Value);
        }

        var attachment = await query
            .Select(a => new
            {
                a.FileName,
                a.ObjectKey,
                a.FileSize
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (attachment is null)
            return NotFound();

        if (attachment.FileSize <= 0 ||
            attachment.FileSize > MaxFileSize)
        {
            return StatusCode(422, new
            {
                message = "Kích thước file không hợp lệ."
            });
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
                    "Kích thước file MinIO không khớp. AttachmentId={Id}",
                    id);

                return StatusCode(502, new
                {
                    message = "Dữ liệu file không khớp."
                });
            }

            string safeFileName = Path.GetFileName(
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
                "Không thể download AttachmentId={Id}",
                id);

            return StatusCode(502, new
            {
                message = "Không thể tải file từ MinIO."
            });
        }
    }

    // POST: /api/attachments
    // multipart/form-data: ReflectionId + File
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] AttachmentUploadRequest request,
        CancellationToken cancellationToken)
    {
        int? userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized();

        bool activeStudent = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == userId.Value &&
                u.Role == "Student" &&
                u.IsActive,
                cancellationToken);

        if (!activeStudent)
            return Forbid();

        int reflectionId = request.ReflectionId;
        IFormFile? file = request.File;

        if (reflectionId <= 0)
        {
            return BadRequest(new
            {
                message = "ReflectionId không hợp lệ."
            });
        }

        // Student chỉ được upload vào phản ánh của mình.
        var reflection = await _db.Reflections
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Id == reflectionId &&
                r.UserId == userId.Value,
                cancellationToken);

        if (reflection is null)
            return NotFound();

        // Không cho upload khi Admin đã bắt đầu xử lý.
        if (reflection.Status != "Pending")
        {
            return BadRequest(new
            {
                message =
                    "Chỉ có thể đính kèm file khi phản ánh đang Pending."
            });
        }

        if (file is null)
        {
            return BadRequest(new
            {
                message = "Vui lòng chọn file."
            });
        }

        string fileName = Path.GetFileName(
            file.FileName.Replace('\\', '/'));

        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Length > 255 ||
            file.Length <= 0 ||
            file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message =
                    "Tên file không hợp lệ hoặc file vượt quá 10 MB."
            });
        }

        string extension = Path.GetExtension(fileName)
            .ToLowerInvariant();

        if (extension != ".pdf" &&
            extension != ".jpg" &&
            extension != ".jpeg" &&
            extension != ".png")
        {
            return BadRequest(new
            {
                message = "Chỉ hỗ trợ PDF, JPG, JPEG và PNG."
            });
        }

        // Đọc file để kiểm tra chữ ký byte.
        byte[] bytes;

        try
        {
            using var memory = new MemoryStream();

            await file.CopyToAsync(
                memory,
                cancellationToken);

            bytes = memory.ToArray();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Không thể đọc file upload");

            return BadRequest(new
            {
                message = "Không thể đọc file upload."
            });
        }

        if (bytes.LongLength != file.Length ||
            bytes.LongLength <= 0 ||
            bytes.LongLength > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "Kích thước file không hợp lệ."
            });
        }

        bool isPdf =
            bytes.Length >= 5 &&
            bytes[0] == 0x25 &&
            bytes[1] == 0x50 &&
            bytes[2] == 0x44 &&
            bytes[3] == 0x46 &&
            bytes[4] == 0x2D;

        bool isJpg =
            bytes.Length >= 3 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xD8 &&
            bytes[2] == 0xFF;

        bool isPng =
            bytes.Length >= 8 &&
            bytes[0] == 0x89 &&
            bytes[1] == 0x50 &&
            bytes[2] == 0x4E &&
            bytes[3] == 0x47 &&
            bytes[4] == 0x0D &&
            bytes[5] == 0x0A &&
            bytes[6] == 0x1A &&
            bytes[7] == 0x0A;

        string? mime = extension switch
        {
            ".pdf" when isPdf => "application/pdf",
            ".jpg" or ".jpeg" when isJpg => "image/jpeg",
            ".png" when isPng => "image/png",
            _ => null
        };

        if (mime is null)
        {
            return BadRequest(new
            {
                message =
                    "Nội dung file không khớp định dạng."
            });
        }

        // Tính cả file đã upload trước đó.
        int existingCount = await _db.Attachments
            .CountAsync(
                a => a.ReflectionId == reflectionId,
                cancellationToken);

        if (existingCount >= MaxFilesPerReflection)
        {
            return BadRequest(new
            {
                message =
                    "Mỗi phản ánh chỉ được đính kèm tối đa 5 file."
            });
        }

        string objectKey =
            $"reflections/{reflectionId}/{Guid.NewGuid():N}";

        bool uploadAttempted = false;

        try
        {
            using var stream = new MemoryStream(
                bytes,
                writable: false);

            // Ghi nhận trước khi upload để dọn dẹp
            // nếu MinIO upload bị lỗi giữa chừng.
            uploadAttempted = true;

            await _storage.UploadAsync(
                objectKey,
                stream,
                bytes.LongLength,
                mime,
                cancellationToken);

            var attachment = new Attachment
            {
                ReflectionId = reflectionId,
                FileName = fileName,
                ObjectKey = objectKey,
                ContentType = mime,
                FileSize = bytes.LongLength,
                UploadedAt = DateTime.UtcNow
            };

            _db.Attachments.Add(attachment);

            await _db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = attachment.Id },
                new
                {
                    attachment.Id,
                    attachment.ReflectionId,
                    attachment.FileName,
                    attachment.ContentType,
                    attachment.FileSize,
                    attachment.UploadedAt
                });
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
                "Upload Attachment thất bại. ReflectionId={ReflectionId}",
                reflectionId);

            if (uploadAttempted)
            {
                try
                {
                    string bucket =
                        _configuration["Minio:BucketName"]
                        ?? "student-feedback-attachments";

                    await _minio.RemoveObjectAsync(
                        new RemoveObjectArgs()
                            .WithBucket(bucket)
                            .WithObject(objectKey),
                        CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(
                        cleanupEx,
                        "Không thể dọn dẹp MinIO object {ObjectKey}",
                        objectKey);
                }
            }

            return StatusCode(502, new
            {
                message =
                    "Không thể lưu file đính kèm. Vui lòng thử lại."
            });
        }
    }
}
