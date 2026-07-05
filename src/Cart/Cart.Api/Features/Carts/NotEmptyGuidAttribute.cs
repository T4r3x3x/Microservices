using System.ComponentModel.DataAnnotations;

namespace Cart.Api.Features.Carts;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute()
    {
        ErrorMessage = "The {0} field must not be empty.";
    }

    public override bool IsValid(object? value) =>
        value is Guid guid && guid != Guid.Empty;
}
