
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
[Route("api/responses")]
public sealed class ResponsesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResponsesController(ApplicationDbContext db)
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

    // GET: /api/responses
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? reflectionId)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        if (reflectionId.HasValue && reflectionId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "ReflectionId phải lớn hơn 0."
            });
        }

        var query = _db.Responses
            .AsNoTracking()
            .AsQueryable();

        // Student chỉ được xem phản hồi thuộc phản ánh của mình.
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

        return item is null ? NotFound() : Ok(item);
    }

    // POST: /api/responses
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromBody] ResponseCreateRequest request)
    {
        int? currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized();

        bool isAdmin = await _db.Users.AnyAsync(u =>
            u.Id == currentUserId.Value &&
            u.Role == "Admin");

        if (!isAdmin)
            return Forbid();

        bool reflectionExists = await _db.Reflections
            .AnyAsync(r => r.Id == request.ReflectionId);

        if (!reflectionExists)
        {
            return BadRequest(new
            {
                message = "ReflectionId không tồn tại."
            });
        }

        string content = request.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new
            {
                message = "Nội dung phản hồi không được để trống."
            });
        }

        var item = new Response
        {
            Content = content,
            ReflectionId = request.ReflectionId,

            // Không dùng request.UserId.
            UserId = currentUserId.Value,

            CreatedAt = DateTime.UtcNow
        };

        _db.Responses.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            new
            {
                item.Id,
                item.Content,
                item.ReflectionId,
                item.UserId,
                item.CreatedAt
            });
    }

    // PUT: /api/responses/5
    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int id)
    {
        return StatusCode(
            StatusCodes.Status405MethodNotAllowed,
            new
            {
                message =
                    "Chức năng sửa phản hồi hiện chưa được hỗ trợ."
            });
    }

    // DELETE: /api/responses/5
    [HttpDelete("{id:int}")]
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
