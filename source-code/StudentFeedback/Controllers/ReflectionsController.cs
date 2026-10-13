using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers;

[ApiController]
[Route("api/reflections")]
public sealed class ReflectionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReflectionsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? categoryId)
    {
        var query = _db.Reflections.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!new[] { "Pending", "InProgress", "Resolved", "Closed" }.Contains(status))
                return BadRequest(new { message = "Trạng thái không hợp lệ." });
            query = query.Where(x => x.Status == status);
        }

        if (categoryId.HasValue)
        {
            if (categoryId.Value <= 0) return BadRequest(new { message = "CategoryId phải lớn hơn 0." });
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Title, x.Content, x.UserId, x.CategoryId, x.Status, x.CreatedAt, x.UpdatedAt })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Reflections.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Title, x.Content, x.UserId, x.CategoryId, x.Status, x.CreatedAt, x.UpdatedAt })
            .FirstOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReflectionCreateRequest request)
    {
        if (!await _db.Users.AnyAsync(x => x.Id == request.UserId))
            return BadRequest(new { message = "UserId không tồn tại." });
        if (!await _db.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return BadRequest(new { message = "CategoryId không tồn tại." });

        var item = new Reflection
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            UserId = request.UserId,
            CategoryId = request.CategoryId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _db.Reflections.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new { item.Id, item.Title, item.Content, item.UserId, item.CategoryId, item.Status, item.CreatedAt, item.UpdatedAt });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReflectionUpdateRequest request)
    {
        var item = await _db.Reflections.FindAsync(id);
        if (item is null) return NotFound();
        if (!await _db.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return BadRequest(new { message = "CategoryId không tồn tại." });

        item.Title = request.Title.Trim();
        item.Content = request.Content.Trim();
        item.CategoryId = request.CategoryId;
        item.Status = request.Status;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Reflections.FindAsync(id);
        if (item is null) return NotFound();
        if (await _db.Responses.AnyAsync(x => x.ReflectionId == id) ||
            await _db.Attachments.AnyAsync(x => x.ReflectionId == id))
            return Conflict(new { message = "Phản ánh có phản hồi hoặc tệp đính kèm, không thể xóa trực tiếp." });

        _db.Reflections.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
