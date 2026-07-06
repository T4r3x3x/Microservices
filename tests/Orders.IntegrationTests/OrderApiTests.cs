using System.Net;
using System.Net.Http.Json;
using Orders.Api.Domain.Orders;
using Orders.Api.Features.Carts;
using Orders.Api.Features.Orders;

namespace Orders.IntegrationTests;

public sealed class OrderApiTests :
    IClassFixture<OrdersApiFactory>,
    IAsyncLifetime
{
    private readonly OrdersApiFactory factory;
    private readonly HttpClient client;

    public OrderApiTests(OrdersApiFactory factory)
    {
        this.factory = factory;
        client = factory.Client;
    }

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateOrderPersistsCartSnapshotOutboxAndDeletesCart()
    {
        Guid productId = Guid.NewGuid();
        factory.SetCart(CreateCart(
            "user-1",
            new CartItemResponse(
                productId,
                "Mechanical Keyboard",
                149.90m,
                2,
                299.80m)));

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/",
            new CreateOrderRequest("user-1"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        OrderResponse order = await ReadOrderAsync(response);
        OrderItemResponse item = Assert.Single(order.Items);

        Assert.Equal("user-1", order.UserId);
        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Mechanical Keyboard", item.ProductName);
        Assert.Equal(149.90m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(299.80m, order.TotalPrice);
        Assert.Equal(1, await factory.GetOutboxMessageCountAsync());
        Assert.True(factory.WasCartDeleted("user-1"));
    }

    [Fact]
    public async Task GetOrderReturnsPersistedOrder()
    {
        factory.SetCart(CreateCart(
            "user-2",
            new CartItemResponse(Guid.NewGuid(), "Mouse", 79.50m, 1, 79.50m)));
        OrderResponse createdOrder = await CreateOrderAsync("user-2");

        using HttpResponseMessage response =
            await client.GetAsync($"/orders/{createdOrder.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        OrderResponse order = await ReadOrderAsync(response);

        Assert.Equal(createdOrder.Id, order.Id);
        Assert.Equal(createdOrder.UserId, order.UserId);
        Assert.Equal(createdOrder.TotalPrice, order.TotalPrice);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task GetOrdersReturnsOnlyRequestedUserOrders()
    {
        factory.SetCart(CreateCart(
            "user-3",
            new CartItemResponse(Guid.NewGuid(), "Cable", 20m, 1, 20m)));
        OrderResponse userOrder = await CreateOrderAsync("user-3");

        factory.SetCart(CreateCart(
            "other-user",
            new CartItemResponse(Guid.NewGuid(), "Adapter", 35m, 1, 35m)));
        await CreateOrderAsync("other-user");

        List<OrderResponse> orders = await client
            .GetFromJsonAsync<List<OrderResponse>>("/orders/?userId=user-3")
            ?? throw new InvalidOperationException("Orders response was empty.");

        OrderResponse order = Assert.Single(orders);
        Assert.Equal(userOrder.Id, order.Id);
    }

    [Fact]
    public async Task EmptyCartReturnsConflictWithoutOutboxMessage()
    {
        factory.SetCart(CreateCart("user-4"));

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/",
            new CreateOrderRequest("user-4"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, await factory.GetOutboxMessageCountAsync());
        Assert.False(factory.WasCartDeleted("user-4"));
    }

    [Fact]
    public async Task MigrationsAreApplied()
    {
        IReadOnlyList<string> migrations =
            await factory.GetAppliedMigrationsAsync();

        Assert.Contains(
            migrations,
            migration => migration.EndsWith("_InitialOrders", StringComparison.Ordinal));
        Assert.Contains(
            migrations,
            migration => migration.EndsWith("_AddOutboxMessages", StringComparison.Ordinal));
    }

    private async Task<OrderResponse> CreateOrderAsync(string userId)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/",
            new CreateOrderRequest(userId));
        response.EnsureSuccessStatusCode();

        return await ReadOrderAsync(response);
    }

    private static CartResponse CreateCart(
        string userId,
        params CartItemResponse[] items) =>
        new(
            userId,
            items,
            items.Sum(item => item.TotalPrice),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static async Task<OrderResponse> ReadOrderAsync(
        HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<OrderResponse>()
        ?? throw new InvalidOperationException("Order response was empty.");
}
