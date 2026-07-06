using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using Orders.Api.Features.Carts;

namespace Orders.IntegrationTests;

public sealed class FakeCartHandler(FakeCart cart) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (TryGetUserId(request.RequestUri, out string? userId))
        {
            if (request.Method == HttpMethod.Get
                && cart.TryGet(userId, out CartResponse? response))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            }

            if (request.Method == HttpMethod.Delete)
            {
                cart.Delete(userId);

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static bool TryGetUserId(
        Uri? requestUri,
        [NotNullWhen(true)] out string? userId)
    {
        userId = null;

        string[] segments = requestUri?.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            ?? [];

        if (segments is not ["carts", string foundUserId])
        {
            return false;
        }

        userId = foundUserId;

        return true;
    }
}
