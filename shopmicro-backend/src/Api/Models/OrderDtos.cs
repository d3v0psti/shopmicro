using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public record CreateOrderItemRequest(
    [Required] Guid ProductId,
    [Range(1, 1000)] int Quantity
);

public record CreateOrderRequest(
    [Required, MaxLength(150)] string CustomerName,
    [Required, EmailAddress] string CustomerEmail,
    string? CustomerCpf,
    string? CustomerPhone,
    string? CustomerCep,
    string? CustomerAddress,
    [Required] string PaymentMethod,
    [MinLength(1)] List<CreateOrderItemRequest> Items
);

public record UpdateOrderStatusRequest(
    [Required] OrderStatus Status
);

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
)
{
    public static OrderItemResponse FromEntity(OrderItem i) =>
        new(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity);
}

public record OrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    string? CustomerCpf,
    string? CustomerPhone,
    string? CustomerCep,
    string? CustomerAddress,
    string PaymentMethod,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    List<OrderItemResponse> Items
)
{
    public static OrderResponse FromEntity(Order o) =>
        new(o.Id, o.CustomerName, o.CustomerEmail, o.CustomerCpf, o.CustomerPhone, o.CustomerCep,
            o.CustomerAddress, o.PaymentMethod, o.Status, o.TotalAmount, o.CreatedAt, o.CancelledAt,
            o.Items.Select(OrderItemResponse.FromEntity).ToList());
}
