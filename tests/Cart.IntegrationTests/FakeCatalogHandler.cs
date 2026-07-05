using System.Net;
using System.Net.Http.Json;
using Cart.Api.Features.Catalog;

namespace Cart.IntegrationTests;

public sealed class FakeCatalogHandler(FakeCatalog catalog) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Get
            && TryGetProductId(request.RequestUri, out Guid productId)
            && catalog.TryGet(productId, out ProductCatalogItem? product))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(product)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static bool TryGetProductId(Uri? requestUri, out Guid productId)
    {
        productId = Guid.Empty;

        string? productIdSegment = requestUri?.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return Guid.TryParse(productIdSegment, out productId);
    }
}
