using System.Net;
using System.Net.Http.Json;
using Catalog.Api.Features.Products;

namespace Catalog.IntegrationTests;

public sealed class ProductApiTests :
    IClassFixture<CatalogApiFactory>,
    IAsyncLifetime
{
    private readonly CatalogApiFactory factory;
    private readonly HttpClient client;

    public ProductApiTests(CatalogApiFactory factory)
    {
        this.factory = factory;
        client = factory.Client;
    }

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateProductReturnsCreatedProduct()
    {
        CreateProductRequest request = new(
            "Mechanical Keyboard",
            "Compact keyboard.",
            149.90m,
            18);

        using HttpResponseMessage response =
            await client.PostAsJsonAsync("/products/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ProductResponse product = await ReadProductAsync(response);

        Assert.Equal($"/products/{product.Id}", response.Headers.Location?.ToString());
        Assert.Equal(request.Name, product.Name);
        Assert.Equal(request.Description, product.Description);
        Assert.Equal(request.Price, product.Price);
        Assert.Equal(request.AvailableQuantity, product.AvailableQuantity);
    }

    [Fact]
    public async Task GetProductReturnsExistingProduct()
    {
        ProductResponse createdProduct =
            await CreateProductAsync("Mechanical Keyboard", 149.90m, 18);

        using HttpResponseMessage response =
            await client.GetAsync($"/products/{createdProduct.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ProductResponse product = await ReadProductAsync(response);

        Assert.Equal(createdProduct.Id, product.Id);
        Assert.Equal(createdProduct.Name, product.Name);
        Assert.Equal(createdProduct.Price, product.Price);
        Assert.Equal(createdProduct.AvailableQuantity, product.AvailableQuantity);
        Assert.InRange(
            (createdProduct.CreatedAt - product.CreatedAt).Duration(),
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task UpdateProductChangesExistingProduct()
    {
        ProductResponse createdProduct =
            await CreateProductAsync("Mechanical Keyboard", 149.90m, 18);

        UpdateProductRequest request = new(
            "Mechanical Keyboard Pro",
            null,
            179.90m,
            12);

        using HttpResponseMessage response =
            await client.PutAsJsonAsync(
                $"/products/{createdProduct.Id}",
                request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        ProductResponse product = await GetProductAsync(createdProduct.Id);

        Assert.Equal(request.Name, product.Name);
        Assert.Null(product.Description);
        Assert.Equal(request.Price, product.Price);
        Assert.Equal(request.AvailableQuantity, product.AvailableQuantity);
    }

    [Fact]
    public async Task DeleteProductRemovesExistingProduct()
    {
        ProductResponse product =
            await CreateProductAsync("Mechanical Keyboard", 149.90m, 18);

        using HttpResponseMessage response =
            await client.DeleteAsync($"/products/{product.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using HttpResponseMessage getResponse =
            await client.GetAsync($"/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task ListReturnsPagedAndFilteredProducts()
    {
        await CreateProductAsync("Budget Phone", 299m, 10);
        await CreateProductAsync("Cable", 20m, 100);
        await CreateProductAsync("Pro Keyboard", 120m, 4);
        await CreateProductAsync("Pro Monitor", 450m, 0);

        PagedResponse<ProductResponse> firstPage = await client
            .GetFromJsonAsync<PagedResponse<ProductResponse>>(
                "/products/?page=1&pageSize=2")
            ?? throw new InvalidOperationException("Paged response was empty.");

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(["Budget Phone", "Cable"], firstPage.Items.Select(item => item.Name));

        PagedResponse<ProductResponse> filtered = await client
            .GetFromJsonAsync<PagedResponse<ProductResponse>>(
                "/products/?search=pro&minPrice=100&maxPrice=500&inStock=true")
            ?? throw new InvalidOperationException("Filtered response was empty.");

        ProductResponse product = Assert.Single(filtered.Items);
        Assert.Equal("Pro Keyboard", product.Name);
        Assert.Equal(1, filtered.TotalCount);
    }

    [Fact]
    public async Task InvalidRequestsReturnValidationProblemWithoutWriting()
    {
        CreateProductRequest invalidProduct = new(
            " ",
            null,
            -1m,
            -1);

        using HttpResponseMessage createResponse =
            await client.PostAsJsonAsync("/products/", invalidProduct);

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            createResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await factory.GetProductCountAsync());

        using HttpResponseMessage queryResponse =
            await client.GetAsync("/products/?page=0&minPrice=100&maxPrice=10");

        Assert.Equal(HttpStatusCode.BadRequest, queryResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            queryResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MigrationsAndDevelopmentSeederWorkWithPostgreSql()
    {
        IReadOnlyList<string> migrations =
            await factory.GetAppliedMigrationsAsync();

        Assert.Contains(
            migrations,
            migration => migration.EndsWith("_InitialCatalog", StringComparison.Ordinal));

        await factory.SeedAsync();
        await factory.SeedAsync();

        Assert.Equal(4, await factory.GetProductCountAsync());
    }

    private async Task<ProductResponse> CreateProductAsync(
        string name,
        decimal price,
        int availableQuantity)
    {
        CreateProductRequest request = new(
            name,
            null,
            price,
            availableQuantity);

        using HttpResponseMessage response =
            await client.PostAsJsonAsync("/products/", request);
        response.EnsureSuccessStatusCode();

        return await ReadProductAsync(response);
    }

    private async Task<ProductResponse> GetProductAsync(Guid id) =>
        await client.GetFromJsonAsync<ProductResponse>($"/products/{id}")
        ?? throw new InvalidOperationException("Product response was empty.");

    private static async Task<ProductResponse> ReadProductAsync(
        HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<ProductResponse>()
        ?? throw new InvalidOperationException("Product response was empty.");
}
