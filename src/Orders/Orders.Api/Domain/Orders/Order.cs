namespace Orders.Api.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> items = [];

    private Order()
    {
    }

    public Order(
        Guid id,
        string userId,
        IEnumerable<OrderItem> orderItems,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id cannot be empty.",
                nameof(id));
        }

        Id = id;
        SetUserId(userId);
        AddItems(orderItems);
        Status = OrderStatus.Created;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string UserId { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => items.AsReadOnly();

    public decimal TotalPrice => items.Sum(item => item.TotalPrice);

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Complete(DateTimeOffset updatedAt)
    {
        if (Status != OrderStatus.Created)
        {
            throw new InvalidOperationException(
                "Only created order can be completed.");
        }

        Status = OrderStatus.Completed;
        MarkUpdated(updatedAt);
    }

    public void Cancel(DateTimeOffset updatedAt)
    {
        if (Status != OrderStatus.Created)
        {
            throw new InvalidOperationException(
                "Only created order can be cancelled.");
        }

        Status = OrderStatus.Cancelled;
        MarkUpdated(updatedAt);
    }

    private void SetUserId(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        string normalizedUserId = userId.Trim();

        if (normalizedUserId.Length > OrderConstraints.UserIdMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                userId,
                $"User id cannot exceed {OrderConstraints.UserIdMaxLength} characters.");
        }

        UserId = normalizedUserId;
    }

    private void AddItems(IEnumerable<OrderItem> orderItems)
    {
        ArgumentNullException.ThrowIfNull(orderItems);

        HashSet<Guid> productIds = [];

        foreach (OrderItem item in orderItems)
        {
            if (!productIds.Add(item.ProductId))
            {
                throw new ArgumentException(
                    "Order cannot contain duplicate products.",
                    nameof(orderItems));
            }

            items.Add(item);
        }

        if (items.Count == 0)
        {
            throw new ArgumentException(
                "Order must contain at least one item.",
                nameof(orderItems));
        }
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
