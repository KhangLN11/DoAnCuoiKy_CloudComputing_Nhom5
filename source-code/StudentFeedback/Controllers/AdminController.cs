
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;
using StudentFeedback.Models;
using StudentFeedback.Models.ViewModels;
using System.Security.Claims;
using StudentFeedback.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;


namespace StudentFeedback.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
private readonly ILogger<AdminController> _logger;

public AdminController(
    ApplicationDbContext db,
    ILogger<AdminController> logger)
{
    _db = db;
    _logger = logger;
}

    
    // GET: /Admin/Index
    // Dashboard sử dụng dữ liệu thật từ MariaDB
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var reflections = _db.Reflections.AsNoTracking();

        // 1. Thống kê tổng quan
        var model = new AdminDashboardViewModel
        {
            TotalReflections = await reflections.CountAsync(),

            PendingCount = await reflections.CountAsync(
                r => r.Status == "Pending"),

            InProgressCount = await reflections.CountAsync(
                r => r.Status == "InProgress"),

            ResolvedCount = await reflections.CountAsync(
                r => r.Status == "Resolved"),

            ClosedCount = await reflections.CountAsync(
                r => r.Status == "Closed")
        };

        // 2. Thống kê số phản ánh theo danh mục
        model.CategoryStatistics = await _db.Categories
            .AsNoTracking()
            .Select(c => new AdminDashboardCategoryItemViewModel
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                ReflectionCount = _db.Reflections.Count(
                    r => r.CategoryId == c.Id)
            })
            .OrderByDescending(c => c.ReflectionCount)
            .ThenBy(c => c.CategoryName)
            .ToListAsync();

        // 3. Lấy 10 phản ánh mới nhất
        model.RecentReflections = await _db.Reflections
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(10)
            .Select(r => new AdminDashboardRecentReflectionViewModel
            {
                Id = r.Id,
                StudentName = r.User != null
                    ? r.User.FullName
                    : "Không xác định",

                Title = r.Title,

                CategoryName = r.Category != null
                    ? r.Category.Name
                    : "Không xác định",

                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return View(model);
    }

    
[HttpGet]
public async Task<IActionResult> Reflections(
    string? search,
    string? status,
    int? categoryId)
{
    var query = _db.Reflections
        .AsNoTracking()
        .Include(r => r.User)
        .Include(r => r.Category)
        .AsQueryable();

    // Tìm kiếm theo tiêu đề hoặc nội dung
    if (!string.IsNullOrWhiteSpace(search))
    {
        string keyword = search.Trim();

        query = query.Where(r =>
            r.Title.Contains(keyword) ||
            r.Content.Contains(keyword));
    }

    // Lọc theo trạng thái
    string[] validStatuses =
    {
        "Pending",
        "InProgress",
        "Resolved",
        "Closed"
    };

    if (!string.IsNullOrWhiteSpace(status))
    {
        if (!validStatuses.Contains(status))
        {
            return BadRequest("Trạng thái không hợp lệ.");
        }

        query = query.Where(r => r.Status == status);
    }

    // Lọc theo danh mục
    if (categoryId.HasValue)
    {
        if (categoryId.Value <= 0)
        {
            return BadRequest("Danh mục không hợp lệ.");
        }

        query = query.Where(r =>
            r.CategoryId == categoryId.Value);
    }

    var model = new AdminReflectionListViewModel
    {
        Reflections = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .ToListAsync(),

        Categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(),

        Search = search,
        Status = status,
        CategoryId = categoryId,

        // Thống kê toàn hệ thống, không phụ thuộc bộ lọc
        TotalCount = await _db.Reflections.CountAsync(),

        PendingCount = await _db.Reflections
            .CountAsync(r => r.Status == "Pending"),

        InProgressCount = await _db.Reflections
            .CountAsync(r => r.Status == "InProgress"),

        ResolvedCount = await _db.Reflections
            .CountAsync(r => r.Status == "Resolved"),

        ClosedCount = await _db.Reflections
            .CountAsync(r => r.Status == "Closed")
    };

    return View(model);
}


    // GET: /Admin/Categories
    // Quản lý danh mục với dữ liệu thật từ MariaDB
    [HttpGet]
    public async Task<IActionResult> Categories(string? search)
    {
        var query = _db.Categories.AsNoTracking();

        // Tìm kiếm theo tên hoặc mô tả danh mục.
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(c =>
                c.Name.Contains(keyword) ||
                (c.Description != null &&
                 c.Description.Contains(keyword)));
        }

        // Đọc danh mục từ MariaDB.
        var categories = await query
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Giữ lại từ khóa để hiển thị trên ô tìm kiếm.
        ViewBag.Search = search;

        return View(categories);
    }

    
    // GET: /Admin/Students
    // Danh sách sinh viên và thống kê phản ánh.
    [HttpGet]
    public async Task<IActionResult> Students(string? search)
    {
        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.Role == "Student");

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(u =>
                u.FullName.Contains(keyword) ||
                u.Email.Contains(keyword) ||
                (u.StudentCode != null &&
                 u.StudentCode.Contains(keyword)));
        }

        var students = await query
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Select(u => new AdminStudentItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                StudentCode = u.StudentCode,
                ClassName = u.ClassName,

                ReflectionCount = _db.Reflections
                    .Count(r => r.UserId == u.Id)
            })
            .ToListAsync();

        var model = new AdminStudentListViewModel
        {
            Students = students,
            Search = search,

            // Thống kê toàn hệ thống, không phụ thuộc tìm kiếm.
            TotalStudents = await _db.Users
                .CountAsync(u => u.Role == "Student"),

            TotalReflections = await _db.Reflections
                .CountAsync(r => r.User != null &&
                                 r.User.Role == "Student")
        };

        return View(model);
    }

    
    // GET: /Admin/StudentDetails/5
    [HttpGet]
    public async Task<IActionResult> StudentDetails(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        // Chỉ lấy tài khoản Student.
        // Không truy vấn hoặc hiển thị PasswordHash.
        var student = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id && u.Role == "Student")
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.StudentCode,
                u.ClassName
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return NotFound();
        }

        // Lịch sử phản ánh của sinh viên.
        var reflections = await _db.Reflections
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.UserId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .ToListAsync();

        var model = new AdminStudentDetailViewModel
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email,
            StudentCode = student.StudentCode,
            ClassName = student.ClassName,

            Reflections = reflections,

            TotalReflections = reflections.Count,

            PendingCount = reflections.Count(r =>
                r.Status == "Pending"),

            InProgressCount = reflections.Count(r =>
                r.Status == "InProgress"),

            ResolvedCount = reflections.Count(r =>
                r.Status == "Resolved"),

            ClosedCount = reflections.Count(r =>
                r.Status == "Closed")
        };

        return View(model);
    }



    

    // GET: /Admin/Accounts
    [HttpGet]
    public async Task<IActionResult> Accounts(
        string? search,
        string? roleFilter)
    {
        var query = _db.Users.AsNoTracking();

        // Tìm theo họ tên, email hoặc mã sinh viên
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(u =>
                u.FullName.Contains(keyword) ||
                u.Email.Contains(keyword) ||
                (u.StudentCode != null &&
                 u.StudentCode.Contains(keyword)));
        }

        // Chỉ chấp nhận các giá trị lọc hợp lệ
        if (roleFilter == "Student" || roleFilter == "Admin")
        {
            query = query.Where(u => u.Role == roleFilter);
        }
        else
        {
            roleFilter = null;
        }

        var accounts = await query
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Select(u => new AdminAccountItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                StudentCode = u.StudentCode,
                ClassName = u.ClassName,

                // Lấy trạng thái kích hoạt thực tế từ database
                IsActive = u.IsActive
            })
            .ToListAsync();

        var model = new AdminAccountListViewModel
        {
            Accounts = accounts,
            Search = search,
            RoleFilter = roleFilter,

            TotalAccounts = await _db.Users.CountAsync(),

            TotalStudents = await _db.Users
                .CountAsync(u => u.Role == "Student"),

            TotalAdmins = await _db.Users
                .CountAsync(u => u.Role == "Admin")
        };

        return View(model);
    }

    
    // POST: /Admin/ActivateAllStudents
    
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ActivateAllStudents()
{
    var adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(adminIdText, out int adminId) || adminId <= 0)
    {
        return Unauthorized();
    }

    bool isActiveAdmin = await _db.Users.AsNoTracking().AnyAsync(u =>
        u.Id == adminId && u.Role == "Admin" && u.IsActive);
    if (!isActiveAdmin)
    {
        return Forbid();
    }

    int activatedCount = await _db.Users
        .Where(u => u.Role == "Student" && !u.IsActive)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(u => u.IsActive, true));

    TempData["SuccessMessage"] = activatedCount == 0
        ? "Không có tài khoản sinh viên nào cần kích hoạt."
        : $"Đã kích hoạt thành công {activatedCount} tài khoản sinh viên.";

    return RedirectToAction(nameof(Accounts));
}



    
    // GET: /Admin/CategoryCreate
    [HttpGet]
    public IActionResult CategoryCreate()
    {
        return View(new AdminCategoryViewModel());
    }

    // POST: /Admin/CategoryCreate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryCreate(
        AdminCategoryViewModel model)
    {
        // Loại bỏ khoảng trắng thừa
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim();

        // Kiểm tra tên sau khi Trim
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Tên danh mục không được để trống.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Không cho phép tên danh mục trùng nhau
        bool exists = await _db.Categories
            .AnyAsync(c => c.Name == model.Name);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Tên danh mục này đã tồn tại.");

            return View(model);
        }

        var category = new Category
        {
            Name = model.Name,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Thêm danh mục thành công!";

        return RedirectToAction(nameof(Categories));
    }

    
    // GET: /Admin/CategoryEdit/5
    [HttpGet]
    public async Task<IActionResult> CategoryEdit(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        var model = new AdminCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return View(model);
    }

    // POST: /Admin/CategoryEdit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryEdit(
        int id,
        AdminCategoryViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Tên danh mục không được để trống.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        // Kiểm tra trùng tên, bỏ qua danh mục đang sửa
        bool exists = await _db.Categories.AnyAsync(c =>
            c.Id != id && c.Name == model.Name);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Tên danh mục này đã tồn tại.");

            return View(model);
        }

        category.Name = model.Name;
        category.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Cập nhật danh mục thành công!";

        return RedirectToAction(nameof(Categories));
    }

    
    // GET: /Admin/CategoryDelete/5
    // Chỉ hiển thị trang xác nhận, không xóa dữ liệu.
    [HttpGet]
    public async Task<IActionResult> CategoryDelete(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // POST: /Admin/CategoryDelete/5
    // Thực hiện xóa sau khi Admin xác nhận.
    [HttpPost]
    [ActionName("CategoryDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryDeleteConfirmed(int id)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        // Kiểm tra danh mục có đang được phản ánh sử dụng không.
        bool isUsed = await _db.Reflections
            .AnyAsync(r => r.CategoryId == id);

        if (isUsed)
        {
            TempData["ErrorMessage"] =
                "Không thể xóa danh mục này vì đã có phản ánh sử dụng.";

            return RedirectToAction(nameof(Categories));
        }

        _db.Categories.Remove(category);

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Xóa danh mục thành công!";

        return RedirectToAction(nameof(Categories));
    }

    
// GET: /Admin/ReflectionDetails/5
[HttpGet]
public async Task<IActionResult> ReflectionDetails(int id)
{
    if (id <= 0)
    {
        return NotFound();
    }

    var reflection = await _db.Reflections
        .AsNoTracking()
        .Include(r => r.User)
        .Include(r => r.Category)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reflection == null)
    {
        return NotFound();
    }

    var responses = await _db.Responses
        .AsNoTracking()
        .Include(r => r.User)
        .Where(r => r.ReflectionId == id)
        .OrderBy(r => r.CreatedAt)
        .ThenBy(r => r.Id)
        .ToListAsync();

    var attachments = await _db.Attachments
        .AsNoTracking()
        .Where(a => a.ReflectionId == id)
        .OrderBy(a => a.UploadedAt)
        .ToListAsync();

    var model = new AdminReflectionDetailViewModel
    {
        Reflection = reflection,
        Responses = responses,
        Attachments = attachments
    };

    return View(model);
}



    // POST: /Admin/ReflectionUpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReflectionUpdateStatus(
        AdminReflectionStatusViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "Trạng thái được chọn không hợp lệ.";

            return RedirectToAction(
                nameof(ReflectionDetails),
                new { id = model.ReflectionId });
        }

        var reflection = await _db.Reflections
            .FirstOrDefaultAsync(r => r.Id == model.ReflectionId);

        if (reflection == null)
        {
            return NotFound();
        }

        // Không tạo thông báo khi trạng thái không thay đổi.
        if (reflection.Status == model.Status)
        {
            TempData["SuccessMessage"] =
                "Trạng thái phản ánh không thay đổi.";

            return RedirectToAction(
                nameof(ReflectionDetails),
                new { id = reflection.Id });
        }

        var statusText = model.Status switch
        {
            "Pending" => "Mới tạo",
            "InProgress" => "Đang xử lý",
            "Resolved" => "Đã xử lý",
            "Closed" => "Đã đóng",
            _ => model.Status
        };

        var now = DateTime.UtcNow;

        // Cập nhật trạng thái phản ánh.
        reflection.Status = model.Status;
        reflection.UpdatedAt = now;

        // Tạo thông báo cho sinh viên sở hữu phản ánh.
        var notification = new Notification
        {
            UserId = reflection.UserId,
            ReflectionId = reflection.Id,
            Title = "Trạng thái phản ánh đã được cập nhật",
            Content =
                $"Phản ánh PF{reflection.Id:D4} " +
                $"đã được cập nhật sang trạng thái " +
                $"\"{statusText}\".",
            Type = "StatusChanged",
            IsRead = false,
            CreatedAt = now
        };

        _db.Notifications.Add(notification);

        // Lưu thay đổi trạng thái và thông báo cùng lúc.
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Cập nhật trạng thái phản ánh thành công!";

        return RedirectToAction(
            nameof(ReflectionDetails),
            new { id = reflection.Id });
    }




    // POST: /Admin/ReflectionReply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReflectionReply(
        AdminResponseCreateViewModel model)
    {
        if (model.ReflectionId <= 0)
        {
            return BadRequest();
        }

        model.Content =
            model.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.Content))
        {
            ModelState.AddModelError(
                nameof(model.Content),
                "Vui lòng nhập nội dung phản hồi.");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "Nội dung phản hồi không hợp lệ. " +
                "Vui lòng nhập từ 1 đến 4000 ký tự.";

            return RedirectToAction(
                nameof(ReflectionDetails),
                new { id = model.ReflectionId });
        }

        // Lấy ID Admin từ cookie xác thực trên server.
        string? userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int adminId)
            || adminId <= 0)
        {
            return Unauthorized();
        }

        // Kiểm tra tài khoản hiện tại vẫn là Admin.
        bool isAdmin = await _db.Users.AnyAsync(u =>
            u.Id == adminId &&
            u.Role == "Admin" &&
            u.IsActive);

        if (!isAdmin)
        {
            return Forbid();
        }

        // Lấy phản ánh để xác định sinh viên nhận thông báo.
        var reflection = await _db.Reflections
            .FirstOrDefaultAsync(r =>
                r.Id == model.ReflectionId);

        if (reflection == null)
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;

        // Lưu phản hồi của Admin.
        var response = new Response
        {
            ReflectionId = reflection.Id,
            UserId = adminId,
            Content = model.Content,
            CreatedAt = now
        };

        _db.Responses.Add(response);

        // Tạo thông báo cho sinh viên sở hữu phản ánh.
        var notification = new Notification
        {
            UserId = reflection.UserId,
            ReflectionId = reflection.Id,
            Title = "Bạn có phản hồi mới từ Admin",
            Content =
                $"Phản ánh PF{reflection.Id:D4} " +
                "đã nhận được phản hồi mới từ Admin. " +
                "Vui lòng mở chi tiết phản ánh để xem nội dung.",
            Type = "AdminReplied",
            IsRead = false,
            CreatedAt = now
        };

        _db.Notifications.Add(notification);

        // Lưu phản hồi và thông báo cùng một lần.
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Gửi phản hồi cho sinh viên thành công!";

        return RedirectToAction(
            nameof(ReflectionDetails),
            new { id = reflection.Id });
    }



    // GET: /Admin/ImportStudents
    [HttpGet]
    public IActionResult ImportStudents()
    {
        return View(new AdminStudentCsvPreviewViewModel());
    }



    

[HttpPost]
[ValidateAntiForgeryToken]
[RequestSizeLimit(3 * 1024 * 1024)]
public async Task<IActionResult> ImportStudents(
    IFormFile? csvFile,
    [FromServices] StudentCsvPreviewService csvService,
    [FromServices] StudentCsvTemporaryStorageService temporaryStorage)
{
    var model = new AdminStudentCsvPreviewViewModel();

    // 1. Kiểm tra file được gửi lên.
    if (csvFile == null || csvFile.Length == 0)
    {
        model.FileErrors.Add("Vui lòng chọn file CSV không rỗng.");
        return View(model);
    }

    // 2. Giới hạn dung lượng.
    if (csvFile.Length > StudentCsvPreviewService.MaxFileSize)
    {
        model.FileErrors.Add(
            "File CSV không được vượt quá 2 MB.");
        return View(model);
    }

    // 3. Kiểm tra phần mở rộng.
    if (!string.Equals(
            Path.GetExtension(csvFile.FileName),
            ".csv",
            StringComparison.OrdinalIgnoreCase))
    {
        model.FileErrors.Add(
            "Chỉ chấp nhận file có phần mở rộng .csv.");
        return View(model);
    }

    // 4. Đọc CSV và tạo bản xem trước.
    try
    {
        await using var stream = csvFile.OpenReadStream();

        model = csvService.Preview(
            stream,
            Path.GetFileName(csvFile.FileName));
    }
    catch (IOException)
    {
        model.FileErrors.Add(
            "Không thể đọc file CSV. Vui lòng thử lại.");
        return View(model);
    }
    catch (InvalidDataException)
    {
        model.FileErrors.Add(
            "Dữ liệu file tải lên không hợp lệ.");
        return View(model);
    }

    // 5. Không đối chiếu database nếu file có lỗi.
    if (model.FileErrors.Count > 0)
    {
        return View(model);
    }

    // 6. Không đối chiếu nếu CSV không có dòng dữ liệu.
    if (model.Rows.Count == 0)
    {
        model.FileErrors.Add(
            "File CSV không có dữ liệu sinh viên.");
        return View(model);
    }

    // 7. Đọc email và mã sinh viên hiện có.
    // Không truy vấn PasswordHash.
    var existingUsers = await _db.Users
        .AsNoTracking()
        .Select(u => new
        {
            u.Email,
            u.StudentCode
        })
        .ToListAsync();

    var existingEmails = new HashSet<string>(
        existingUsers.Select(u => u.Email.Trim()),
        StringComparer.OrdinalIgnoreCase);

    var existingStudentCodes = new HashSet<string>(
        existingUsers
            .Where(u => !string.IsNullOrWhiteSpace(u.StudentCode))
            .Select(u => u.StudentCode!.Trim()),
        StringComparer.OrdinalIgnoreCase);

    // 8. Kiểm tra dữ liệu trùng trong MariaDB.
    foreach (var row in model.Rows)
    {
        if (!string.IsNullOrWhiteSpace(row.Email) &&
            existingEmails.Contains(row.Email))
        {
            row.Errors.Add(
                "Email đã tồn tại trong hệ thống.");
        }

        if (!string.IsNullOrWhiteSpace(row.StudentCode) &&
            existingStudentCodes.Contains(row.StudentCode))
        {
            row.Errors.Add(
                "Mã sinh viên đã tồn tại trong hệ thống.");
        }
    }

    // 9. Cập nhật thống kê sau khi đối chiếu.
    model.TotalRows = model.Rows.Count;
    model.ValidRows = model.Rows.Count(r => r.IsValid);
    model.InvalidRows = model.Rows.Count(r => !r.IsValid);

    // 10. Chỉ lưu tạm nếu tất cả dòng đều hợp lệ.
    if (model.TotalRows > 0 && model.InvalidRows == 0)
    {
        // Lấy ID Admin đang đăng nhập.
        var adminIdText = User.FindFirst(
            ClaimTypes.NameIdentifier
        )?.Value;

        if (!int.TryParse(adminIdText, out var adminId) ||
            adminId <= 0)
        {
            model.FileErrors.Add(
                "Không xác định được tài khoản quản trị viên.");

            return View(model);
        }

        // Kiểm tra tài khoản vẫn là Admin đang hoạt động.
        var isActiveAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == adminId &&
                u.Role == "Admin" &&
                u.IsActive);

        if (!isActiveAdmin)
        {
            return Forbid();
        }

        try
        {
            // Mở lại file gốc để lưu tạm.
            await using var sourceStream =
                csvFile.OpenReadStream();

            model.ImportToken =
                await temporaryStorage.SaveAsync(
                    sourceStream,
                    adminId);
        }
        catch (IOException)
        {
            model.FileErrors.Add(
                "Không thể lưu tạm file CSV. Vui lòng thử lại.");
        }
        catch (UnauthorizedAccessException)
        {
            model.FileErrors.Add(
                "Hệ thống không có quyền lưu file CSV.");
        }

        catch (InvalidDataException)
{
    model.FileErrors.Add(
        "File CSV vượt quá dung lượng cho phép.");
}
    }

    // 11. Hiển thị kết quả xem trước.
    // Chưa ghi dữ liệu vào database.
    return View(model);
}


    // POST: /Admin/ConfirmImportStudents
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmImportStudents(
        string? importToken,
        [FromServices] StudentCsvPreviewService csvService,
        [FromServices] StudentCsvTemporaryStorageService temporaryStorage)
    {
        if (string.IsNullOrWhiteSpace(importToken))
        {
            TempData["ErrorMessage"] =
                "Không tìm thấy mã xác nhận nhập sinh viên.";

            return RedirectToAction(nameof(ImportStudents));
        }

        // Xác định Admin từ cookie đăng nhập.
        var adminIdText =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(adminIdText, out int adminId) ||
            adminId <= 0)
        {
            return Unauthorized();
        }

        // Khóa toàn bộ quá trình xác nhận trong cùng ứng dụng.
        using var confirmationLock =
            await temporaryStorage.AcquireConfirmationLockAsync();

        // Kiểm tra lại quyền Admin và trạng thái kích hoạt.
        bool isActiveAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == adminId &&
                u.Role == "Admin" &&
                u.IsActive);

        if (!isActiveAdmin)
        {
            return Forbid();
        }

        // Đọc lại file CSV đã lưu tạm.
        await using var csvStream =
            await temporaryStorage.OpenReadAsync(
                importToken,
                adminId);

        if (csvStream == null)
        {
            TempData["ErrorMessage"] =
                "File CSV đã hết hạn, không tồn tại hoặc không thuộc tài khoản này. " +
                "Vui lòng tải lại file.";

            return RedirectToAction(nameof(ImportStudents));
        }

        AdminStudentCsvPreviewViewModel preview;

        try
        {
            preview = csvService.Preview(
                csvStream,
                "students.csv");
        }
        catch (IOException)
        {
            TempData["ErrorMessage"] =
                "Không thể đọc file CSV đã lưu tạm.";

            return RedirectToAction(nameof(ImportStudents));
        }

        // Nếu file không hợp lệ, không nhập bất kỳ tài khoản nào.
        if (preview.FileErrors.Count > 0 ||
            preview.Rows.Count == 0 ||
            preview.Rows.Any(r => !r.IsValid))
        {
            TempData["ErrorMessage"] =
                "File CSV không còn hợp lệ. Vui lòng tải lại và kiểm tra.";

            return RedirectToAction(nameof(ImportStudents));
        }

        // Đối chiếu database lần cuối.
        var existingUsers = await _db.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Email,
                u.StudentCode
            })
            .ToListAsync();

        var existingEmails = new HashSet<string>(
            existingUsers.Select(u => u.Email.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var existingStudentCodes = new HashSet<string>(
            existingUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.StudentCode))
                .Select(u => u.StudentCode!.Trim()),
            StringComparer.OrdinalIgnoreCase);

        bool hasDuplicate = preview.Rows.Any(row =>
            existingEmails.Contains(row.Email) ||
            existingStudentCodes.Contains(row.StudentCode));

        if (hasDuplicate)
        {
            TempData["ErrorMessage"] =
                "Một hoặc nhiều sinh viên đã tồn tại trong hệ thống. " +
                "Vui lòng tải lại CSV để xem chi tiết.";

            return RedirectToAction(nameof(ImportStudents));
        }

        // Tạo tài khoản chưa kích hoạt.
        var passwordHasher = new PasswordHasher<User>();

        var students = new List<User>();

        foreach (var row in preview.Rows)
        {
            var student = new User
            {
                FullName = row.FullName,
                Email = row.Email,
                StudentCode = row.StudentCode,
                ClassName = string.IsNullOrWhiteSpace(row.ClassName)
                    ? null
                    : row.ClassName,
                Role = "Student",
                IsActive = false
            };

            
            // Mật khẩu ban đầu cho tài khoản sinh viên demo:
            // Mã sinh viên + ký tự @.
            // Chỉ lưu mật khẩu đã băm vào database.
            var initialPassword = row.StudentCode + "@";

            student.PasswordHash =
                passwordHasher.HashPassword(
                    student,
                    initialPassword);


            students.Add(student);
        }

        try
        {
            // Đảm bảo nhập toàn bộ hoặc không nhập gì.
            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            _db.Users.AddRange(students);

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Lỗi database khi Admin {AdminId} nhập CSV.",
                adminId);

            TempData["ErrorMessage"] =
                "Không thể nhập sinh viên do dữ liệu trùng " +
                "hoặc vi phạm ràng buộc database. " +
                "Vui lòng kiểm tra danh sách tài khoản trước khi thử lại.";

            return RedirectToAction(nameof(ImportStudents));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Lỗi khi Admin {AdminId} nhập CSV.",
                adminId);

            TempData["ErrorMessage"] =
                "Không thể xác nhận kết quả nhập sinh viên. " +
                "Vui lòng kiểm tra danh sách tài khoản trước khi thử lại.";

            return RedirectToAction(nameof(ImportStudents));
        }

        // MariaDB đã commit thành công. Việc cập nhật file JSON
        // không thuộc transaction nên cần xử lý lỗi riêng.
        try
        {
            await temporaryStorage.MarkCompletedAsync(importToken, adminId);

            // Đóng luồng đọc trước khi xóa file CSV tạm.
            await csvStream.DisposeAsync();
            temporaryStorage.Delete(importToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex,
                "Đã nhập {Count} sinh viên nhưng chưa dọn được ImportToken của Admin {AdminId}.",
                students.Count, adminId);

            // Không báo nhập thất bại: transaction đã commit.
            // Unique index tiếp tục bảo vệ khi token cũ bị gửi lại.
        }

        TempData["SuccessMessage"] =
            $"Đã nhập thành công {students.Count} sinh viên. " +
            "Các tài khoản đang ở trạng thái chưa kích hoạt.";

        return RedirectToAction(nameof(Accounts));
    }



}
