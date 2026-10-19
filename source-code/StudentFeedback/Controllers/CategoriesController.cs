
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Contracts;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers;

[ApiController]
[Authorize(Roles = "Student,Admin")]
[Route("api/categories")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new CategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
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
            .Select(x => new CategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .FirstOrDefaultAsync();

        if (item is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy danh mục."
            });
        }

        return Ok(item);
    }

    // POST: api/categories
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CategoryRequest request)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Tên danh mục không được để trống."
            });
        }

        var normalizedName = name.ToLower();

        var isDuplicate = await _db.Categories
            .AnyAsync(x => x.Name.ToLower() == normalizedName);

        if (isDuplicate)
        {
            return Conflict(new
            {
                message = "Tên danh mục đã tồn tại."
            });
        }

        var item = new Category
        {
            Name = name,
            Description = request.Description?.Trim()
        };

        _db.Categories.Add(item);
        await _db.SaveChangesAsync();

        var response = new CategoryResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            response);
    }

    // PUT: api/categories/1
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CategoryRequest request)
    {
        var item = await _db.Categories.FindAsync(id);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy danh mục."
            });
        }

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Tên danh mục không được để trống."
            });
        }

        var normalizedName = name.ToLower();

        var isDuplicate = await _db.Categories
            .AnyAsync(x =>
                x.Id != id &&
                x.Name.ToLower() == normalizedName);

        if (isDuplicate)
        {
            return Conflict(new
            {
                message = "Tên danh mục đã tồn tại."
            });
        }

        item.Name = name;
        item.Description = request.Description?.Trim();

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/categories/1
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Categories.FindAsync(id);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy danh mục."
            });
        }

        var isUsed = await _db.Reflections
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

// DTO phản hồi API
public sealed class CategoryResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
