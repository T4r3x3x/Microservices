using System.Net;
using System.Net.Http.Json;

namespace Cart.Api.Features.Catalog;

public sealed class ProductCatalogClient(HttpClient httpClient)
{
    public async Task<ProductCatalogItem?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.GetAsync($"/products/{productId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductCatalogItem>(
            cancellationToken);
    }
}
