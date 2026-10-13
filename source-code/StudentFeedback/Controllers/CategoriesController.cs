using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Categories.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Name, x.Description })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Categories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Name, x.Description })
            .FirstOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request)
    {
        var item = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };
        _db.Categories.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new { item.Id, item.Name, item.Description });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        var item = await _db.Categories.FindAsync(id);
        if (item is null) return NotFound();

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Categories.FindAsync(id);
        if (item is null) return NotFound();

        if (await _db.Reflections.AnyAsync(x => x.CategoryId == id))
            return Conflict(new { message = "Không thể xóa danh mục đang có phản ánh." });

        _db.Categories.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
