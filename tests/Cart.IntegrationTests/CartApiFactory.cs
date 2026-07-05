using Cart.Api.Features.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microservices.Infrastructure;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Cart.IntegrationTests;

public sealed class CartApiFactory :
    WebApplicationFactory<Program>,
    IAsyncLifetime
{
    private readonly RedisContainer redis = new RedisBuilder(ContainerImages.RedisImage)
        .Build();
    private readonly FakeCatalog catalog = new();

    public HttpClient Client { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:cart-cache", redis.GetConnectionString());
        builder.UseSetting("Cart:TimeToLive", "00:05:00");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ProductCatalogClient>();
            services.AddScoped(_ =>
            {
                HttpClient httpClient = new(new FakeCatalogHandler(catalog))
                {
                    BaseAddress = new Uri("http://catalog-api")
                };

                return new ProductCatalogClient(httpClient);
            });
        });
    }

    public async Task InitializeAsync()
    {
        await redis.StartAsync();
        Client = CreateClient();
    }

    public async Task ResetAsync()
    {
        catalog.Clear();

        IConnectionMultiplexer connection =
            Services.GetRequiredService<IConnectionMultiplexer>();
        await connection.GetDatabase().ExecuteAsync("FLUSHDB");
    }

    public void AddCatalogProduct(
        Guid productId,
        string name,
        decimal price,
        int availableQuantity) =>
        catalog.Add(new ProductCatalogItem(
            productId,
            name,
            null,
            price,
            availableQuantity,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

    public async Task<TimeSpan?> GetCartTimeToLiveAsync(string userId)
    {
        IConnectionMultiplexer connection =
            Services.GetRequiredService<IConnectionMultiplexer>();

        return await connection
            .GetDatabase()
            .KeyTimeToLiveAsync($"cart:{userId}");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Client.Dispose();
        Dispose();
        await redis.DisposeAsync();
    }
}
