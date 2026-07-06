using System.Net;
using System.Net.Http.Json;

namespace Orders.Api.Features.Carts;

public sealed class CartClient(HttpClient httpClient)
{
    public async Task<CartResponse?> GetCartAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.GetAsync($"/carts/{userId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CartResponse>(
            cancellationToken);
    }

    public async Task DeleteCartAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await httpClient.DeleteAsync($"/carts/{userId}", cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
