using System.Text.Json;
using Cart.Api.Domain.Carts;
using Cart.Api.Features.Carts;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using CartModel = Cart.Api.Domain.Carts.Cart;

namespace Cart.Api.Infrastructure.Persistence;

public sealed class RedisCartStore(
    IConnectionMultiplexer connection,
    IOptions<CartOptions> options) : ICartStore
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IDatabase database = connection.GetDatabase();

    public async Task<CartModel?> GetAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RedisValue value = await database.StringGetAsync(BuildKey(userId));

        cancellationToken.ThrowIfCancellationRequested();

        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<CartDocument>(
                value.ToString(),
                SerializerOptions)?.ToCart();
    }

    public async Task SaveAsync(
        CartModel cart,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);
        cancellationToken.ThrowIfCancellationRequested();

        string value = JsonSerializer.Serialize(
            CartDocument.FromCart(cart),
            SerializerOptions);

        await database.StringSetAsync(
            BuildKey(cart.UserId),
            value,
            options.Value.TimeToLive);
    }

    public async Task DeleteAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await database.KeyDeleteAsync(BuildKey(userId));
    }

    private static string BuildKey(string userId)
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

        return $"cart:{normalizedUserId}";
    }

    private sealed record CartDocument(
        string UserId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        CartItemDocument[] Items)
    {
        public static CartDocument FromCart(CartModel cart) =>
            new(
                cart.UserId,
                cart.CreatedAt,
                cart.UpdatedAt,
                cart.Items.Select(CartItemDocument.FromCartItem).ToArray());

        public CartModel ToCart() =>
            CartModel.Restore(
                UserId,
                Items.Select(item => item.ToCartItem()),
                CreatedAt,
                UpdatedAt);
    }

    private sealed record CartItemDocument(
        Guid ProductId,
        string ProductName,
        decimal UnitPrice,
        int Quantity)
    {
        public static CartItemDocument FromCartItem(CartItem item) =>
            new(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity);

        public CartItem ToCartItem() =>
            new(ProductId, ProductName, UnitPrice, Quantity);
    }
}
