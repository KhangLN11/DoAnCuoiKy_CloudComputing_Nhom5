using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers;

[ApiController]
[Route("api/responses")]
public sealed class ResponsesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResponsesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? reflectionId)
    {
        var query = _db.Responses.AsNoTracking().AsQueryable();
        if (reflectionId.HasValue)
        {
            if (reflectionId.Value <= 0) return BadRequest(new { message = "ReflectionId phải lớn hơn 0." });
            query = query.Where(x => x.ReflectionId == reflectionId.Value);
        }
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Content, x.ReflectionId, x.UserId, x.CreatedAt })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Responses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Content, x.ReflectionId, x.UserId, x.CreatedAt })
            .FirstOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ResponseCreateRequest request)
    {
        if (!await _db.Reflections.AnyAsync(x => x.Id == request.ReflectionId))
            return BadRequest(new { message = "ReflectionId không tồn tại." });
        if (!await _db.Users.AnyAsync(x => x.Id == request.UserId))
            return BadRequest(new { message = "UserId không tồn tại." });

        var item = new Response
        {
            Content = request.Content.Trim(),
            ReflectionId = request.ReflectionId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Responses.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new { item.Id, item.Content, item.ReflectionId, item.UserId, item.CreatedAt });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ResponseUpdateRequest request)
    {
        var item = await _db.Responses.FindAsync(id);
        if (item is null) return NotFound();

        item.Content = request.Content.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Responses.FindAsync(id);
        if (item is null) return NotFound();

        _db.Responses.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
