namespace Cart.Api.Features.Catalog;

public sealed record ProductCatalogItem(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int AvailableQuantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
