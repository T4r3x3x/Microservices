using System.Globalization;

namespace Orders.Api.Domain.Orders;

public static class OrderMoney
{
    private static readonly decimal MaxValue = decimal.Parse(
        OrderConstraints.MaxMoney,
        CultureInfo.InvariantCulture);

    public static bool IsValid(decimal value) =>
        value >= 0
        && value <= MaxValue
        && GetScale(value) <= OrderConstraints.MoneyScale;

    private static int GetScale(decimal value) =>
        (decimal.GetBits(value)[3] >> 16) & 0xFF;
}
