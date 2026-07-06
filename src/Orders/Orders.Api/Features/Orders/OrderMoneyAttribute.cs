using System.ComponentModel.DataAnnotations;
using Orders.Api.Domain.Orders;

namespace Orders.Api.Features.Orders;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class OrderMoneyAttribute : ValidationAttribute
{
    public OrderMoneyAttribute()
    {
        ErrorMessage =
            $"The {{0}} field must fit decimal({OrderConstraints.MoneyPrecision},{OrderConstraints.MoneyScale}).";
    }

    public override bool IsValid(object? value) =>
        value is decimal money && OrderMoney.IsValid(money);
}
