
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers;

[ApiController]
[Authorize(Roles = "Student,Admin")]
[Route("api/responses")]
public sealed class ResponsesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResponsesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Lấy ID người đang đăng nhập từ cookie.
    private int? GetCurrentUserId()
    {
        string? claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out int id) && id > 0
            ? id
            : null;
    }

    // Xác nhận tài khoản vẫn hoạt động
    // và vai trò trong database khớp cookie.
    private async Task<bool> IsActiveUserAsync(int userId)
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
                ));
    }

    // GET: /api/responses
    // GET: /api/responses?reflectionId=16
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? reflectionId)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        if (!await IsActiveUserAsync(currentUserId.Value))
            return Forbid();

        if (reflectionId.HasValue &&
            reflectionId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "ReflectionId phải lớn hơn 0."
            });
        }

        var query = _db.Responses
            .AsNoTracking()
            .AsQueryable();

        // Student chỉ xem phản hồi thuộc phản ánh của mình.
        if (!User.IsInRole("Admin"))
        {
            query = query.Where(r =>
                _db.Reflections.Any(f =>
                    f.Id == r.ReflectionId &&
                    f.UserId == currentUserId.Value));
        }

        if (reflectionId.HasValue)
        {
            query = query.Where(r =>
                r.ReflectionId == reflectionId.Value);
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Content,
                r.ReflectionId,
                r.UserId,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: /api/responses/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        if (!await IsActiveUserAsync(currentUserId.Value))
            return Forbid();

        if (id <= 0)
            return NotFound();

        var query = _db.Responses
            .AsNoTracking()
            .Where(r => r.Id == id);

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(r =>
                _db.Reflections.Any(f =>
                    f.Id == r.ReflectionId &&
                    f.UserId == currentUserId.Value));
        }

        var item = await query
            .Select(r => new
            {
                r.Id,
                r.Content,
                r.ReflectionId,
                r.UserId,
                r.CreatedAt
            })
            .FirstOrDefaultAsync();

        return item is null
            ? NotFound()
            : Ok(item);
    }

    // POST: /api/responses
    // Chỉ Admin được tạo phản hồi.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromBody] ResponseCreateRequest request)
    {
        int? adminId = GetCurrentUserId();

        if (!adminId.HasValue)
            return Unauthorized();

        bool isActiveAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == adminId.Value &&
                u.Role == "Admin" &&
                u.IsActive);

        if (!isActiveAdmin)
            return Forbid();

        string content =
            request.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content) ||
            content.Length > 4000)
        {
            return BadRequest(new
            {
                message =
                    "Nội dung phản hồi phải có từ 1 đến 4000 ký tự."
            });
        }

        if (request.ReflectionId <= 0)
        {
            return BadRequest(new
            {
                message = "ReflectionId không hợp lệ."
            });
        }

        // Lấy Reflection để xác định Student nhận thông báo.
        var reflection = await _db.Reflections
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Id == request.ReflectionId);

        if (reflection is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy phản ánh."
            });
        }

        var now = DateTime.UtcNow;

        // Người viết phản hồi là Admin đang đăng nhập.
        // Không nhận UserId từ client.
        var response = new Response
        {
            Content = content,
            ReflectionId = reflection.Id,
            UserId = adminId.Value,
            CreatedAt = now
        };

        _db.Responses.Add(response);

        // Giữ nguyên nghiệp vụ Admin/ReflectionReply:
        // Student nhận thông báo khi Admin phản hồi.
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

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            new
            {
                response.Id,
                response.Content,
                response.ReflectionId,
                response.UserId,
                response.CreatedAt
            });
    }

    // PUT: /api/responses/5
    // Chỉ Admin tạo phản hồi mới được sửa nội dung.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ResponseUpdateRequest request)
    {
        int? adminId = GetCurrentUserId();

        if (!adminId.HasValue)
            return Unauthorized();

        bool isActiveAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == adminId.Value &&
                u.Role == "Admin" &&
                u.IsActive);

        if (!isActiveAdmin)
            return Forbid();

        if (id <= 0)
            return NotFound();

        var response = await _db.Responses
            .FirstOrDefaultAsync(r => r.Id == id);

        if (response is null)
            return NotFound();

        // Admin chỉ được sửa phản hồi do chính mình tạo.
        if (response.UserId != adminId.Value)
            return Forbid();

        string content =
            request.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content) ||
            content.Length > 4000)
        {
            return BadRequest(new
            {
                message =
                    "Nội dung phản hồi phải có từ 1 đến 4000 ký tự."
            });
        }

        response.Content = content;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: /api/responses/5
    // Chưa hỗ trợ xóa để bảo vệ lịch sử phản hồi.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        return StatusCode(
            StatusCodes.Status405MethodNotAllowed,
            new
            {
                message =
                    "Chức năng xóa phản hồi hiện chưa được hỗ trợ."
            });
    }
}
