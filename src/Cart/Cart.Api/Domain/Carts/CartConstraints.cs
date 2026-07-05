namespace Cart.Api.Domain.Carts;

public static class CartConstraints
{
    public const int UserIdMaxLength = 200;
    public const int ProductNameMaxLength = 200;
    public const int PricePrecision = 18;
    public const int PriceScale = 2;
    public const string MaxPrice = "9999999999999999.99";
}
