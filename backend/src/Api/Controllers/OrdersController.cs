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

    // GET /api/v1/orders            -> todos os pedidos (uso administrativo)
    // GET /api/v1/orders?email=...  -> só os pedidos do cliente informado
    //                                  (usado pelo frontend para "Meus Pedidos"
    //                                  e para a checagem antes de excluir a conta)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll(
        [FromQuery] string? email, CancellationToken ct)
    {
        var query = _db.Orders.Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(o => o.CustomerEmail.ToLower() == email.ToLower());

        var orders = await query
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
    // Cria o pedido validando estoque e "congelando" todos os dados do
    // cliente e do pagamento no momento da compra (snapshot).
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
            CustomerCpf = request.CustomerCpf,
            CustomerPhone = request.CustomerPhone,
            CustomerCep = request.CustomerCep,
            CustomerAddress = request.CustomerAddress,
            PaymentMethod = request.PaymentMethod,
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

    // POST /api/v1/orders/{id}/cancel
    // Cancelamento feito pelo próprio cliente. Devolve os itens ao estoque.
    // Pedidos já enviados (Shipped) não podem mais ser cancelados por aqui.
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return NotFound();

        if (order.Status == OrderStatus.Cancelled)
            return BadRequest(new { message = "Este pedido já está cancelado." });

        if (order.Status == OrderStatus.Shipped)
            return BadRequest(new { message = "Pedidos já enviados não podem ser cancelados por aqui. Entre em contato com o suporte." });

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        foreach (var item in order.Items)
        {
            var product = await _db.Products.FindAsync(new object[] { item.ProductId }, ct);
            if (product is not null)
                product.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Pedido {OrderId} cancelado pelo cliente", order.Id);

        return Ok(OrderResponse.FromEntity(order));
    }

    // PUT /api/v1/orders/{id}/status
    // Uso administrativo (ex: avançar Pending -> Confirmed -> Shipped).
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return NotFound();

        order.Status = request.Status;
        if (request.Status == OrderStatus.Cancelled)
            order.CancelledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(OrderResponse.FromEntity(order));
    }
}
