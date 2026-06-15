namespace Catalog.Api.Domain.Products;

public sealed class Product
{
    private Product()
    {
    }

    public Product(
        Guid id,
        string name,
        string? description,
        decimal price,
        int availableQuantity,
        DateTimeOffset createdAt)
    {
        Id = id;
        SetDetails(name, description, price);
        SetAvailableQuantity(availableQuantity);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public int AvailableQuantity { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateDetails(
        string name,
        string? description,
        decimal price,
        DateTimeOffset updatedAt)
    {
        SetDetails(name, description, price);
        MarkUpdated(updatedAt);
    }

    public void ChangeAvailableQuantity(
        int availableQuantity,
        DateTimeOffset updatedAt)
    {
        SetAvailableQuantity(availableQuantity);
        MarkUpdated(updatedAt);
    }

    private void SetDetails(string name, string? description, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                price,
                "Product price cannot be negative.");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        Price = price;
    }

    private void SetAvailableQuantity(int availableQuantity)
    {
        if (availableQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableQuantity),
                availableQuantity,
                "Available quantity cannot be negative.");
        }

        AvailableQuantity = availableQuantity;
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
