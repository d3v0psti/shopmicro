using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;

namespace Api.Services;

public sealed class GcpStorageService : IStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;

    public GcpStorageService(string bucketName, string? serviceAccountJson = null)
    {
        _bucketName = bucketName;
        _storageClient = string.IsNullOrWhiteSpace(serviceAccountJson)
            ? StorageClient.Create()
            : StorageClient.Create(GoogleCredential.FromJson(serviceAccountJson));
    }

    public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var objectName = $"{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        await _storageClient.UploadObjectAsync(_bucketName, objectName, file.ContentType, stream, cancellationToken: cancellationToken);

        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }
}
