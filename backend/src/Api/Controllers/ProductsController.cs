using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductsController> _logger;
    private readonly IStorageService _storageService;

    public ProductsController(AppDbContext db, IStorageService storageService, ILogger<ProductsController> logger)
    {
        _db = db;
        _storageService = storageService;
        _logger = logger;
    }

    // GET /api/v1/products?category=Eletronicos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll(
        [FromQuery] string? category, CancellationToken ct)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => ProductResponse.FromEntity(p))
            .ToListAsync(ct);

        return Ok(products);
    }

    // GET /api/v1/products/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is null) return NotFound();

        return Ok(ProductResponse.FromEntity(product));
    }

    // GET /api/v1/products/categories
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories(CancellationToken ct)
    {
        var categories = await _db.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

        return Ok(categories);
    }

    // POST /api/v1/products
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromForm] CreateProductRequest request, IFormFile? Image, CancellationToken ct)
    {
        var imageUrl = request.ImageUrl;
        if (Image is not null && Image.Length > 0)
        {
            imageUrl = await _storageService.SaveFileAsync(Image, ct);
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = imageUrl,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Geral" : request.Category,
            StockQuantity = request.StockQuantity
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Produto {ProductId} criado", product.Id);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ProductResponse.FromEntity(product));
    }

    // PUT /api/v1/products/{id}
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, [FromForm] UpdateProductRequest request, IFormFile? Image, CancellationToken ct)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is null) return NotFound();

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Category = request.Category;
        product.StockQuantity = request.StockQuantity;

        if (Image is not null && Image.Length > 0)
        {
            product.ImageUrl = await _storageService.SaveFileAsync(Image, ct);
        }
        else if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            product.ImageUrl = request.ImageUrl;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(ProductResponse.FromEntity(product));
    }

    // DELETE /api/v1/products/{id}
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is null) return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
