using Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests;

public sealed class CatalogApiFactory :
    WebApplicationFactory<Program>,
    IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18.3")
        .WithDatabase("catalog_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public HttpClient Client { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:catalog-db",
            _database.GetConnectionString());
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        Client = CreateClient();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await dbContext.Products.ExecuteDeleteAsync();
    }

    public async Task<int> GetProductCountAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await dbContext.Products.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        IEnumerable<string> migrations =
            await dbContext.Database.GetAppliedMigrationsAsync();

        return migrations.ToArray();
    }

    public Task SeedAsync() => Services.SeedCatalogAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        Client.Dispose();
        Dispose();
        await _database.DisposeAsync();
    }
}
