
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentFeedback.Data;
using StudentFeedback.Models;

namespace StudentFeedback.Services;

public class PasswordSetupTokenService
{
    private readonly ApplicationDbContext _db;

    private static readonly TimeSpan TokenLifetime =
        TimeSpan.FromMinutes(30);

    public PasswordSetupTokenService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Tạo token cho sinh viên CHƯA kích hoạt.
    // Chỉ trả token gốc một lần để tạo liên kết gửi email.
    public async Task<string?> CreateTokenAsync(int userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.Role == "Student" &&
                !u.IsActive);

        if (user == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        // Giới hạn tạo lại token: tối thiểu 5 phút
        // giữa hai lần cấp cho cùng một sinh viên.
        var lastCreatedAt = await _db.PasswordSetupTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => (DateTime?)t.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (lastCreatedAt.HasValue &&
            lastCreatedAt.Value > now.AddMinutes(-5))
        {
            return null;
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        // Hủy hiệu lực token cũ chưa sử dụng.
        await _db.PasswordSetupTokens
            .Where(t =>
                t.UserId == userId &&
                t.UsedAtUtc == null)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    t => t.UsedAtUtc,
                    now));

        // Sinh token 32 byte bằng bộ sinh số ngẫu nhiên bảo mật.
        var token = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        var entity = new PasswordSetupToken
        {
            UserId = userId,
            TokenHash = HashToken(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(TokenLifetime),
            UsedAtUtc = null
        };

        _db.PasswordSetupTokens.Add(entity);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return token;
    }

    // Kiểm tra token còn hiệu lực và thuộc sinh viên
    // chưa hoàn tất thiết lập mật khẩu.
    public async Task<bool> IsTokenValidAsync(string? token)
    {
        if (!IsValidTokenFormat(token))
        {
            return false;
        }

        var tokenHash = HashToken(token!);
        var now = DateTime.UtcNow;

        return await _db.PasswordSetupTokens
            .AsNoTracking()
            .AnyAsync(t =>
                t.TokenHash == tokenHash &&
                t.UsedAtUtc == null &&
                t.ExpiresAtUtc > now &&
                t.User.Role == "Student" &&
                !t.User.IsActive);
    }

    // Đặt mật khẩu và kích hoạt tài khoản trong cùng transaction.
    public async Task<bool> SetPasswordAsync(
        string? token,
        string newPassword)
    {
        if (!IsValidTokenFormat(token) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 12 ||
            newPassword.Length > 128)
        {
            return false;
        }

        var tokenHash = HashToken(token!);
        var now = DateTime.UtcNow;

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        // Chỉ một yêu cầu được phép tiêu thụ token.
        var affectedRows = await _db.PasswordSetupTokens
            .Where(t =>
                t.TokenHash == tokenHash &&
                t.UsedAtUtc == null &&
                t.ExpiresAtUtc > now &&
                t.User.Role == "Student" &&
                !t.User.IsActive)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    t => t.UsedAtUtc,
                    now));

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();
            return false;
        }

        var setupToken = await _db.PasswordSetupTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash);

        if (setupToken == null)
        {
            await transaction.RollbackAsync();
            return false;
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == setupToken.UserId &&
                u.Role == "Student" &&
                !u.IsActive);

        if (user == null)
        {
            await transaction.RollbackAsync();
            return false;
        }

        var passwordHasher = new PasswordHasher<User>();

        user.PasswordHash =
            passwordHasher.HashPassword(user, newPassword);

        // Chỉ kích hoạt sau khi mật khẩu mới được thiết lập.
        user.IsActive = true;

        await _db.SaveChangesAsync();

        // Hủy hiệu lực mọi token còn lại của sinh viên.
        await _db.PasswordSetupTokens
            .Where(t =>
                t.UserId == user.Id &&
                t.UsedAtUtc == null)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    t => t.UsedAtUtc,
                    now));

        await transaction.CommitAsync();

        return true;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }

    private static bool IsValidTokenFormat(string? token)
    {
        return token is { Length: 64 } &&
               token.All(c =>
                   (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F'));
    }
}
