using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microservices.Infrastructure;
using Orders.Api.Features.Carts;
using Orders.Api.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Orders.IntegrationTests;

public sealed class OrdersApiFactory :
    WebApplicationFactory<Program>,
    IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder(
            ContainerImages.PostgresImage)
        .WithDatabase("orders_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    private readonly FakeCart cart = new();

    public HttpClient Client { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:orders-db",
            database.GetConnectionString());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CartClient>();
            services.AddScoped(_ =>
            {
                HttpClient httpClient = new(new FakeCartHandler(cart))
                {
                    BaseAddress = new Uri("http://cart-api")
                };

                return new CartClient(httpClient);
            });
        });
    }

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        Client = CreateClient();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        OrdersDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        cart.Clear();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        OrdersDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        await dbContext.OutboxMessages.ExecuteDeleteAsync();
        await dbContext.Orders.ExecuteDeleteAsync();
    }

    public void SetCart(CartResponse response) =>
        cart.Set(response);

    public bool WasCartDeleted(string userId) =>
        cart.WasDeleted(userId);

    public async Task<int> GetOutboxMessageCountAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        OrdersDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        return await dbContext.OutboxMessages.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        OrdersDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        IEnumerable<string> migrations =
            await dbContext.Database.GetAppliedMigrationsAsync();

        return migrations.ToArray();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Client.Dispose();
        Dispose();
        await database.DisposeAsync();
    }
}
