namespace Orders.Api.Domain.Orders;

public static class OrderConstraints
{
    public const int UserIdMaxLength = 200;
    public const int ProductNameMaxLength = 200;
    public const int MoneyPrecision = 18;
    public const int MoneyScale = 2;
    public const string MaxMoney = "9999999999999999.99";
}
