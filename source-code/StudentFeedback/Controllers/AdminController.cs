
using Microsoft.AspNetCore.Mvc;
using StudentFeedback.Models;

namespace StudentFeedback.Controllers
{
    public class AdminController : Controller
    {
        // Dữ liệu minh họa giao diện, chưa kết nối database.
       private static readonly List<AdminReflectionVM> ReflectionData = new()
        {
            new AdminReflectionVM
            {
                Id = 1,
                Code = "PF001",
                StudentName = "Nguyễn Văn A",
                Title = "Máy chiếu phòng A101 bị hỏng",
                Category = "Cơ sở vật chất",
                Content = "Máy chiếu phòng A101 không hoạt động.",
                Status = "Đang xử lý",
                CreatedAt = DateTime.Now.AddDays(-1)
            },
            new AdminReflectionVM
            {
                Id = 2,
                Code = "PF002",
                StudentName = "Trần Thị B",
                Title = "Wifi chập chờn tại thư viện",
                Category = "Dịch vụ sinh viên",
                Content = "Wifi tại thư viện thường xuyên mất kết nối.",
                Status = "Đã xử lý",
                CreatedAt = DateTime.Now.AddDays(-2),
                AdminResponse = "Bộ phận kỹ thuật đã kiểm tra."
            },
            new AdminReflectionVM
            {
                Id = 3,
                Code = "PF003",
                StudentName = "Lê Văn C",
                Title = "Quạt phòng B202 không hoạt động",
                Category = "Phòng học",
                Content = "Quạt trong phòng học cần được kiểm tra.",
                Status = "Mới tạo",
                CreatedAt = DateTime.Now
            },
            new AdminReflectionVM
            {
                Id = 4,
                Code = "PF004",
                StudentName = "Phạm Văn D",
                Title = "Lỗi đăng ký học phần",
                Category = "Học vụ",
                Content = "Sinh viên gặp lỗi khi đăng ký học phần.",
                Status = "Mới tạo",
                CreatedAt = DateTime.Now.AddDays(-3)
            },
            new AdminReflectionVM
            {
                Id = 5,
                Code = "PF005",
                StudentName = "Hoàng Văn E",
                Title = "Bàn ghế lớp học bị hỏng",
                Category = "Cơ sở vật chất",
                Content = "Một số bàn ghế trong lớp cần sửa chữa.",
                Status = "Đã xử lý",
                CreatedAt = DateTime.Now.AddDays(-4)
            }
        };

        private static readonly List<AdminCategoryVM> CategoriesData = new()
        {
            new AdminCategoryVM { Id = 1, Name = "Cơ sở vật chất", Description = "Thiết bị và cơ sở vật chất", ReflectionCount = 2 },
            new AdminCategoryVM { Id = 2, Name = "Phòng học", Description = "Các vấn đề trong phòng học", ReflectionCount = 1 },
            new AdminCategoryVM { Id = 3, Name = "Dịch vụ sinh viên", Description = "Các dịch vụ hỗ trợ sinh viên", ReflectionCount = 1 },
            new AdminCategoryVM { Id = 4, Name = "Học vụ", Description = "Đăng ký và thông tin học vụ", ReflectionCount = 1 }
        };

        private static readonly List<AdminUserVM> UsersData = new()
        {
            new AdminUserVM { Id = 1, StudentCode = "SV123456", FullName = "Nguyễn Văn A", Email = "nguyenvana@stu.edu.vn" },
            new AdminUserVM { Id = 2, StudentCode = "SV123457", FullName = "Trần Thị B", Email = "tranthib@stu.edu.vn" },
            new AdminUserVM { Id = 3, StudentCode = "SV123458", FullName = "Lê Văn C", Email = "levanc@stu.edu.vn" },
            new AdminUserVM { Id = 4, StudentCode = "AD000001", FullName = "Quản trị viên", Email = "admin@stu.edu.vn", Role = "Admin" }
        };

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Dashboard()
        {
            var vm = new AdminDashboardVM
            {
               Total = ReflectionData.Count,
NewCount = ReflectionData.Count(x => x.Status == "Mới tạo"),
ProcessingCount = ReflectionData.Count(x => x.Status == "Đang xử lý"),
ResolvedCount = ReflectionData.Count(x => x.Status == "Đã xử lý"),
RecentReflections = ReflectionData
    .OrderByDescending(x => x.CreatedAt)
    .Take(5)
    .ToList()
            };

            return View(vm);
        }

        public IActionResult Reflections(string? keyword, string? status)
        {
            IEnumerable<AdminReflectionVM> result = ReflectionData;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                result = result.Where(x =>
                    x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.StudentName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                result = result.Where(x => x.Status == status);
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;

            return View(result.OrderByDescending(x => x.CreatedAt).ToList());
        }

        public IActionResult Details(int id)
        {
          var item = ReflectionData.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        public IActionResult Categories()
        {
            return View(CategoriesData);
        }

        public IActionResult Users()
        {
            return View(UsersData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string status, string adminResponse)
        {
           var item = ReflectionData.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return NotFound();

            var allowed = new[] { "Mới tạo", "Đang xử lý", "Đã xử lý", "Đã đóng" };

            if (!allowed.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            item.Status = status;
            item.AdminResponse = adminResponse ?? "";

            TempData["Success"] = "Đã cập nhật thông tin xử lý.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
