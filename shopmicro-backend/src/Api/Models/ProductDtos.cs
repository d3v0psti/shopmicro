using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public record CreateProductRequest(
    [Required, MaxLength(150)] string Name,
    string? Description,
    [Range(0.01, double.MaxValue)] decimal Price,
    string? ImageUrl,
    string Category,
    [Range(0, int.MaxValue)] int StockQuantity
);

public record UpdateProductRequest(
    [Required, MaxLength(150)] string Name,
    string? Description,
    [Range(0.01, double.MaxValue)] decimal Price,
    string? ImageUrl,
    string Category,
    [Range(0, int.MaxValue)] int StockQuantity
);

public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    string Category,
    int StockQuantity,
    DateTime CreatedAt
)
{
    public static ProductResponse FromEntity(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.Category, p.StockQuantity, p.CreatedAt);
}
