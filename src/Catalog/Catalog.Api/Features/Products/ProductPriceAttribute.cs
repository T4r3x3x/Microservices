using System.ComponentModel.DataAnnotations;
using Catalog.Api.Domain.Products;

namespace Catalog.Api.Features.Products;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ProductPriceAttribute : ValidationAttribute
{
    public ProductPriceAttribute()
    {
        ErrorMessage =
            $"Price must be non-negative and fit decimal({ProductConstraints.PricePrecision},{ProductConstraints.PriceScale}).";
    }

    public override bool IsValid(object? value) =>
        value is null || value is decimal price && ProductPrice.IsValid(price);
}
