
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;

namespace StudentFeedback.Controllers.Api;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private async Task<int?> GetActiveStudentIdAsync(
        CancellationToken cancellationToken)
    {
        string? claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out int userId) ||
            userId <= 0)
        {
            return null;
        }

        bool isActiveStudent = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == userId &&
                u.Role == "Student" &&
                u.IsActive,
                cancellationToken);

        return isActiveStudent ? userId : null;
    }

    // GET: /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        int? studentId = await GetActiveStudentIdAsync(
            cancellationToken);

        if (!studentId.HasValue)
            return Forbid();

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == studentId.Value);

        int totalCount = await query.CountAsync(
            cancellationToken);

        int unreadCount = await query.CountAsync(
            n => !n.IsRead,
            cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Select(n => new
            {
                n.Id,
                n.ReflectionId,
                n.Title,
                n.Content,
                n.Type,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            totalCount,
            unreadCount,
            items
        });
    }

    // GET: /api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        int? studentId = await GetActiveStudentIdAsync(
            cancellationToken);

        if (!studentId.HasValue)
            return Forbid();

        int count = await _db.Notifications
            .AsNoTracking()
            .CountAsync(n =>
                n.UserId == studentId.Value &&
                !n.IsRead,
                cancellationToken);

        return Ok(new { count });
    }

    // GET: /api/notifications/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        int? studentId = await GetActiveStudentIdAsync(
            cancellationToken);

        if (!studentId.HasValue)
            return Forbid();

        if (id <= 0)
            return NotFound();

        var notification = await _db.Notifications
            .AsNoTracking()
            .Where(n =>
                n.Id == id &&
                n.UserId == studentId.Value &&
                _db.Reflections.Any(r =>
                    r.Id == n.ReflectionId &&
                    r.UserId == studentId.Value))
            .Select(n => new
            {
                n.Id,
                n.ReflectionId,
                n.Title,
                n.Content,
                n.Type,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return notification is null
            ? NotFound()
            : Ok(notification);
    }

    // PUT: /api/notifications/5/read
    [HttpPut("{id:int}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(
        int id,
        CancellationToken cancellationToken)
    {
        int? studentId = await GetActiveStudentIdAsync(
            cancellationToken);

        if (!studentId.HasValue)
            return Forbid();

        if (id <= 0)
            return NotFound();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == id &&
                n.UserId == studentId.Value &&
                _db.Reflections.Any(r =>
                    r.Id == n.ReflectionId &&
                    r.UserId == studentId.Value),
                cancellationToken);

        if (notification is null)
            return NotFound();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(
                cancellationToken);
        }

        return NoContent();
    }

    // PUT: /api/notifications/read-all
    [HttpPut("read-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(
        CancellationToken cancellationToken)
    {
        int? studentId = await GetActiveStudentIdAsync(
            cancellationToken);

        if (!studentId.HasValue)
            return Forbid();

        var unread = await _db.Notifications
            .Where(n =>
                n.UserId == studentId.Value &&
                !n.IsRead)
            .ToListAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        if (unread.Count > 0)
        {
            await _db.SaveChangesAsync(
                cancellationToken);
        }

        return Ok(new
        {
            message = "Đã đánh dấu tất cả thông báo là đã đọc.",
            updatedCount = unread.Count
        });
    }
}
