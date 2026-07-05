using System.ComponentModel.DataAnnotations;

namespace Cart.Api.Features.Carts;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public NotWhiteSpaceAttribute()
    {
        ErrorMessage = "The {0} field must not be empty or whitespace.";
    }

    public override bool IsValid(object? value) =>
        value is string text && !string.IsNullOrWhiteSpace(text);
}
