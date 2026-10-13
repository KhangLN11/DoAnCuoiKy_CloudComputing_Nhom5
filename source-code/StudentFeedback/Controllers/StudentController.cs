
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentFeedback.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult List()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Details()
        {
            return View();
        }
    }
}
