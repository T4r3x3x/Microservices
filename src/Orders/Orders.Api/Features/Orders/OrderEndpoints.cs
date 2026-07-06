using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Domain.Orders;
using Orders.Api.Features.Carts;
using Orders.Api.Infrastructure.Messaging;
using Orders.Api.Infrastructure.Persistence;
using OrderModel = Orders.Api.Domain.Orders.Order;

namespace Orders.Api.Features.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/orders")
            .WithTags("Orders");

        group.MapPost("/", CreateOrder);
        group.MapGet("/{id:guid}", GetOrder);
        group.MapGet("/", GetOrders);

        return endpoints;
    }

    private static async Task<Results<Created<OrderResponse>, Conflict<string>, StatusCodeHttpResult>> CreateOrder(
        CreateOrderRequest request,
        OrdersDbContext dbContext,
        CartClient cartClient,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        CartResponse? cart;

        try
        {
            cart = await cartClient.GetCartAsync(request.UserId, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return TypedResults.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (cart is null || cart.Items.Count == 0)
        {
            return TypedResults.Conflict("Cart is empty.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        OrderItem[] items = cart.Items
            .Select(item => new OrderItem(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity))
            .ToArray();
        OrderModel order = new(
            Guid.NewGuid(),
            cart.UserId,
            items,
            now);
        OrderCreatedEvent orderCreated = ToOrderCreatedEvent(order);
        OutboxMessage outboxMessage = OutboxMessageFactory.CreateOrderCreated(
            orderCreated,
            now);

        dbContext.Orders.Add(order);
        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DeleteCartBestEffortAsync(
            cartClient,
            cart.UserId,
            cancellationToken);

        OrderResponse response = ToResponse(order);

        return TypedResults.Created($"/orders/{order.Id}", response);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetOrder(
        Guid id,
        OrdersDbContext dbContext,
        CancellationToken cancellationToken)
    {
        OrderModel? order = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

        return order is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(order));
    }

    private static async Task<Ok<List<OrderResponse>>> GetOrders(
        string userId,
        OrdersDbContext dbContext,
        CancellationToken cancellationToken)
    {
        List<OrderResponse> orders = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Select(order => ToResponse(order))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(orders);
    }

    private static OrderResponse ToResponse(OrderModel order) =>
        new(
            order.Id,
            order.UserId,
            order.Status,
            order.Items
                .OrderBy(item => item.ProductName)
                .ThenBy(item => item.ProductId)
                .Select(ToResponse)
                .ToArray(),
            order.TotalPrice,
            order.CreatedAt,
            order.UpdatedAt);

    private static OrderItemResponse ToResponse(OrderItem item) =>
        new(
            item.ProductId,
            item.ProductName,
            item.UnitPrice,
            item.Quantity,
            item.TotalPrice);

    private static OrderCreatedEvent ToOrderCreatedEvent(OrderModel order) =>
        new(
            order.Id,
            order.UserId,
            order.TotalPrice,
            order.Items
                .Select(item => new OrderCreatedItem(
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.TotalPrice))
                .ToArray(),
            order.CreatedAt);

    private static async Task DeleteCartBestEffortAsync(
        CartClient cartClient,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await cartClient.DeleteCartAsync(userId, cancellationToken);
        }
        catch (HttpRequestException)
        {
        }
    }
}
