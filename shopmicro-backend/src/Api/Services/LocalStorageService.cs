using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Api.Services;

public sealed class LocalStorageService : IStorageService
{
    private readonly string _uploadsFolderPath;

    public LocalStorageService(IWebHostEnvironment environment)
    {
        _uploadsFolderPath = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(_uploadsFolderPath);
    }

    public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        var fileName = $"{Guid.NewGuid()}{extension}";
        var targetPath = Path.Combine(_uploadsFolderPath, fileName);

        await using var outputStream = File.Create(targetPath);
        await file.CopyToAsync(outputStream, cancellationToken);

        return $"/uploads/{fileName}";
    }

    public Task<StorageFile?> GetFileAsync(string fileName, CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal))
            return Task.FromResult<StorageFile?>(null);

        var path = Path.Combine(_uploadsFolderPath, safeFileName);
        if (!File.Exists(path))
            return Task.FromResult<StorageFile?>(null);

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        if (!contentTypeProvider.TryGetContentType(safeFileName, out var contentType))
            contentType = "application/octet-stream";

        StorageFile result = new(File.OpenRead(path), contentType);
        return Task.FromResult<StorageFile?>(result);
    }
}
