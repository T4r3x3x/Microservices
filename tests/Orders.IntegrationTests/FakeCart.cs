using System.Collections.Concurrent;
using Orders.Api.Features.Carts;

namespace Orders.IntegrationTests;

public sealed class FakeCart
{
    private readonly ConcurrentDictionary<string, CartResponse> carts = [];
    private readonly ConcurrentDictionary<string, bool> deletedCarts = [];

    public void Set(CartResponse cart) =>
        carts[cart.UserId] = cart;

    public bool TryGet(string userId, out CartResponse? cart) =>
        carts.TryGetValue(userId, out cart);

    public void Delete(string userId)
    {
        carts.TryRemove(userId, out _);
        deletedCarts[userId] = true;
    }

    public bool WasDeleted(string userId) =>
        deletedCarts.ContainsKey(userId);

    public void Clear()
    {
        carts.Clear();
        deletedCarts.Clear();
    }
}
