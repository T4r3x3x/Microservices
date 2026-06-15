namespace Catalog.Api.Domain.Products;

public static class ProductConstraints
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int PricePrecision = 18;
    public const int PriceScale = 2;
    public const string MaxPrice = "9999999999999999.99";
}
