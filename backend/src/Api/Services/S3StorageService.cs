using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

namespace Api.Services;

public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly string _regionName;

    public S3StorageService(IAmazonS3 client, string bucketName, string regionName)
    {
        _client = client;
        _bucketName = bucketName;
        _regionName = regionName;
    }

    public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var key = $"{Guid.NewGuid()}{extension}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _client.PutObjectAsync(request, cancellationToken);

        var normalizedRegion = _regionName?.Trim();
        var endpoint = string.IsNullOrWhiteSpace(normalizedRegion)
            ? $"https://{_bucketName}.s3.amazonaws.com/{key}"
            : $"https://{_bucketName}.s3.{normalizedRegion}.amazonaws.com/{key}";

        return endpoint;
    }
}
