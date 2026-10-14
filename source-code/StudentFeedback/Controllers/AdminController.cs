
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;

namespace StudentFeedback.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Admin/Index
    // Trang Dashboard
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/Reflections
    // Quản lý phản ánh - sẽ triển khai sau
    [HttpGet]
    public IActionResult Reflections()
    {
        return View();
    }

    // GET: /Admin/Categories
    // Quản lý danh mục với dữ liệu thật từ MariaDB
    [HttpGet]
    public async Task<IActionResult> Categories(string? search)
    {
        var query = _db.Categories.AsNoTracking();

        // Tìm kiếm theo tên hoặc mô tả danh mục.
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(c =>
                c.Name.Contains(keyword) ||
                (c.Description != null &&
                 c.Description.Contains(keyword)));
        }

        // Đọc danh mục từ MariaDB.
        var categories = await query
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Giữ lại từ khóa để hiển thị trên ô tìm kiếm.
        ViewBag.Search = search;

        return View(categories);
    }

    // GET: /Admin/Students
    // Quản lý sinh viên - sẽ triển khai sau
    [HttpGet]
    public IActionResult Students()
    {
        return View();
    }

    // GET: /Admin/Accounts
    // Quản lý tài khoản - sẽ triển khai sau
    [HttpGet]
    public IActionResult Accounts()
    {
        return View();
    }
}
