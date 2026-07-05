using Cart.Api.Domain.Carts;
using Cart.Api.Features.Catalog;
using Microsoft.AspNetCore.Http.HttpResults;
using CartModel = Cart.Api.Domain.Carts.Cart;

namespace Cart.Api.Features.Carts;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/carts")
            .WithTags("Carts");

        group.MapGet("/{userId}", GetCart);
        group.MapPost("/{userId}/items", AddCartItem);
        group.MapPut("/{userId}/items/{productId:guid}", ChangeCartItem);
        group.MapDelete("/{userId}/items/{productId:guid}", RemoveCartItem);
        group.MapDelete("/{userId}", DeleteCart);

        return endpoints;
    }

    private static async Task<Ok<CartResponse>> GetCart(
        string userId,
        ICartStore cartStore,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        CartModel cart = await cartStore.GetAsync(userId, cancellationToken)
            ?? new CartModel(userId, timeProvider.GetUtcNow());

        return TypedResults.Ok(ToResponse(cart));
    }

    private static async Task<Results<Ok<CartResponse>, NotFound, Conflict<string>>> AddCartItem(
        string userId,
        UpsertCartItemRequest request,
        ICartStore cartStore,
        ProductCatalogClient catalogClient,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        CartModel cart = await cartStore.GetAsync(userId, cancellationToken)
            ?? new CartModel(userId, now);
        ProductCatalogItem? product =
            await catalogClient.GetProductAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        int currentQuantity = cart.Items
            .SingleOrDefault(item => item.ProductId == request.ProductId)
            ?.Quantity ?? 0;
        int requestedQuantity = checked(currentQuantity + request.Quantity);

        if (requestedQuantity > product.AvailableQuantity)
        {
            return TypedResults.Conflict(
                $"Product '{product.Name}' has only {product.AvailableQuantity} items available.");
        }

        cart.AddItem(
            request.ProductId,
            product.Name,
            product.Price,
            request.Quantity,
            now);

        await cartStore.SaveAsync(cart, cancellationToken);

        return TypedResults.Ok(ToResponse(cart));
    }

    private static async Task<Results<Ok<CartResponse>, NotFound, Conflict<string>>> ChangeCartItem(
        string userId,
        Guid productId,
        UpsertCartItemRequest request,
        ICartStore cartStore,
        ProductCatalogClient catalogClient,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        CartModel? cart = await cartStore.GetAsync(userId, cancellationToken);

        if (cart is null || productId != request.ProductId)
        {
            return TypedResults.NotFound();
        }

        ProductCatalogItem? product =
            await catalogClient.GetProductAsync(productId, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Quantity > product.AvailableQuantity)
        {
            return TypedResults.Conflict(
                $"Product '{product.Name}' has only {product.AvailableQuantity} items available.");
        }

        bool changed = cart.ChangeItem(
            productId,
            product.Name,
            product.Price,
            request.Quantity,
            timeProvider.GetUtcNow());

        if (!changed)
        {
            return TypedResults.NotFound();
        }

        await cartStore.SaveAsync(cart, cancellationToken);

        return TypedResults.Ok(ToResponse(cart));
    }

    private static async Task<Results<NoContent, NotFound>> RemoveCartItem(
        string userId,
        Guid productId,
        ICartStore cartStore,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        CartModel? cart = await cartStore.GetAsync(userId, cancellationToken);

        if (cart is null || !cart.RemoveItem(productId, timeProvider.GetUtcNow()))
        {
            return TypedResults.NotFound();
        }

        if (cart.IsEmpty)
        {
            await cartStore.DeleteAsync(userId, cancellationToken);
        }
        else
        {
            await cartStore.SaveAsync(cart, cancellationToken);
        }

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteCart(
        string userId,
        ICartStore cartStore,
        CancellationToken cancellationToken)
    {
        await cartStore.DeleteAsync(userId, cancellationToken);

        return TypedResults.NoContent();
    }

    private static CartResponse ToResponse(CartModel cart) =>
        new(
            cart.UserId,
            cart.Items
                .OrderBy(item => item.ProductName)
                .ThenBy(item => item.ProductId)
                .Select(ToResponse)
                .ToArray(),
            cart.TotalPrice,
            cart.CreatedAt,
            cart.UpdatedAt);

    private static CartItemResponse ToResponse(CartItem item) =>
        new(
            item.ProductId,
            item.ProductName,
            item.UnitPrice,
            item.Quantity,
            item.TotalPrice);
}
