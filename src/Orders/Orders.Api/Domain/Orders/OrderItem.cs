namespace Orders.Api.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    public OrderItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product id cannot be empty.",
                nameof(productId));
        }

        ProductId = productId;
        SetSnapshot(productName, unitPrice);
        SetQuantity(quantity);
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    private void SetSnapshot(string productName, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        string normalizedProductName = productName.Trim();

        if (normalizedProductName.Length > OrderConstraints.ProductNameMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productName),
                productName,
                $"Product name cannot exceed {OrderConstraints.ProductNameMaxLength} characters.");
        }

        if (!OrderMoney.IsValid(unitPrice))
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                $"Unit price must fit decimal({OrderConstraints.MoneyPrecision},{OrderConstraints.MoneyScale}).");
        }

        ProductName = normalizedProductName;
        UnitPrice = unitPrice;
    }

    private void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Quantity must be positive.");
        }

        Quantity = quantity;
    }
}
