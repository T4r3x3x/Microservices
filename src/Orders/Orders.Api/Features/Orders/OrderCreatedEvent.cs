namespace Orders.Api.Features.Orders;

public sealed record OrderCreatedEvent(
    Guid OrderId,
    string UserId,
    decimal TotalPrice,
    IReadOnlyList<OrderCreatedItem> Items,
    DateTimeOffset CreatedAt);

public sealed record OrderCreatedItem(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);
