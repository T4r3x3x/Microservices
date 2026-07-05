using System.Net;
using System.Net.Http.Json;
using Cart.Api.Features.Carts;

namespace Cart.IntegrationTests;

public sealed class CartApiTests :
    IClassFixture<CartApiFactory>,
    IAsyncLifetime
{
    private readonly CartApiFactory factory;
    private readonly HttpClient client;

    public CartApiTests(CartApiFactory factory)
    {
        this.factory = factory;
        client = factory.Client;
    }

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddItemStoresCatalogSnapshotInRedis()
    {
        Guid productId = Guid.NewGuid();
        factory.AddCatalogProduct(productId, "Mechanical Keyboard", 149.90m, 5);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/carts/user-1/items",
            new UpsertCartItemRequest(productId, 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CartResponse cart = await ReadCartAsync(response);
        CartItemResponse item = Assert.Single(cart.Items);

        Assert.Equal("user-1", cart.UserId);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Mechanical Keyboard", item.ProductName);
        Assert.Equal(149.90m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(299.80m, item.TotalPrice);
        Assert.Equal(299.80m, cart.TotalPrice);
    }

    [Fact]
    public async Task UpdateItemRefreshesQuantityAndCatalogSnapshot()
    {
        Guid productId = Guid.NewGuid();
        factory.AddCatalogProduct(productId, "Keyboard", 100m, 5);
        await AddItemAsync("user-2", productId, 1);

        factory.AddCatalogProduct(productId, "Keyboard Pro", 120m, 5);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/carts/user-2/items/{productId}",
            new UpsertCartItemRequest(productId, 3));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CartItemResponse item = Assert.Single((await ReadCartAsync(response)).Items);

        Assert.Equal("Keyboard Pro", item.ProductName);
        Assert.Equal(120m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(360m, item.TotalPrice);
    }

    [Fact]
    public async Task RemoveLastItemDeletesCart()
    {
        Guid productId = Guid.NewGuid();
        factory.AddCatalogProduct(productId, "Mouse", 79.50m, 4);
        await AddItemAsync("user-3", productId, 1);

        using HttpResponseMessage deleteResponse =
            await client.DeleteAsync($"/carts/user-3/items/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Null(await factory.GetCartTimeToLiveAsync("user-3"));

        CartResponse cart = await client.GetFromJsonAsync<CartResponse>("/carts/user-3")
            ?? throw new InvalidOperationException("Cart response was empty.");

        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task MissingCatalogProductReturnsNotFound()
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/carts/user-4/items",
            new UpsertCartItemRequest(Guid.NewGuid(), 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task QuantityGreaterThanAvailableStockReturnsConflict()
    {
        Guid productId = Guid.NewGuid();
        factory.AddCatalogProduct(productId, "Monitor", 499m, 2);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/carts/user-5/items",
            new UpsertCartItemRequest(productId, 3));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SaveRefreshesCartTimeToLive()
    {
        Guid firstProductId = Guid.NewGuid();
        Guid secondProductId = Guid.NewGuid();
        factory.AddCatalogProduct(firstProductId, "Cable", 20m, 10);
        factory.AddCatalogProduct(secondProductId, "Adapter", 35m, 10);

        await AddItemAsync("user-6", firstProductId, 1);
        TimeSpan? firstTimeToLive = await factory.GetCartTimeToLiveAsync("user-6");

        await Task.Delay(TimeSpan.FromSeconds(1));
        await AddItemAsync("user-6", secondProductId, 1);
        TimeSpan? refreshedTimeToLive = await factory.GetCartTimeToLiveAsync("user-6");

        Assert.NotNull(firstTimeToLive);
        Assert.NotNull(refreshedTimeToLive);
        Assert.True(refreshedTimeToLive > firstTimeToLive);
    }

    private async Task<CartResponse> AddItemAsync(
        string userId,
        Guid productId,
        int quantity)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/carts/{userId}/items",
            new UpsertCartItemRequest(productId, quantity));
        response.EnsureSuccessStatusCode();

        return await ReadCartAsync(response);
    }

    private static async Task<CartResponse> ReadCartAsync(
        HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<CartResponse>()
        ?? throw new InvalidOperationException("Cart response was empty.");
}
