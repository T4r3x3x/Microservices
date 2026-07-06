namespace Orders.Api.Features.Carts;

public sealed record CartResponse(
    string UserId,
    IReadOnlyList<CartItemResponse> Items,
    decimal TotalPrice,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CartItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);
