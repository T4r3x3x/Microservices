using Catalog.Api.Domain.Products;
using Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/products")
            .WithTags("Products");

        group.MapGet("/", GetProducts);
        group.MapGet("/{id:guid}", GetProduct);
        group.MapPost("/", CreateProduct);
        group.MapPut("/{id:guid}", UpdateProduct);
        group.MapDelete("/{id:guid}", DeleteProduct);

        return endpoints;
    }

    private static async Task<Ok<PagedResponse<ProductResponse>>> GetProducts(
        [AsParameters] ProductListQuery query,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(query.Page ?? 1, 1);
        int pageSize = Math.Clamp(query.PageSize ?? 20, 1, 100);
        IQueryable<Product> productsQuery = ApplyFilters(
            dbContext.Products.AsNoTracking(),
            query);
        int totalCount = await productsQuery.CountAsync(cancellationToken);

        List<ProductResponse> products = await productsQuery
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => ToResponse(product))
            .ToListAsync(cancellationToken);

        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        PagedResponse<ProductResponse> response = new(
            products,
            page,
            pageSize,
            totalCount,
            totalPages);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetProduct(
        Guid id,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ProductResponse? product = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => ToResponse(product))
            .SingleOrDefaultAsync(cancellationToken);

        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(product);
    }

    private static async Task<Created<ProductResponse>> CreateProduct(
        CreateProductRequest request,
        CatalogDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Product product = new(
            Guid.NewGuid(),
            request.Name,
            request.Description,
            request.Price,
            request.AvailableQuantity,
            now);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        ProductResponse response = ToResponse(product);

        return TypedResults.Created($"/products/{product.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        CatalogDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        product.UpdateDetails(
            request.Name,
            request.Description,
            request.Price,
            now);
        product.ChangeAvailableQuantity(request.AvailableQuantity, now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteProduct(
        Guid id,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        int deletedProducts = await dbContext.Products
            .Where(product => product.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedProducts == 0
            ? TypedResults.NotFound()
            : TypedResults.NoContent();
    }

    private static IQueryable<Product> ApplyFilters(
        IQueryable<Product> products,
        ProductListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string searchPattern =
                $"%{EscapeLikePattern(query.Search.Trim())}%";

            products = products.Where(product =>
                EF.Functions.ILike(product.Name, searchPattern, "\\"));
        }

        if (query.MinPrice is not null)
        {
            products = products.Where(product =>
                product.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice is not null)
        {
            products = products.Where(product =>
                product.Price <= query.MaxPrice.Value);
        }

        if (query.InStock is not null)
        {
            products = query.InStock.Value
                ? products.Where(product => product.AvailableQuantity > 0)
                : products.Where(product => product.AvailableQuantity == 0);
        }

        return products;
    }

    private static string EscapeLikePattern(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static ProductResponse ToResponse(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.AvailableQuantity,
            product.CreatedAt,
            product.UpdatedAt);
}
