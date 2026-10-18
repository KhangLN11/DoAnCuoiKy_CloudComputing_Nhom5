
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace StudentFeedback.Services;

public sealed class EmailSenderService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(
        IOptions<SmtpOptions> options,
        ILogger<EmailSenderService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendPasswordSetupEmailAsync(
        string recipientEmail,
        string recipientName,
        string setupUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException(
                "Địa chỉ email người nhận không hợp lệ.",
                nameof(recipientEmail));
        }

        if (!Uri.TryCreate(
                setupUrl,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             !(uri.Scheme == Uri.UriSchemeHttp &&
               uri.IsLoopback)))
        {
            throw new ArgumentException(
                "Liên kết thiết lập mật khẩu không hợp lệ.",
                nameof(setupUrl));
        }

        if (string.IsNullOrWhiteSpace(_options.Host) ||
            _options.Port <= 0 ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình đầy đủ Gmail SMTP.");
        }

        var message = new MimeMessage();

        message.From.Add(
            MailboxAddress.Parse(_options.FromEmail));

        message.To.Add(
            MailboxAddress.Parse(recipientEmail));

        message.Subject =
            "StudentFeedback - Thiết lập mật khẩu tài khoản";

        var displayName = string.IsNullOrWhiteSpace(recipientName)
            ? "bạn"
            : recipientName.Trim();

        // Gửi dạng văn bản thuần để tránh nội dung HTML
        // và các tài nguyên bên ngoài không cần thiết.
        message.Body = new TextPart("plain")
        {
            Text =
                $"Xin chào {displayName},\n\n" +
                "Bạn được mời thiết lập mật khẩu cho tài khoản " +
                "StudentFeedback.\n\n" +
                "Mở liên kết sau để thiết lập mật khẩu:\n" +
                $"{setupUrl}\n\n" +
                "Liên kết có hiệu lực trong 30 phút " +
                "và chỉ sử dụng được một lần.\n\n" +
                "Nếu bạn không mong đợi email này, " +
                "vui lòng liên hệ quản trị viên.\n\n" +
                "Trân trọng,\n" +
                "Hệ thống StudentFeedback"
        };

        using var client = new SmtpClient();

        // Tránh chờ SMTP vô thời hạn.
        client.Timeout = 15000;

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await client.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await client.SendAsync(
                message,
                cancellationToken);

            await client.DisconnectAsync(
                true,
                cancellationToken);

            // Không ghi URL chứa token hoặc App Password vào log.
            _logger.LogInformation(
                "Đã gửi email thiết lập mật khẩu thành công.");
        }
        catch (Exception ex)
        {
            // Không log nội dung email hoặc liên kết bí mật.
            _logger.LogWarning(
                "Gửi email thiết lập mật khẩu thất bại: {ErrorType}",
                ex.GetType().Name);

            throw;
        }
    }
}
