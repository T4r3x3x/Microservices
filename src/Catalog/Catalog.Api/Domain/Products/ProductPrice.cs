using System.Globalization;

namespace Catalog.Api.Domain.Products;

public static class ProductPrice
{
    private static readonly decimal MaxValue = decimal.Parse(
        ProductConstraints.MaxPrice,
        CultureInfo.InvariantCulture);

    public static bool IsValid(decimal value) =>
        value >= 0
        && value <= MaxValue
        && GetScale(value) <= ProductConstraints.PriceScale;

    private static int GetScale(decimal value) =>
        (decimal.GetBits(value)[3] >> 16) & 0xFF;
}
