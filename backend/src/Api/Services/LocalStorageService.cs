using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

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
}
