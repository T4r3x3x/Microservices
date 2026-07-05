using System.Globalization;

namespace Cart.Api.Domain.Carts;

public static class CartPrice
{
    private static readonly decimal MaxValue = decimal.Parse(
        CartConstraints.MaxPrice,
        CultureInfo.InvariantCulture);

    public static bool IsValid(decimal value) =>
        value >= 0
        && value <= MaxValue
        && GetScale(value) <= CartConstraints.PriceScale;

    private static int GetScale(decimal value) =>
        (decimal.GetBits(value)[3] >> 16) & 0xFF;
}
