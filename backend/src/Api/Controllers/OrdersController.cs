using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(AppDbContext db, ILogger<OrdersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /api/v1/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll(CancellationToken ct)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return Ok(orders.Select(OrderResponse.FromEntity));
    }

    // GET /api/v1/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return NotFound();

        return Ok(OrderResponse.FromEntity(order));
    }

    // POST /api/v1/orders
    // Cria o pedido validando estoque e "congelando" preço/nome do produto no momento da compra.
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "O pedido precisa ter ao menos um item." });

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var order = new Order
        {
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending
        };

        decimal total = 0;

        foreach (var item in request.Items)
        {
            var product = await _db.Products.FindAsync(new object[] { item.ProductId }, ct);
            if (product is null)
                return BadRequest(new { message = $"Produto {item.ProductId} não encontrado." });

            if (product.StockQuantity < item.Quantity)
                return BadRequest(new { message = $"Estoque insuficiente para '{product.Name}'." });

            product.StockQuantity -= item.Quantity;

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            };

            total += orderItem.UnitPrice * orderItem.Quantity;
            order.Items.Add(orderItem);
        }

        order.TotalAmount = total;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Pedido {OrderId} criado, total {Total}", order.Id, order.TotalAmount);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, OrderResponse.FromEntity(order));
    }

    // PUT /api/v1/orders/{id}/status
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return NotFound();

        order.Status = request.Status;
        await _db.SaveChangesAsync(ct);

        return Ok(OrderResponse.FromEntity(order));
    }
}
