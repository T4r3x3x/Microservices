namespace Cart.Api.Domain.Carts;

public sealed class Cart
{
    private readonly List<CartItem> items = [];

    private Cart()
    {
    }

    public Cart(string userId, DateTimeOffset createdAt)
    {
        SetUserId(userId);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Cart Restore(
        string userId,
        IEnumerable<CartItem> restoredItems,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Cart cart = new(userId, createdAt);
        HashSet<Guid> productIds = [];

        foreach (CartItem item in restoredItems)
        {
            if (!productIds.Add(item.ProductId))
            {
                throw new ArgumentException(
                    "Cart cannot contain duplicate products.",
                    nameof(restoredItems));
            }

            cart.items.Add(item);
        }

        cart.MarkUpdated(updatedAt);

        return cart;
    }

    public string UserId { get; private set; } = string.Empty;

    public IReadOnlyCollection<CartItem> Items => items.AsReadOnly();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public decimal TotalPrice => items.Sum(item => item.TotalPrice);

    public bool IsEmpty => items.Count == 0;

    public void AddItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity,
        DateTimeOffset updatedAt)
    {
        CartItem? item = FindItem(productId);

        if (item is null)
        {
            items.Add(new CartItem(productId, productName, unitPrice, quantity));
        }
        else
        {
            item.UpdateSnapshot(productName, unitPrice);
            item.IncreaseQuantity(quantity);
        }

        MarkUpdated(updatedAt);
    }

    public bool ChangeItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity,
        DateTimeOffset updatedAt)
    {
        CartItem? item = FindItem(productId);

        if (item is null)
        {
            return false;
        }

        item.UpdateSnapshot(productName, unitPrice);
        item.ChangeQuantity(quantity);
        MarkUpdated(updatedAt);

        return true;
    }

    public bool RemoveItem(Guid productId, DateTimeOffset updatedAt)
    {
        CartItem? item = FindItem(productId);

        if (item is null)
        {
            return false;
        }

        items.Remove(item);
        MarkUpdated(updatedAt);

        return true;
    }

    public void Clear(DateTimeOffset updatedAt)
    {
        if (items.Count == 0)
        {
            return;
        }

        items.Clear();
        MarkUpdated(updatedAt);
    }

    private CartItem? FindItem(Guid productId) =>
        items.SingleOrDefault(item => item.ProductId == productId);

    private void SetUserId(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        string normalizedUserId = userId.Trim();

        if (normalizedUserId.Length > CartConstraints.UserIdMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                userId,
                $"User id cannot exceed {CartConstraints.UserIdMaxLength} characters.");
        }

        UserId = normalizedUserId;
    }

    private void MarkUpdated(DateTimeOffset updatedAt)
    {
        if (updatedAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAt),
                updatedAt,
                "Update time cannot be earlier than creation time.");
        }

        UpdatedAt = updatedAt;
    }
}
