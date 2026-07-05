using System.Collections.Concurrent;
using Cart.Api.Features.Catalog;

namespace Cart.IntegrationTests;

public sealed class FakeCatalog
{
    private readonly ConcurrentDictionary<Guid, ProductCatalogItem> products = [];

    public void Add(ProductCatalogItem product) =>
        products[product.Id] = product;

    public bool TryGet(Guid productId, out ProductCatalogItem? product) =>
        products.TryGetValue(productId, out product);

    public void Clear() =>
        products.Clear();
}
