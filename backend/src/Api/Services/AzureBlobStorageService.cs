using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace Api.Services;

public sealed class AzureBlobStorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(string connectionString, string containerName)
    {
        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var fileName = $"{Guid.NewGuid()}{extension}";
        var blob = _containerClient.GetBlobClient(fileName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: cancellationToken);

        return blob.Uri.ToString();
    }
}
