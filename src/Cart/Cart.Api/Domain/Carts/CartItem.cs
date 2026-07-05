namespace Cart.Api.Domain.Carts;

public sealed class CartItem
{
    private CartItem()
    {
    }

    public CartItem(
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

    public void UpdateSnapshot(string productName, decimal unitPrice) =>
        SetSnapshot(productName, unitPrice);

    public void ChangeQuantity(int quantity) =>
        SetQuantity(quantity);

    public void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Quantity increment must be positive.");
        }

        SetQuantity(checked(Quantity + quantity));
    }

    private void SetSnapshot(string productName, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        string normalizedProductName = productName.Trim();

        if (normalizedProductName.Length > CartConstraints.ProductNameMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productName),
                productName,
                $"Product name cannot exceed {CartConstraints.ProductNameMaxLength} characters.");
        }

        if (!CartPrice.IsValid(unitPrice))
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                $"Unit price must fit decimal({CartConstraints.PricePrecision},{CartConstraints.PriceScale}).");
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
