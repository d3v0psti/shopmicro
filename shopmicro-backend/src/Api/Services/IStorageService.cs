using Microsoft.AspNetCore.Http;

namespace Api.Services;

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken);
    Task<StorageFile?> GetFileAsync(string fileName, CancellationToken cancellationToken);
}

public sealed record StorageFile(Stream Content, string ContentType);
