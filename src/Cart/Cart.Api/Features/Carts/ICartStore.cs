using CartModel = Cart.Api.Domain.Carts.Cart;

namespace Cart.Api.Features.Carts;

public interface ICartStore
{
    Task<CartModel?> GetAsync(string userId, CancellationToken cancellationToken = default);

    Task SaveAsync(CartModel cart, CancellationToken cancellationToken = default);

    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
}
