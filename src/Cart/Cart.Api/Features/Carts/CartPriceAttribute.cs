using System.ComponentModel.DataAnnotations;
using Cart.Api.Domain.Carts;

namespace Cart.Api.Features.Carts;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class CartPriceAttribute : ValidationAttribute
{
    public CartPriceAttribute()
    {
        ErrorMessage =
            $"The {{0}} field must fit decimal({CartConstraints.PricePrecision},{CartConstraints.PriceScale}).";
    }

    public override bool IsValid(object? value) =>
        value is decimal price && CartPrice.IsValid(price);
}
