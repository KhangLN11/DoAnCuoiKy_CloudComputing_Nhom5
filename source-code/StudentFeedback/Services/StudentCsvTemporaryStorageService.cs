using System.Security.Cryptography;
using System.Text.Json;

namespace StudentFeedback.Services;

public class StudentCsvTemporaryStorageService
{
    private readonly string _storageDirectory;
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan OrphanGracePeriod = TimeSpan.FromMinutes(30);
    private readonly SemaphoreSlim _confirmationLock = new(1, 1);

    public StudentCsvTemporaryStorageService(IWebHostEnvironment environment)
    {
        _storageDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "StudentCsvImports");
        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<string> SaveAsync(Stream csvStream, int adminUserId)
    {
        if (adminUserId <= 0)
            throw new ArgumentException("Tài khoản Admin không hợp lệ.", nameof(adminUserId));

        // Opportunistic cleanup: runs whenever a new preview is stored.
        // Do not block uploads when cleanup fails.
        await CleanupExpiredFilesAsync();

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var csvPath = GetCsvPath(token);
        var metadataPath = GetMetadataPath(token);
        var metadata = new StudentCsvTemporaryMetadata
        {
            AdminUserId = adminUserId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Pending"
        };

        try
        {
            const long maxSize = StudentCsvPreviewService.MaxFileSize;
            await using (var output = new FileStream(csvPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long totalBytes = 0;
                int bytesRead;
                while ((bytesRead = await csvStream.ReadAsync(buffer.AsMemory())) > 0)
                {
                    totalBytes += bytesRead;
                    if (totalBytes > maxSize)
                        throw new InvalidDataException("File CSV vượt quá 2 MB.");
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }

            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));
            return token;
        }
        catch
        {
            Delete(token);
            throw;
        }
    }

    public async Task<Stream?> OpenReadAsync(string? token, int adminUserId)
    {
        if (!IsValidToken(token) || adminUserId <= 0)
            return null;

        var csvPath = GetCsvPath(token!);
        var metadataPath = GetMetadataPath(token!);
        if (!File.Exists(csvPath) || !File.Exists(metadataPath))
            return null;

        StudentCsvTemporaryMetadata? metadata;
        try
        {
            var json = await File.ReadAllTextAsync(metadataPath);
            metadata = JsonSerializer.Deserialize<StudentCsvTemporaryMetadata>(json);
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        catch (JsonException) { return null; }

        if (metadata == null || metadata.AdminUserId != adminUserId || metadata.Status != "Pending")
            return null;

        if (metadata.CreatedAtUtc > DateTime.UtcNow || DateTime.UtcNow - metadata.CreatedAtUtc > Expiration)
        {
            Delete(token!);
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(csvPath);
            if (fileInfo.Length == 0 || fileInfo.Length > StudentCsvPreviewService.MaxFileSize)
                return null;
            return new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    // Call only after the database transaction commits, while holding confirmation lock.
    public async Task MarkCompletedAsync(string? token, int adminUserId)
    {
        if (!IsValidToken(token) || adminUserId <= 0)
            throw new InvalidOperationException("ImportToken không hợp lệ.");

        var metadataPath = GetMetadataPath(token!);
        if (!File.Exists(metadataPath))
            throw new InvalidOperationException("Không tìm thấy ImportToken.");

        var json = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<StudentCsvTemporaryMetadata>(json);
        if (metadata == null || metadata.AdminUserId != adminUserId || metadata.Status != "Pending")
            throw new InvalidOperationException("ImportToken không thể xác nhận.");

        metadata.Status = "Completed";
        await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));
    }

    // Cleanup is serialized with confirmations in this application instance.
    // Orphan files are only removed after a 30-minute grace period so a concurrent
    // SaveAsync that is still writing its CSV is not accidentally deleted.
    public async Task CleanupExpiredFilesAsync()
    {
        await _confirmationLock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            foreach (var metadataPath in Directory.EnumerateFiles(_storageDirectory, "*.json"))
            {
                var token = Path.GetFileNameWithoutExtension(metadataPath);
                if (!IsValidToken(token)) continue;

                try
                {
                    var info = new FileInfo(metadataPath);
                    // Do not interfere with very recent writes.
                    if (now - info.LastWriteTimeUtc < TimeSpan.FromMinutes(1)) continue;

                    var json = await File.ReadAllTextAsync(metadataPath);
                    var metadata = JsonSerializer.Deserialize<StudentCsvTemporaryMetadata>(json);
                    var expired = metadata == null ||
                                  metadata.CreatedAtUtc > now ||
                                  now - metadata.CreatedAtUtc > Expiration ||
                                  metadata.Status == "Completed";
                    if (expired) Delete(token);
                }
                catch (IOException) { /* Retry on next upload. */ }
                catch (UnauthorizedAccessException) { /* Retry on next upload. */ }
                catch (JsonException)
                {
                    if (now - File.GetLastWriteTimeUtc(metadataPath) > OrphanGracePeriod)
                        Delete(token);
                }
            }

            foreach (var csvPath in Directory.EnumerateFiles(_storageDirectory, "*.csv"))
            {
                var token = Path.GetFileNameWithoutExtension(csvPath);
                if (!IsValidToken(token)) continue;
                try
                {
                    if (!File.Exists(GetMetadataPath(token)) &&
                        now - File.GetLastWriteTimeUtc(csvPath) > OrphanGracePeriod)
                        DeleteFileIfExists(csvPath);
                }
                catch (IOException) { /* Retry on next upload. */ }
                catch (UnauthorizedAccessException) { /* Retry on next upload. */ }
            }
        }
        catch (IOException) { /* Cleanup must not prevent CSV uploads. */ }
        catch (UnauthorizedAccessException) { /* Cleanup must not prevent CSV uploads. */ }
        finally
        {
            _confirmationLock.Release();
        }
    }

    public async Task<IDisposable> AcquireConfirmationLockAsync()
    {
        await _confirmationLock.WaitAsync();
        return new ConfirmationLockHandle(_confirmationLock);
    }

    public void Delete(string? token)
    {
        if (!IsValidToken(token)) return;
        DeleteFileIfExists(GetCsvPath(token!));
        DeleteFileIfExists(GetMetadataPath(token!));
    }

    private string GetCsvPath(string token) => Path.Combine(_storageDirectory, $"{token}.csv");
    private string GetMetadataPath(string token) => Path.Combine(_storageDirectory, $"{token}.json");

    private static bool IsValidToken(string? token) =>
        token is { Length: 64 } && token.All(c =>
            (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'));

    private static void DeleteFileIfExists(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class ConfirmationLockHandle : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;
        public ConfirmationLockHandle(SemaphoreSlim semaphore) => _semaphore = semaphore;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _semaphore.Release();
        }
    }

    private sealed class StudentCsvTemporaryMetadata
    {
        public int AdminUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
