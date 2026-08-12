namespace Api.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public string Category { get; set; } = "Geral";

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
