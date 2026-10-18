
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/categories
    // Student và Admin đã đăng nhập đều được xem danh mục.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/categories/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Categories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description
            })
            .FirstOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    // POST: api/categories
    // Chỉ Admin được thêm danh mục.
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CategoryRequest request)
    {
        var item = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        _db.Categories.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            new
            {
                item.Id,
                item.Name,
                item.Description
            });
    }

    // PUT: api/categories/1
    // Chỉ Admin được sửa danh mục.
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CategoryRequest request)
    {
        var item = await _db.Categories.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim();

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/categories/1
    // Chỉ Admin được xóa danh mục.
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Categories.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        // Không xóa danh mục đang được phản ánh sử dụng.
        bool isUsed = await _db.Reflections
            .AnyAsync(x => x.CategoryId == id);

        if (isUsed)
        {
            return Conflict(new
            {
                message = "Không thể xóa danh mục đang có phản ánh."
            });
        }

        _db.Categories.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
