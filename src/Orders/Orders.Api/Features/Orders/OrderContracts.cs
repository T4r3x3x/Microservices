using System.ComponentModel.DataAnnotations;
using Orders.Api.Domain.Orders;

namespace Orders.Api.Features.Orders;

public sealed record CreateOrderRequest(
    [property: Required]
    [property: NotWhiteSpace]
    [property: StringLength(OrderConstraints.UserIdMaxLength, MinimumLength = 1)]
    string UserId);

public sealed record OrderResponse(
    Guid Id,
    string UserId,
    OrderStatus Status,
    IReadOnlyList<OrderItemResponse> Items,
    decimal TotalPrice,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);
