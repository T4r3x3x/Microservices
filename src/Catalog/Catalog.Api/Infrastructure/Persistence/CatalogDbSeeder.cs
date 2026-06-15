using Catalog.Api.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Infrastructure.Persistence;

public static class CatalogDbSeeder
{
    private static readonly DateTimeOffset SeedCreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly Product[] Products =
    [
        new(
            Guid.Parse("01976d7b-b06a-7000-8000-000000000001"),
            "Laptop",
            "Development laptop with 16 GB RAM and 512 GB SSD.",
            1299.00m,
            5,
            SeedCreatedAt),
        new(
            Guid.Parse("01976d7b-b06a-7000-8000-000000000002"),
            "Mechanical Keyboard",
            "Compact mechanical keyboard.",
            149.90m,
            18,
            SeedCreatedAt),
        new(
            Guid.Parse("01976d7b-b06a-7000-8000-000000000003"),
            "Wireless Mouse",
            "Ergonomic wireless mouse.",
            79.50m,
            32,
            SeedCreatedAt),
        new(
            Guid.Parse("01976d7b-b06a-7000-8000-000000000004"),
            "4K Monitor",
            "27-inch 4K monitor currently out of stock.",
            499.00m,
            0,
            SeedCreatedAt)
    ];

    public static async Task SeedCatalogAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        Guid[] seedProductIds = Products
            .Select(product => product.Id)
            .ToArray();

        HashSet<Guid> existingProductIds = await dbContext.Products
            .Where(product => seedProductIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToHashSetAsync(cancellationToken);

        Product[] missingProducts = Products
            .Where(product => !existingProductIds.Contains(product.Id))
            .ToArray();

        if (missingProducts.Length == 0)
        {
            return;
        }

        dbContext.Products.AddRange(missingProducts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
