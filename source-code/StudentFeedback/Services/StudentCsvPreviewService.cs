
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using StudentFeedback.Models.ViewModels;

namespace StudentFeedback.Services;

public class StudentCsvPreviewService
{
    public const long MaxFileSize = 2 * 1024 * 1024;
    public const int MaxDataRows = 5000;

    public AdminStudentCsvPreviewViewModel Preview(
        Stream fileStream,
        string? fileName)
    {
        var result = new AdminStudentCsvPreviewViewModel
        {
            FileName = fileName
        };

        // Chỉ đọc UTF-8 hợp lệ.
        // UTF-8 BOM được xử lý ở phần kiểm tra header.
        using var reader = new StreamReader(
            fileStream,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: false);

        var config = new CsvConfiguration(
            CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = false,
            MissingFieldFound = null,
            HeaderValidated = null,

            BadDataFound = args =>
            {
                throw new CsvHelperException(
                    args.Context,
                    "Dữ liệu CSV sai định dạng.");
            }
        };

        using var csv = new CsvReader(reader, config);

        try
        {
            // 1. Kiểm tra file rỗng.
            if (!csv.Read())
            {
                result.FileErrors.Add("File CSV trống.");
                return result;
            }

            // 2. Đọc tiêu đề CSV.
            csv.ReadHeader();

            var headers = csv.HeaderRecord
                ?? Array.Empty<string>();

            string[] expectedHeaders =
            {
                "StudentCode",
                "FullName",
                "Email",
                "ClassName"
            };

            // 3. Kiểm tra đúng 4 cột.
            if (headers.Length != expectedHeaders.Length)
            {
                result.FileErrors.Add(
                    "Tiêu đề CSV phải gồm đúng 4 cột: " +
                    "StudentCode,FullName,Email,ClassName.");

                return result;
            }

            // 4. Chuẩn hóa header.
            // Loại bỏ BOM nếu xuất hiện ở cột đầu tiên.
            var normalizedHeaders = headers
                .Select((header, index) =>
                    index == 0
                        ? header.Trim().TrimStart('\uFEFF')
                        : header.Trim())
                .ToArray();

            if (!normalizedHeaders.SequenceEqual(
                expectedHeaders,
                StringComparer.OrdinalIgnoreCase))
            {
                result.FileErrors.Add(
                    "Tiêu đề CSV phải gồm đúng 4 cột: " +
                    "StudentCode,FullName,Email,ClassName.");

                return result;
            }

            // 5. Theo dõi dữ liệu trùng trong file.
            var seenEmails = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            var seenStudentCodes = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            // 6. Đọc từng dòng sinh viên.
            while (csv.Read())
            {
                if (result.Rows.Count >= MaxDataRows)
                {
                    result.FileErrors.Add(
                        $"File vượt quá {MaxDataRows} dòng dữ liệu.");

                    break;
                }

                int rowNumber =
                    csv.Context.Parser?.Row ?? 0;

                int columnCount = csv.Parser.Count;

                // Dòng không đủ hoặc thừa cột.
                if (columnCount != 4)
                {
                    var invalidRow =
                        new AdminStudentCsvRowViewModel
                        {
                            RowNumber = rowNumber
                        };

                    invalidRow.Errors.Add(
                        "Dòng CSV phải có đúng 4 cột, " +
                        $"hiện có {columnCount} cột.");

                    result.Rows.Add(invalidRow);
                    continue;
                }

                // 7. Lấy dữ liệu sinh viên.
                var row =
                    new AdminStudentCsvRowViewModel
                    {
                        RowNumber = rowNumber,

                        StudentCode =
                            csv.GetField(0)?.Trim() ?? "",

                        FullName =
                            csv.GetField(1)?.Trim() ?? "",

                        Email =
                            csv.GetField(2)?.Trim() ?? "",

                        ClassName =
                            csv.GetField(3)?.Trim()
                    };

                // 8. Kiểm tra dữ liệu từng cột.
                ValidateRow(row);

                // 9. Kiểm tra email trùng trong CSV.
                if (!string.IsNullOrWhiteSpace(row.Email) &&
                    !seenEmails.Add(row.Email))
                {
                    row.Errors.Add(
                        "Email trùng trong file CSV.");
                }

                // 10. Kiểm tra mã sinh viên trùng.
                if (!string.IsNullOrWhiteSpace(
                        row.StudentCode) &&
                    !seenStudentCodes.Add(
                        row.StudentCode))
                {
                    row.Errors.Add(
                        "Mã sinh viên trùng trong file CSV.");
                }

                result.Rows.Add(row);
            }
        }
        catch (CsvHelperException)
        {
            result.FileErrors.Add(
                "File CSV có cấu trúc không hợp lệ. " +
                "Vui lòng kiểm tra dấu ngoặc kép " +
                "và số cột.");
        }
        catch (DecoderFallbackException)
        {
            result.FileErrors.Add(
                "File phải sử dụng mã hóa UTF-8 hợp lệ.");
        }
        catch (IOException)
        {
            result.FileErrors.Add(
                "Không thể đọc file CSV.");
        }

        // Nếu file có lỗi nghiêm trọng,
        // không hiển thị dữ liệu xem trước.
        if (result.FileErrors.Count > 0)
        {
            result.Rows.Clear();
            result.TotalRows = 0;
            result.ValidRows = 0;
            result.InvalidRows = 0;

            return result;
        }

        // 11. Thống kê kết quả.
        result.TotalRows = result.Rows.Count;

        result.ValidRows = result.Rows.Count(
            r => r.IsValid);

        result.InvalidRows = result.Rows.Count(
            r => !r.IsValid);

        return result;
    }

    private static void ValidateRow(
        AdminStudentCsvRowViewModel row)
    {
        // Mã sinh viên.
        if (string.IsNullOrWhiteSpace(
                row.StudentCode))
        {
            row.Errors.Add(
                "Mã sinh viên không được để trống.");
        }
        else if (row.StudentCode.Length > 20)
        {
            row.Errors.Add(
                "Mã sinh viên tối đa 20 ký tự.");
        }

        // Họ tên.
        if (string.IsNullOrWhiteSpace(
                row.FullName))
        {
            row.Errors.Add(
                "Họ tên không được để trống.");
        }
        else if (row.FullName.Length > 100)
        {
            row.Errors.Add(
                "Họ tên tối đa 100 ký tự.");
        }

        // Email.
        if (string.IsNullOrWhiteSpace(row.Email))
        {
            row.Errors.Add(
                "Email không được để trống.");
        }
        else
        {
            var emailValidator =
                new EmailAddressAttribute();

            if (row.Email.Length > 255 ||
                !emailValidator.IsValid(row.Email))
            {
                row.Errors.Add(
                    "Email không hợp lệ.");
            }
        }

        // Lớp học (không bắt buộc).
        if (!string.IsNullOrWhiteSpace(
                row.ClassName) &&
            row.ClassName.Length > 100)
        {
            row.Errors.Add(
                "Tên lớp tối đa 100 ký tự.");
        }
    }
}
