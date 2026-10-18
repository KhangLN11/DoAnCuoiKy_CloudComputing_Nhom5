using Minio;
using Minio.DataModel.Args;

namespace StudentFeedback.Services;

public class MinioStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;

    public MinioStorageService(
        IMinioClient client,
        IConfiguration configuration)
    {
        _client = client;

        _bucketName = configuration["Minio:BucketName"]
            ?? "student-feedback-attachments";
    }

    public async Task<bool> CheckConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        await _client.ListBucketsAsync(cancellationToken);
        return true;
    }

    public async Task EnsureBucketExistsAsync(
        CancellationToken cancellationToken = default)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs()
                .WithBucket(_bucketName),
            cancellationToken);

        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs()
                    .WithBucket(_bucketName),
                cancellationToken);
        }
    }

    public async Task UploadAsync(
        string objectKey,
        Stream stream,
        long fileSize,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException(
                "ObjectKey không được để trống.",
                nameof(objectKey));

        if (fileSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(fileSize));

        await EnsureBucketExistsAsync(cancellationToken);

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(fileSize)
                .WithContentType(contentType),
            cancellationToken);
    }

    public async Task DownloadAsync(
        string objectKey,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException(
                "ObjectKey không được để trống.",
                nameof(objectKey));

        await _client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(source =>
                {
                    source.CopyTo(destination);
                }),
            cancellationToken);
    }
}