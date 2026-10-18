
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;
using StudentFeedback.Models.ViewModels;

namespace StudentFeedback.Controllers;

[Authorize(Roles = "Student")]
[Route("Student/Notifications")]
public class StudentNotificationsController : Controller
{
    private readonly ApplicationDbContext _db;

    public StudentNotificationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private async Task<int?> GetActiveStudentIdAsync()
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out var userId))
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

    // GET: /Student/Notifications
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var studentId = await GetActiveStudentIdAsync();

        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == studentId.Value);

        var model = new StudentNotificationViewModel
        {
            TotalCount = await query.CountAsync(),

            UnreadCount = await query.CountAsync(
                n => !n.IsRead),

            Items = await query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Select(n => new StudentNotificationItemViewModel
                {
                    Id = n.Id,
                    ReflectionId = n.ReflectionId,
                    Title = n.Title,
                    Content = n.Content,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync()
        };

        return View(
            "~/Views/Student/Notifications.cshtml",
            model);
    }

    // GET: /Student/Notifications/UnreadCount
    [HttpGet("UnreadCount")]
    public async Task<IActionResult> UnreadCount()
    {
        var studentId = await GetActiveStudentIdAsync();

        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var count = await _db.Notifications
            .AsNoTracking()
            .CountAsync(n =>
                n.UserId == studentId.Value &&
                !n.IsRead);

        return Json(new { count });
    }

    // POST: /Student/Notifications/Open/5
    [HttpPost("Open/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id)
    {
        var studentId = await GetActiveStudentIdAsync();

        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == id &&
                n.UserId == studentId.Value);

        if (notification == null)
        {
            return NotFound();
        }

        // Kiểm tra phản ánh thực sự thuộc sinh viên.
        var ownsReflection = await _db.Reflections
            .AsNoTracking()
            .AnyAsync(r =>
                r.Id == notification.ReflectionId &&
                r.UserId == studentId.Value);

        if (!ownsReflection)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        return RedirectToAction(
            "Details",
            "Student",
            new { id = notification.ReflectionId });
    }

    // POST: /Student/Notifications/MarkAllRead
    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var studentId = await GetActiveStudentIdAsync();

        if (!studentId.HasValue)
        {
            return Forbid();
        }

        var unread = await _db.Notifications
            .Where(n =>
                n.UserId == studentId.Value &&
                !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _db.SaveChangesAsync();

        TempData["NotificationSuccess"] =
            "Đã đánh dấu tất cả thông báo là đã đọc.";

        return RedirectToAction(nameof(Index));
    }
}
