namespace Api.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Cancelled = 3
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    // Dados completos do pedido — guardados como snapshot (não referenciam a
    // tabela de usuários), então cancelar um pedido ou excluir a conta do
    // cliente nunca quebra o histórico de pedidos já realizados.
    public string? CustomerCpf { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerCep { get; set; }

    public string? CustomerAddress { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }

    // Snapshot do nome/preço no momento da compra (não muda se o produto mudar depois)
    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}
