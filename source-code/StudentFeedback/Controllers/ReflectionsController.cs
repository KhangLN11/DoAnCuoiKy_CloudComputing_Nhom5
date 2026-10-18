
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers;

[ApiController]
[Authorize]
[Route("api/reflections")]
public sealed class ReflectionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReflectionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private int? GetCurrentUserId()
    {
        string? claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out int id) && id > 0
            ? id
            : null;
    }

    // GET: /api/reflections
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? categoryId)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        bool isAdmin = User.IsInRole("Admin");

        var query = _db.Reflections
            .AsNoTracking()
            .AsQueryable();

        // Student chỉ được xem phản ánh của mình.
        if (!isAdmin)
        {
            query = query.Where(x =>
                x.UserId == currentUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            string[] validStatuses =
            {
                "Pending",
                "InProgress",
                "Resolved",
                "Closed"
            };

            if (!validStatuses.Contains(status))
            {
                return BadRequest(new
                {
                    message = "Trạng thái không hợp lệ."
                });
            }

            query = query.Where(x => x.Status == status);
        }

        if (categoryId.HasValue)
        {
            if (categoryId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "CategoryId phải lớn hơn 0."
                });
            }

            query = query.Where(x =>
                x.CategoryId == categoryId.Value);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Content,
                x.UserId,
                x.CategoryId,
                x.Status,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: /api/reflections/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        var query = _db.Reflections
            .AsNoTracking()
            .Where(x => x.Id == id);

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(x =>
                x.UserId == currentUserId.Value);
        }

        var item = await query
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Content,
                x.UserId,
                x.CategoryId,
                x.Status,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    // POST: /api/reflections
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromBody] ReflectionCreateRequest request)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        // Không sử dụng request.UserId.
        // Người gửi luôn là Student đang đăng nhập.
        bool studentExists = await _db.Users.AnyAsync(u =>
            u.Id == currentUserId.Value &&
            u.Role == "Student");

        if (!studentExists)
            return Forbid();

        if (!await _db.Categories.AnyAsync(
            x => x.Id == request.CategoryId))
        {
            return BadRequest(new
            {
                message = "CategoryId không tồn tại."
            });
        }

        string title = request.Title?.Trim() ?? string.Empty;
        string content = request.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new
            {
                message = "Tiêu đề và nội dung không được để trống."
            });
        }

        var item = new Reflection
        {
            Title = title,
            Content = content,
            UserId = currentUserId.Value,
            CategoryId = request.CategoryId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.Reflections.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            new
            {
                item.Id,
                item.Title,
                item.Content,
                item.UserId,
                item.CategoryId,
                item.Status,
                item.CreatedAt,
                item.UpdatedAt
            });
    }

    // PUT: /api/reflections/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ReflectionUpdateRequest request)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        var item = await _db.Reflections
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == currentUserId.Value);

        if (item == null)
            return NotFound();

        // Chỉ cho sửa khi phản ánh còn chờ xử lý.
        if (item.Status != "Pending")
        {
            return Conflict(new
            {
                message =
                    "Chỉ có thể chỉnh sửa phản ánh đang chờ xử lý."
            });
        }

        if (!await _db.Categories.AnyAsync(
            x => x.Id == request.CategoryId))
        {
            return BadRequest(new
            {
                message = "CategoryId không tồn tại."
            });
        }

        string title = request.Title?.Trim() ?? string.Empty;
        string content = request.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new
            {
                message = "Tiêu đề và nội dung không được để trống."
            });
        }

        // Student không được thay đổi trạng thái.
        // Bỏ qua request.Status.
        item.Title = title;
        item.Content = content;
        item.CategoryId = request.CategoryId;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: /api/reflections/5
    // Tạm khóa cho đến khi có quy trình xóa rõ ràng.
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        return StatusCode(
            StatusCodes.Status405MethodNotAllowed,
            new
            {
                message =
                    "Chức năng xóa phản ánh hiện chưa được hỗ trợ."
            });
    }
}
