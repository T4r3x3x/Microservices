using System.ComponentModel.DataAnnotations;
using Cart.Api.Domain.Carts;

namespace Cart.Api.Features.Carts;

public sealed record UpsertCartItemRequest(
    [property: NotEmptyGuid]
    Guid ProductId,
    [property: Range(1, int.MaxValue)]
    int Quantity);

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
