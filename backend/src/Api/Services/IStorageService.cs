using Microsoft.AspNetCore.Http;

namespace Api.Services;

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken);
}
