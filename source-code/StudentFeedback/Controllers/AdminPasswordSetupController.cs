
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;
using StudentFeedback.Services;

namespace StudentFeedback.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminPasswordSetupController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordSetupTokenService _tokenService;
    private readonly EmailSenderService _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminPasswordSetupController> _logger;

    public AdminPasswordSetupController(
        ApplicationDbContext db,
        PasswordSetupTokenService tokenService,
        EmailSenderService emailSender,
        IConfiguration configuration,
        ILogger<AdminPasswordSetupController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTestEmail(int studentId)
    {
        if (studentId <= 0)
        {
            return BadRequest();
        }

        var adminIdText = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(adminIdText, out int adminId) ||
            adminId <= 0)
        {
            return Unauthorized();
        }

        var isActiveAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == adminId &&
                u.Role == "Admin" &&
                u.IsActive);

        if (!isActiveAdmin)
        {
            return Forbid();
        }

        var allowedRecipient =
            _configuration["PasswordSetup:TestRecipient"]?.Trim();

        var publicBaseUrl =
            _configuration["PasswordSetup:PublicBaseUrl"]?.Trim();

        if (string.IsNullOrWhiteSpace(allowedRecipient) ||
            string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            TempData["ErrorMessage"] =
                "Chưa cấu hình địa chỉ email hoặc URL kiểm thử.";

            return RedirectToAction("Accounts", "Admin");
        }

        if (!Uri.TryCreate(
                publicBaseUrl,
                UriKind.Absolute,
                out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttp ||
            !baseUri.IsLoopback ||
            !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment) ||
            baseUri.AbsolutePath != "/")
        {
            TempData["ErrorMessage"] =
                "URL kiểm thử phải là địa chỉ HTTP localhost " +
                "hoặc 127.0.0.1 tại thư mục gốc.";

            return RedirectToAction("Accounts", "Admin");
        }

        var student = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == studentId &&
                u.Role == "Student" &&
                !u.IsActive);

        if (student == null)
        {
            TempData["ErrorMessage"] =
                "Không tìm thấy sinh viên chưa kích hoạt.";

            return RedirectToAction("Accounts", "Admin");
        }

        if (!string.Equals(
                student.Email.Trim(),
                allowedRecipient,
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] =
                "Chế độ kiểm thử chỉ cho phép gửi đến " +
                "địa chỉ email đã được cấu hình.";

            return RedirectToAction("Accounts", "Admin");
        }

        try
        {
            // Service đã giới hạn khoảng cách cấp token
            // tối thiểu 5 phút cho mỗi sinh viên.
            var token = await _tokenService.CreateTokenAsync(
                student.Id);

            if (token == null)
            {
                TempData["ErrorMessage"] =
                    "Chưa thể tạo liên kết. Tài khoản có thể " +
                    "đã kích hoạt hoặc vừa được cấp token " +
                    "trong vòng 5 phút.";

                return RedirectToAction("Accounts", "Admin");
            }

            var setupUrl = new Uri(
                baseUri,
                "PasswordSetup/Index?token=" +
                Uri.EscapeDataString(token)).ToString();

            await _emailSender.SendPasswordSetupEmailAsync(
                student.Email,
                student.FullName,
                setupUrl);

            TempData["SuccessMessage"] =
                "Gmail đã chấp nhận email kiểm thử. " +
                "Vui lòng kiểm tra hộp thư người nhận.";
        }
        catch (Exception ex)
        {
            // Không ghi exception chi tiết vì một số lỗi SMTP
            // có thể chứa thông tin nhạy cảm.
            _logger.LogWarning(
                "Gửi email kiểm thử thất bại. " +
                "AdminId={AdminId}, StudentId={StudentId}, " +
                "ErrorType={ErrorType}",
                adminId,
                studentId,
                ex.GetType().Name);

            TempData["ErrorMessage"] =
                "Không thể gửi email kiểm thử. " +
                "Vui lòng kiểm tra cấu hình Gmail SMTP.";
        }

        return RedirectToAction("Accounts", "Admin");
    }
}
