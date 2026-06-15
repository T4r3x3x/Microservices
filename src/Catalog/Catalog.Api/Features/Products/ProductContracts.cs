using System.ComponentModel.DataAnnotations;
using Catalog.Api.Domain.Products;

namespace Catalog.Api.Features.Products;

public sealed class ProductListQuery : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? Page { get; init; }

    [Range(1, 100)]
    public int? PageSize { get; init; }

    [StringLength(ProductConstraints.NameMaxLength)]
    public string? Search { get; init; }

    [ProductPrice]
    public decimal? MinPrice { get; init; }

    [ProductPrice]
    public decimal? MaxPrice { get; init; }

    public bool? InStock { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice > MaxPrice)
        {
            yield return new ValidationResult(
                "MinPrice cannot be greater than MaxPrice.",
                [nameof(MinPrice), nameof(MaxPrice)]);
        }
    }
}

public sealed record CreateProductRequest(
    [property: Required]
    [property: NotWhiteSpace]
    [property: StringLength(ProductConstraints.NameMaxLength, MinimumLength = 1)]
    string Name,
    [property: StringLength(ProductConstraints.DescriptionMaxLength)]
    string? Description,
    [property: ProductPrice]
    decimal Price,
    [property: Range(0, int.MaxValue)]
    int AvailableQuantity);

public sealed record UpdateProductRequest(
    [property: Required]
    [property: NotWhiteSpace]
    [property: StringLength(ProductConstraints.NameMaxLength, MinimumLength = 1)]
    string Name,
    [property: StringLength(ProductConstraints.DescriptionMaxLength)]
    string? Description,
    [property: ProductPrice]
    decimal Price,
    [property: Range(0, int.MaxValue)]
    int AvailableQuantity);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int AvailableQuantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
