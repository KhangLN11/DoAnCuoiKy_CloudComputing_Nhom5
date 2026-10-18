
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFeedback.Models.ViewModels;
using StudentFeedback.Services;

namespace StudentFeedback.Controllers;

[AllowAnonymous]
public class PasswordSetupController : Controller
{
    private readonly PasswordSetupTokenService _tokenService;
    private readonly ILogger<PasswordSetupController> _logger;

    public PasswordSetupController(
        PasswordSetupTokenService tokenService,
        ILogger<PasswordSetupController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    // GET: /PasswordSetup/Index?token=...
    [HttpGet]
    public async Task<IActionResult> Index(string? token)
    {
        if (!await _tokenService.IsTokenValidAsync(token))
        {
            return View("InvalidToken");
        }

        // Tránh lưu trang chứa token trong cache.
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";

        return View(new PasswordSetupViewModel
        {
            Token = token!
        });
    }

    // POST: /PasswordSetup/Index
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        PasswordSetupViewModel model)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            bool success = await _tokenService.SetPasswordAsync(
                model.Token,
                model.NewPassword);

            if (!success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Liên kết không hợp lệ, đã hết hạn hoặc mật khẩu không đáp ứng yêu cầu.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Thiết lập mật khẩu thành công. Bạn có thể đăng nhập.";

            return RedirectToAction(
                "Login",
                "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi thiết lập mật khẩu sinh viên.");

            ModelState.AddModelError(
                string.Empty,
                "Không thể thiết lập mật khẩu lúc này. Vui lòng thử lại.");

            return View(model);
        }
    }
}
