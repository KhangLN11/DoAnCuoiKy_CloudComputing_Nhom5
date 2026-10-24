using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Minio;
using Minio.DataModel.Args;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;
using StudentFeedback.Models;
using StudentFeedback.Models.ViewModels;
using StudentFeedback.Services;

namespace StudentFeedback.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly MinioStorageService _storage;
        private readonly IMinioClient _minio;
        private readonly ILogger<StudentController> _logger;
        private readonly string _bucket;

        public StudentController(ApplicationDbContext db, MinioStorageService storage,
            IMinioClient minio, IConfiguration config, ILogger<StudentController> logger)
        {
            _db = db;
            _storage = storage;
            _minio = minio;
            _logger = logger;
            _bucket = config["Minio:BucketName"] ?? "student-feedback-attachments";
        }

        private async Task<int?> GetActiveStudentIdAsync()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            var exists = await _db.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id == userId &&
                    u.Role == "Student" &&
                    u.IsActive);

            return exists ? userId : null;
        }

        // GET: /Student/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = await GetActiveStudentIdAsync();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var student = await _db.Users
                .AsNoTracking()
                .FirstAsync(u => u.Id == userId.Value);

            var reflections = _db.Reflections
                .AsNoTracking()
                .Where(r => r.UserId == userId.Value);

            var model = new StudentDashboardViewModel
            {
                FullName = student.FullName,
                Email = student.Email,
                StudentCode = student.StudentCode ?? "",
                ClassName = student.ClassName ?? "",

                TotalReflections =
                    await reflections.CountAsync(),

                PendingCount =
                    await reflections.CountAsync(
                        r => r.Status == "Pending"),

                InProgressCount =
                    await reflections.CountAsync(
                        r => r.Status == "InProgress"),

                ResolvedCount =
                    await reflections.CountAsync(
                        r => r.Status == "Resolved"),

                ClosedCount =
                    await reflections.CountAsync(
                        r => r.Status == "Closed")
            };

            return View(model);
        }

        // GET: /Student/List
        [HttpGet]
        public async Task<IActionResult> List(
            string? search,
            string? status,
            int? categoryId,
            int page = 1)
        {
            var userId = await GetActiveStudentIdAsync();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var normalizedSearch = search?.Trim() ?? "";

            var allowedStatuses = new[]
            {
                "Pending",
                "InProgress",
                "Resolved",
                "Closed"
            };

            var normalizedStatus =
                !string.IsNullOrWhiteSpace(status) &&
                allowedStatuses.Contains(status)
                    ? status
                    : "all";

            if (page < 1)
            {
                page = 1;
            }

            const int pageSize = 10;

            var query = _db.Reflections
                .AsNoTracking()
                .Where(r => r.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var keyword = normalizedSearch;

                if (keyword.StartsWith(
                    "PF",
                    StringComparison.OrdinalIgnoreCase))
                {
                    keyword = keyword.Substring(2);
                }

                if (int.TryParse(keyword, out var reflectionId))
                {
                    query = query.Where(r =>
                        r.Id == reflectionId ||
                        r.Title.Contains(normalizedSearch) ||
                        r.Content.Contains(normalizedSearch));
                }
                else
                {
                    query = query.Where(r =>
                        r.Title.Contains(normalizedSearch) ||
                        r.Content.Contains(normalizedSearch));
                }
            }

            if (normalizedStatus != "all")
            {
                query = query.Where(r =>
                    r.Status == normalizedStatus);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(r =>
                    r.CategoryId == categoryId.Value);
            }

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r =>
                    new StudentReflectionListItemViewModel
                    {
                        Id = r.Id,
                        Title = r.Title,
                        CategoryName = r.Category != null
                            ? r.Category.Name
                            : "Chưa phân loại",
                        Status = r.Status,
                        CreatedAt = r.CreatedAt
                    })
                .ToListAsync();

            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c =>
                    new StudentReflectionCategoryViewModel
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                .ToListAsync();

            var model = new StudentReflectionListViewModel
            {
                Search = normalizedSearch,
                Status = normalizedStatus,
                CategoryId = categoryId,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                Items = items,
                Categories = categories
            };

            return View(model);
        }

        // GET: /Student/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = await GetActiveStudentIdAsync();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var model = new StudentCreateReflectionViewModel();

            await LoadCategoriesAsync(model);

            return View(model);
        }

        // POST: /Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(60 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 60 * 1024 * 1024)]
        public async Task<IActionResult> Create(
            StudentCreateReflectionViewModel model,
            List<IFormFile>? attachments)
        {
            var userId = await GetActiveStudentIdAsync();
            if (!userId.HasValue) return Forbid();

            model.Title = model.Title?.Trim() ?? "";
            model.Content = model.Content?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(nameof(model.Title), "Vui lòng nhập tiêu đề phản ánh.");
            if (string.IsNullOrWhiteSpace(model.Content))
                ModelState.AddModelError(nameof(model.Content), "Vui lòng nhập nội dung phản ánh.");
            if (model.Title.Length > 200)
                ModelState.AddModelError(nameof(model.Title), "Tiêu đề không được vượt quá 200 ký tự.");
            if (model.Content.Length > 4000)
                ModelState.AddModelError(nameof(model.Content), "Nội dung không được vượt quá 4000 ký tự.");

            if (!await _db.Categories.AsNoTracking().AnyAsync(c => c.Id == model.CategoryId))
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không hợp lệ hoặc không tồn tại.");

            attachments ??= new List<IFormFile>();
            const long maxFileSize = 10 * 1024 * 1024;
            if (attachments.Count > 5)
                ModelState.AddModelError("", "Chỉ được đính kèm tối đa 5 tệp.");

            // Validate signatures, not just extensions or browser-supplied ContentType.
            var prepared = new List<(string Name, string Mime, byte[] Bytes)>();
            foreach (var file in attachments.Take(6))
            {
                var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
                if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || file.Length <= 0 || file.Length > maxFileSize)
                {
                    ModelState.AddModelError("", "Tên tệp không hợp lệ hoặc tệp vượt quá 10 MB.");
                    continue;
                }
                var ext = Path.GetExtension(name).ToLowerInvariant();
                if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    ModelState.AddModelError("", $"Tệp {name}: chỉ hỗ trợ PDF, JPG, JPEG và PNG.");
                    continue;
                }
                if (!ModelState.IsValid) continue;
                using var ms = new MemoryStream();
                try
                {
                    await file.CopyToAsync(ms);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed reading uploaded file");
                    ModelState.AddModelError("", $"Không thể đọc tệp {name}.");
                    continue;
                }
                var bytes = ms.ToArray();
                if (bytes.LongLength != file.Length || bytes.LongLength > maxFileSize)
                {
                    ModelState.AddModelError("", $"Tệp {name} không hợp lệ.");
                    continue;
                }
                var pdf = bytes.Length >= 5 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46 && bytes[4] == 0x2D;
                var jpg = bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
                var png = bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
                string? mime = ext switch
                {
                    ".pdf" when pdf => "application/pdf",
                    ".jpg" or ".jpeg" when jpg => "image/jpeg",
                    ".png" when png => "image/png",
                    _ => null
                };
                if (mime == null)
                {
                    ModelState.AddModelError("", $"Nội dung tệp {name} không khớp định dạng.");
                    continue;
                }
                prepared.Add((name, mime, bytes));
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model);
                return View(model);
            }

            var uploadedKeys = new List<string>();
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var reflection = new Reflection
                {
                    Title = model.Title,
                    Content = model.Content,
                    CategoryId = model.CategoryId,
                    UserId = userId.Value,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _db.Reflections.Add(reflection);
                await _db.SaveChangesAsync();

                if (prepared.Count > 0)
                    await _storage.EnsureBucketExistsAsync();

                foreach (var file in prepared)
                {
                    var key = $"reflections/{reflection.Id}/{Guid.NewGuid():N}";
                    // Record the key before attempting upload so cleanup also handles partial uploads.
                    uploadedKeys.Add(key);
                    using var stream = new MemoryStream(file.Bytes, writable: false);
                    await _storage.UploadAsync(key, stream, file.Bytes.LongLength, file.Mime);
                    _db.Attachments.Add(new Attachment
                    {
                        ReflectionId = reflection.Id,
                        FileName = file.Name,
                        ObjectKey = key,
                        ContentType = file.Mime,
                        FileSize = file.Bytes.LongLength,
                        UploadedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["StudentSuccessMessage"] =
                    $"Đã gửi phản ánh PF{reflection.Id:D4} thành công.";
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed creating reflection with MinIO attachments");
                await transaction.RollbackAsync();
                foreach (var key in uploadedKeys)
                {
                    try
                    {
                        await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                            .WithBucket(_bucket).WithObject(key));
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed cleaning up MinIO object {Key}", key);
                    }
                }
                ModelState.AddModelError("", "Không thể gửi phản ánh lúc này. Vui lòng thử lại.");
                await LoadCategoriesAsync(model);
                return View(model);
            }
        }

        private async Task LoadCategoriesAsync(
            StudentCreateReflectionViewModel model)
        {
            model.Categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new StudentCreateCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }

        // GET: /Student/Details?id=123
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = await GetActiveStudentIdAsync();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            if (id <= 0)
            {
                return NotFound();
            }

            // Bắt buộc kiểm tra quyền sở hữu ngay trong SQL.
            // Không tải phản ánh của sinh viên khác.
            var model = await _db.Reflections
                .AsNoTracking()
                .Where(r =>
                    r.Id == id &&
                    r.UserId == userId.Value)
                .Select(r => new StudentReflectionDetailsViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Content = r.Content,
                    CategoryName = r.Category != null
                        ? r.Category.Name
                        : "Chưa phân loại",
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            // Chỉ truy vấn phản hồi sau khi đã xác nhận
            // phản ánh thuộc sinh viên đang đăng nhập.
            model.Responses = await _db.Responses
                .AsNoTracking()
                .Where(r => r.ReflectionId == model.Id)
                .OrderBy(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .Select(r => new StudentReflectionResponseViewModel
                {
                    Id = r.Id,
                    Content = r.Content,
                    AdminName = r.User != null
                        ? r.User.FullName
                        : "Quản trị viên",
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return View(model);
        }
    }
}
