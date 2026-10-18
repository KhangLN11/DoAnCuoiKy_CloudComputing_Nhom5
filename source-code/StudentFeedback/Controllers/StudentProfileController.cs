
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;
using StudentFeedback.Models;
using StudentFeedback.Models.ViewModels;

namespace StudentFeedback.Controllers
{
    [Authorize(Roles = "Student")]
    [Route("Student/Profile")]
    public class StudentProfileController : Controller
    {
        private readonly ApplicationDbContext _db;

        private readonly PasswordHasher<User> _passwordHasher = new();

        public StudentProfileController(ApplicationDbContext db)
        {
            _db = db;
        }

        private async Task<User?> GetStudentAsync()
        {
            var idText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(idText, out var userId))
            {
                return null;
            }

            return await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.Role == "Student" &&
                u.IsActive);
        }

        private static StudentProfileViewModel BuildModel(User user)
        {
            return new StudentProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                StudentCode = user.StudentCode ?? "",
                ClassName = user.ClassName ?? "",
                IsActive = user.IsActive,
                EditFullName = user.FullName
            };
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var student = await GetStudentAsync();

            if (student == null)
            {
                return Forbid();
            }

            return View(
                "~/Views/Student/Profile.cshtml",
                BuildModel(student));
        }

        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            [FromForm] string? editFullName)
        {
            var student = await GetStudentAsync();

            if (student == null)
            {
                return Forbid();
            }

            var fullName = editFullName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError(
                    "EditFullName",
                    "Vui lòng nhập họ tên.");
            }
            else if (fullName.Length > 100)
            {
                ModelState.AddModelError(
                    "EditFullName",
                    "Họ tên không được vượt quá 100 ký tự.");
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = BuildModel(student);
                invalidModel.EditFullName = fullName;

                return View(
                    "~/Views/Student/Profile.cshtml",
                    invalidModel);
            }

            student.FullName = fullName;

            await _db.SaveChangesAsync();

            await RefreshSignInAsync(student);

            TempData["ProfileSuccess"] =
                "Cập nhật họ tên thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            [FromForm] string? currentPassword,
            [FromForm] string? newPassword,
            [FromForm] string? confirmPassword)
        {
            var student = await GetStudentAsync();

            if (student == null)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Vui lòng nhập mật khẩu hiện tại.");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "Vui lòng nhập mật khẩu mới.");
            }
            else if (newPassword.Length < 8)
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "Mật khẩu mới phải có ít nhất 8 ký tự.");
            }
            else if (newPassword.Length > 128)
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "Mật khẩu mới không được vượt quá 128 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Vui lòng xác nhận mật khẩu mới.");
            }
            else if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Mật khẩu xác nhận không khớp.");
            }

            if (ModelState.IsValid)
            {
                var verification =
                    _passwordHasher.VerifyHashedPassword(
                        student,
                        student.PasswordHash,
                        currentPassword!);

                if (verification ==
                    PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError(
                        "CurrentPassword",
                        "Mật khẩu hiện tại không chính xác.");
                }
            }

            if (ModelState.IsValid &&
                currentPassword == newPassword)
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "Mật khẩu mới phải khác mật khẩu hiện tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Student/Profile.cshtml",
                    BuildModel(student));
            }

            student.PasswordHash =
                _passwordHasher.HashPassword(
                    student,
                    newPassword!);

            await _db.SaveChangesAsync();

            // Thu hồi cookie hiện tại để người dùng
            // đăng nhập lại bằng mật khẩu mới.
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["LoginSuccess"] =
                "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";

            return RedirectToAction(
                "Login",
                "Account");
        }

        private async Task RefreshSignInAsync(User student)
        {
            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    student.Id.ToString()),

                new(
                    ClaimTypes.Name,
                    student.FullName),

                new(
                    ClaimTypes.Email,
                    student.Email),

                new(
                    ClaimTypes.Role,
                    student.Role)
            };

            if (!string.IsNullOrWhiteSpace(student.StudentCode))
            {
                claims.Add(new Claim(
                    "StudentCode",
                    student.StudentCode));
            }

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }
    }
}
