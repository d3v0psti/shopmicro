using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Api.Services;

public sealed class S3StorageService : IStorageService
{
    private const string ObjectPrefix = "uploads/";
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3StorageService(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _bucketName = configuration["S3_BUCKET_NAME"]
            ?? throw new InvalidOperationException("S3_BUCKET_NAME não configurado.");
    }

    public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var inputStream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"{ObjectPrefix}{fileName}",
            InputStream = inputStream,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        return $"/uploads/{fileName}";
    }

    public async Task<StorageFile?> GetFileAsync(string fileName, CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal))
            return null;

        try
        {
            var response = await _s3.GetObjectAsync(
                _bucketName,
                $"{ObjectPrefix}{safeFileName}",
                cancellationToken);

            var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType;

            return new StorageFile(response.ResponseStream, contentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
